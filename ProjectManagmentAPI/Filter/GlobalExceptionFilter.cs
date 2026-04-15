using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ProjectManagementAPI.DTO.Common;

namespace ProjectManagementAPI.Filter
{
    public class GlobalExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<GlobalExceptionFilter> _logger;

        public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
        {
            _logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            _logger.LogError(context.Exception, "Необработанное исключение: {Message}", context.Exception.Message);

            var response = ApiResponse<object>.Fail(
                "Внутренняя ошибка сервера. Пожалуйста, обратитесь к администратору.",
                new List<string> { context.Exception.Message },
                500
            );

            context.Result = new ObjectResult(response) { StatusCode = 500 };
            context.ExceptionHandled = true;
        }
    }
}