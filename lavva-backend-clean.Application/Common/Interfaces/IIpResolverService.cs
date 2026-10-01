namespace lavva_backend_clean.Application.Common.Interfaces;

public interface IIpResolverService
{
    string? GetClientIpAddress();
    bool IsValidPublicIp(string? ipAddress);
}
