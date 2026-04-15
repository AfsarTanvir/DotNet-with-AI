using MediatR;
using Serilog;
using System.Diagnostics;

namespace Notes.Application.Behaviors
{
    public class LoggingBehavior<TRequest, TResponse>
        : IPipelineBehavior<TRequest, TResponse>
    {
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;

            Log.Information("Handling {RequestName}", requestName);

            var stopwatch = Stopwatch.StartNew();
            var response = await next(cancellationToken);
            Log.Information("Handled {RequestName} in {ElapsedMilliseconds}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);

            return response;
        }
    }
}
