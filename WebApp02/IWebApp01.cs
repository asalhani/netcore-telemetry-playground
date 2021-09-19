﻿using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Common.Models;
using Refit;

namespace WebApp02
{
    public interface IWebApp01
    {
        [Get("/WeatherForecast")]
         Task<List<WeatherForecast>> GetAll();
        // Task<ApiResponse<List<WeatherForecast>>> GetAll();

        [Post("/WeatherForecast/postRequest")]
        Task PostRequest(PostRequestParam param, [Header("adib-header")] string headerValue);
        
        [Post("/WeatherForecast/exception")]
         Task ExceptionRequest(PostRequestParam param, [Header("test-header")] string headerValue);
        // Task<HttpResponseMessage> ExceptionRequest(PostRequestParam param, [Header("test-header")] string headerValue);
        
    }
}