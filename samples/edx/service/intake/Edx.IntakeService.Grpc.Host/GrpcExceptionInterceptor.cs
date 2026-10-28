using Grpc.Core;
using Grpc.Core.Interceptors;
using HsnSoft.Base;

namespace Edx.IntakeService.Grpc.Host;

/// <summary>
/// Says what went wrong in the language of gRPC, as the exception handler of the web api does in the language of HTTP: a rule the client broke is <c>InvalidArgument</c> (the code is in the trailer <c>error-code</c>), what is not there is
/// <c>NotFound</c>, and anything else is <c>Internal</c> with the trace id and nothing of the cause. A known error is a Warning in the logs, an unknown one an Error, each once.
/// </summary>
public sealed class GrpcExceptionInterceptor(ILogger<GrpcExceptionInterceptor> logger) : Interceptor
{
    public const string ErrorCodeTrailer = "error-code";

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            return await continuation(request, context).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not RpcException)
        {
            throw Translate(exception, context);
        }
    }

    public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, ServerCallContext context, ClientStreamingServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            return await continuation(requestStream, context).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not RpcException)
        {
            throw Translate(exception, context);
        }
    }

    private RpcException Translate(Exception exception, ServerCallContext context)
    {
        switch (exception)
        {
            case BusinessException business:
                logger.LogWarning("gRPC {GrpcMethod} refused: {ErrorCode} {ErrorMessage}", context.Method, business.ErrorCode, business.Message);
                return Fail(StatusCode.InvalidArgument, business.Message, business.ErrorCode);
            case BusinessRuleException rule:
                logger.LogWarning("gRPC {GrpcMethod} refused: {ErrorMessage}", context.Method, rule.Message);
                return Fail(StatusCode.InvalidArgument, rule.Message, rule.MessageKey);
            case BaseHttpException refused when refused.HttpStatusCode == StatusCodes.Status403Forbidden:
                logger.LogWarning("gRPC {GrpcMethod} refused: {ErrorMessage}", context.Method, refused.Message);
                return Fail(StatusCode.PermissionDenied, refused.Message, refused.MessageKey);
            case NotFoundException notFound:
                logger.LogWarning("gRPC {GrpcMethod} found nothing: {ErrorMessage}", context.Method, notFound.Message);
                return Fail(StatusCode.NotFound, notFound.Message, notFound.MessageKey);
            case OperationCanceledException:
                return Fail(StatusCode.Cancelled, "The call was cancelled.", null);
            default:
                string trace = context.GetHttpContext().TraceIdentifier;
                logger.LogError(exception, "gRPC {GrpcMethod} failed ({TraceId})", context.Method, trace);
                return Fail(StatusCode.Internal, $"The call failed. Trace id: {trace}", "internal_error");
        }
    }

    private static RpcException Fail(StatusCode status, string message, string? code)
        => new(new Status(status, message), code is null ? [] : new Metadata { { ErrorCodeTrailer, code } });
}
