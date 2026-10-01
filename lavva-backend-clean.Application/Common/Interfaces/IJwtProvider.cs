using lavva_backend_clean.Domain.Entities.Users;

namespace lavva_backend_clean.Application.Common.Interfaces;

public interface IJwtProvider
{
    string Generate(User user);
}
