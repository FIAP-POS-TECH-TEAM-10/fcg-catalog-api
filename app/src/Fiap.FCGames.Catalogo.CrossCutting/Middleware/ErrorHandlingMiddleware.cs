using FluentValidation;
using Fiap.FCGames.Catalogo.Domain.Exception;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fiap.FCGames.Catalogo.CrossCutting.Middleware
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            if (exception is ValidationException ve)
            {
                _logger.LogWarning("Erro de validação na requisição: {Fields}", ve.Errors.Select(e => e.PropertyName));
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

                var validationResponse = new
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Errors = ve.Errors.Select(e => new
                    {
                        Field = e.PropertyName,
                        Message = e.ErrorMessage
                    })
                };

                return context.Response.WriteAsync(JsonSerializer.Serialize(validationResponse, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }));
            }

            if (exception is LoginException le)
            {
                _logger.LogWarning(le, "Erro de login na requisição");
                context.Response.StatusCode = le.StatusCode;

                var errorResponse = new ErrorResponse
                {
                    StatusCode = le.StatusCode,
                    Message = le.Message
                };

                return context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }));
            }

            if (exception is NotFoundException nfe)
            {
                _logger.LogWarning(nfe, "Recurso não encontrado");
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;

                var errorResponse = new ErrorResponse
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = nfe.Message
                };

                return context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }));
            }

            if (exception is BusinessException be)
            {
                _logger.LogWarning(be, "Erro de negócio na requisição");
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

                var errorResponse = new ErrorResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Message = be.Message
                };

                return context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }));
            }

            if (exception is ConflictException ce)
            {
                _logger.LogWarning(ce, "Conflito detectado na requisição");
                context.Response.StatusCode = (int)HttpStatusCode.Conflict;

                var errorResponse = new ErrorResponse
                {
                    StatusCode = (int)HttpStatusCode.Conflict,
                    Message = ce.Message
                };

                return context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }));
            }

            _logger.LogError(exception, "Ocorreu um erro não tratado na requisição: {Message}", exception.Message);
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = new ErrorResponse
            {
                StatusCode = context.Response.StatusCode,
                Message = "Ocorreu um erro interno no servidor. Por favor, tente novamente mais tarde."
            };

            return context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
        }
    }

    public class ErrorResponse
    {
        public int StatusCode { get; set; }
        public required string Message { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? DetailedMessage { get; set; }
    }
}
