namespace IndustrialBackendDemo.Application.Common.Results;

public sealed record ApplicationError(string Code, string Description)
{
    public static ApplicationError None { get; } = new(string.Empty, string.Empty);
}
