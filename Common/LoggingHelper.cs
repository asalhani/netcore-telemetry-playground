﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Enrichers.Span;

namespace Common
{
   public class LoggingHelper
    {
        /// <summary>
        /// In the target service, for example: InspectionProcessService make sure you provide
        /// settings inside the Serilog section.
        /// For example:
        ///     "Internal": {
        ///          "Enable": true,
        ///          "SerilogExceptionsFile": "logs/InternalSerilogExceptions.log",
        ///          "RotateSizeMB": 0.5,
        ///          "HeartBeatMilliSeconds": 30
        ///     },
        ///     
        /// Later on, if for some reason Serilog failed to log to one or all of its sinks, it will log that failure.
        /// </summary>
        public class InternalSerilogConfig
        {
            public bool Enable { get; set; } = false;
            public string SerilogExceptionsFile { get; set; }
            public float RotateSizeMB { get; set; } = 0.5f;
            public int HeartBeatMilliSeconds { get; set; } = 30; // The rate (seconds) at which the rotation logic is checked for.
                                                                 // To keep the server healthy, try to make it at least 1 minute.
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0052:Remove unread private members", Justification = "<Pending>")]
        private static InternalSerilogExceptionsLogger _internalSerilogExceptionsLogger = null;
        public static void SetupLogger(IConfiguration configuration)
        {
            var maxResturtureDepth = configuration.GetSection("Serilog:MaxResturtureDepth").Get<int>();
            if( maxResturtureDepth == 0)
            {
                maxResturtureDepth = 10;        // fallback value
            }

            Log.Logger = new LoggerConfiguration()
                .Destructure.ToMaximumDepth(maxResturtureDepth)
                .ReadFrom.Configuration(configuration)
                  .Enrich.FromLogContext()
                  .Enrich.WithSpan(new SpanOptions(){IncludeTags = true})
                // .Enrich.WithEnvironmentName()
                // .Enrich.WithMachineName()
                .CreateLogger();

            try
            {
                var internalSerilogConfigurationSettings = configuration.GetSection("Serilog:Internal").Get<InternalSerilogConfig>();
                if (internalSerilogConfigurationSettings.Enable)
                {
                    _internalSerilogExceptionsLogger = new InternalSerilogExceptionsLogger(internalSerilogConfigurationSettings);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "missing Serilog internal settings. Serilog will fail silently (not recommended).");
            }
        }

        private sealed class InternalSerilogExceptionsLogger
        {
            private StreamWriter _streamWriter = null;
            private TextWriter _threadSafeWriter = null;
            private InternalSerilogConfig _internalSerilogConfig = null;
            private string _directoryName;
            private string _fileNameWithoutExtension;
            private string _extension;
            private readonly float _rotateSizeBytes;

            public InternalSerilogExceptionsLogger(InternalSerilogConfig configuration)
            {
                try
                {
                    _directoryName = Path.GetDirectoryName(configuration.SerilogExceptionsFile);
                    _fileNameWithoutExtension = Path.GetFileNameWithoutExtension(configuration.SerilogExceptionsFile);
                    _extension = Path.GetExtension(configuration.SerilogExceptionsFile);

                    _internalSerilogConfig = configuration;
                    _rotateSizeBytes = _internalSerilogConfig.RotateSizeMB * 1024 * 1024;
                    Setup();
                    Thread.Sleep(_internalSerilogConfig.HeartBeatMilliSeconds);
                    Task.Run(RotateIfNeeded);
                }
                catch (Exception ex)
                {
                    Serilog.Debugging.SelfLog.Disable();
                    Log.Error(ex, "failed to initialize internal serilog exception logger");
                }
            }

            private void Setup()
            {
                _streamWriter = File.CreateText(_internalSerilogConfig.SerilogExceptionsFile);
                _streamWriter.AutoFlush = true;
                _threadSafeWriter = TextWriter.Synchronized(_streamWriter);
                Serilog.Debugging.SelfLog.Enable(LogInternalSerilogException);
            }

            private async Task RotateIfNeeded()
            {
                await Task.Run(() =>
                {
                    if (_streamWriter.BaseStream.Length >= _rotateSizeBytes)
                    {
                        Rotate();
                        Setup();
                    }
                    Thread.Sleep(_internalSerilogConfig.HeartBeatMilliSeconds);
                    Task.Run(RotateIfNeeded);
                });
            }

            private void Rotate()
            {
                Serilog.Debugging.SelfLog.Disable();        // This important to prevent other threads from using _threadSafeWriter
                _threadSafeWriter.Flush();
                _threadSafeWriter.Close();
                var rotatedFileFullName = $"{_directoryName}/{_fileNameWithoutExtension}_{DateTime.Now:dd_MM_yy_h_m_s}{_extension}";
                File.Move(_internalSerilogConfig.SerilogExceptionsFile, rotatedFileFullName);
            }

            private void LogInternalSerilogException(string message)
            {
                _threadSafeWriter.WriteLine("==== exception start =======");
                _threadSafeWriter.WriteLine($"environment: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}");
                _threadSafeWriter.WriteLine($"reporter machine: {Environment.MachineName}");
                _threadSafeWriter.WriteLine($"message:\n{message}\n");
                _threadSafeWriter.WriteLine($"stacktrace:\n{Environment.StackTrace}");
                _threadSafeWriter.WriteLine("==== exception end =======\n");
                _threadSafeWriter.Flush();
            }
        }
    }
}