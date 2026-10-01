using System;

namespace lavva_backend_clean.Application.Common.Interfaces;

public interface ISsoJtiValidator
{
    bool TryConsume(string jti, TimeSpan duration);
}
