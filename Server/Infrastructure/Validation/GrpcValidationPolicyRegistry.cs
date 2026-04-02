namespace Server.Infrastructure.Validation;

public class GrpcValidationPolicyRegistry
{
    private readonly HashSet<string> validatedMethodPrefixes = new(StringComparer.Ordinal);

    public void RequireValidationForPrefix(string methodPrefix)
    {
        validatedMethodPrefixes.Add(methodPrefix);
    }

    public bool ShouldValidate(string method)
    {
        return validatedMethodPrefixes.Any(method.StartsWith);
    }
}
