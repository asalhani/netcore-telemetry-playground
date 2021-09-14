using System.Collections.Generic;
using System.Security.Permissions;

namespace Common
{
    public class TelemetryConfiguration
    {
        public bool Enabled { get; set; }

        public HttpClientInstrumentationConfiguration
            HttpClientInstrumentationConfiguration { get; set; }
    }

    public class HttpClientInstrumentationConfiguration
    {
        public bool Enabled { get; set; } = true;
        public bool TagRequestBody { get; set; } = true;
        public bool TagResponseBody { get; set; } = true;
        public HttpHeadersOption TagRequestHeadersOption { get; set; }
        public HttpHeadersOption TagResponseHeadersOption { get; set; }
    }

    public class HttpHeadersOption
    {
        public bool Enabled { get; set; } = true;
        public List<string> ExcludedHeaders { get; set; }
    }
}