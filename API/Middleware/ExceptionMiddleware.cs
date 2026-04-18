using BuildingBlocks;
using Notes.Domain.Exceptions;
using Serilog;

namespace API.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (FluentValidation.ValidationException ex)
            {
                Log.Warning(ex,
                    "Validation failed. Path: {Path}, Method: {Method}",
                    context.Request.Path,
                    context.Request.Method);

                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.ContentType = "application/json";

                var errors = ex.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}").ToList();
                var response = ApiResponse<object>.ErrorResponse(errors, "Validation Failed");

                await context.Response.WriteAsJsonAsync(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                Log.Warning(ex,
                    "Unauthorized access. Path: {Path}, Method: {Method}",
                    context.Request.Path,
                    context.Request.Method);

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";

                var response = ApiResponse<object>.ErrorResponse(ex.Message, "Unauthorized");
                await context.Response.WriteAsJsonAsync(response);
            }
            catch (NoteNotFoundException ex)
            {
                Log.Warning(ex,
                    "Resource not found. Path: {Path}, Method: {Method}",
                    context.Request.Path,
                    context.Request.Method);

                context.Response.StatusCode = StatusCodes.Status404NotFound;
                context.Response.ContentType = "application/json";

                var response = ApiResponse<object>.ErrorResponse(ex.Message, "Not Found");
                await context.Response.WriteAsJsonAsync(response);
            }
            catch (InvalidOperationException ex)
            {
                Log.Warning(ex,
                    "Invalid operation. Path: {Path}, Method: {Method}",
                    context.Request.Path,
                    context.Request.Method);

                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.ContentType = "application/json";

                var response = ApiResponse<object>.ErrorResponse(ex.Message, "Bad Request");
                await context.Response.WriteAsJsonAsync(response);
            }
            catch (Exception ex)
            {
                Log.Error(ex,
                    "Unhandled exception. Path: {Path}, Method: {Method}",
                    context.Request.Path,
                    context.Request.Method);

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";

                // In development, show actual error details for debugging
                var errorMessage = "An internal server error occurred";
                if (context.RequestServices.GetService<IWebHostEnvironment>()?.IsDevelopment() == true)
                {
                    errorMessage = ex.Message; // Show actual error in dev
                }

                var response = ApiResponse<object>.ErrorResponse(
                    errorMessage,
                    "Internal Server Error");

                await context.Response.WriteAsJsonAsync(response);
            }
        }
    }
}
