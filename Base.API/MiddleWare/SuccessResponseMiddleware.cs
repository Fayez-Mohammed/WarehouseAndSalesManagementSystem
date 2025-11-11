using Base.API.DTOs;
using System.Text.Json;

namespace Base.API.MiddleWare
{
    public class SuccessResponseMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SuccessResponseMiddleware> _logger;

        public SuccessResponseMiddleware(RequestDelegate next, ILogger<SuccessResponseMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // تخطي أي شيء يبدأ بـ /hangfire
            if (context.Request.Path.StartsWithSegments("/hangfire"))
            {
                await _next(context);
                return;
            }
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            try
            {
                await _next(context);
            }
            catch
            {
                context.Response.Body = originalBodyStream;
                throw;
            }

            responseBody.Seek(0, SeekOrigin.Begin);
            var bodyText = await new StreamReader(responseBody).ReadToEndAsync();

            // ✅ فقط لو 2xx
            if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
            {
                string traceId = context.TraceIdentifier ?? Guid.NewGuid().ToString();
                object? data = null;
                string? message = null;

                if (!string.IsNullOrWhiteSpace(bodyText))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(bodyText);
                        if (doc.RootElement.TryGetProperty("statusCode", out _))
                        {
                            var json = JsonSerializer.Deserialize<Dictionary<string, object>>(bodyText);
                            if (json != null)
                            {
                                if (!json.ContainsKey("traceId"))
                                    json["traceId"] = traceId;

                                var modifiedJson = JsonSerializer.Serialize(json,
                                    new JsonSerializerOptions
                                    {
                                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                                        WriteIndented = true
                                    });

                                context.Response.Body = originalBodyStream;
                                context.Response.ContentType = "application/json";
                                await context.Response.WriteAsync(modifiedJson);
                                return;
                            }
                        }
                        data = JsonSerializer.Deserialize<object>(bodyText);
                    }
                    catch
                    {
                        message = bodyText.Trim('"', '\'');
                    }
                }

                message ??= "Success";

                var apiResponse = new ApiResponseDTO(
                    statusCode: context.Response.StatusCode,
                    message: message,
                    data: data
                )
                {
                    TraceId = traceId
                };

                var jsonResponse = JsonSerializer.Serialize(apiResponse, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });

                context.Response.Body = originalBodyStream;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(jsonResponse);
            }
            else
            {
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }
        }
    }

}
