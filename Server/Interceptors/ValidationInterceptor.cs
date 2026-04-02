using FluentValidation;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Server.Infrastructure.Validation;

namespace Server.Interceptors;

public class ValidationInterceptor(GrpcValidationPolicyRegistry policyRegistry) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        if (!policyRegistry.ShouldValidate(context.Method))
        {
            return await continuation(request, context);
        }

        var validator = context.GetHttpContext().RequestServices.GetService<IValidator<TRequest>>();
        if (validator is null)
        {
            return await continuation(request, context);
        }

        var validationResult = await validator.ValidateAsync(request, context.CancellationToken);
        if (!validationResult.IsValid)
        {
            var message = string.Join(" | ", validationResult.Errors.Select(error => error.ErrorMessage));
            throw new RpcException(new Status(StatusCode.InvalidArgument, message));
        }

        return await continuation(request, context);
    }
}
