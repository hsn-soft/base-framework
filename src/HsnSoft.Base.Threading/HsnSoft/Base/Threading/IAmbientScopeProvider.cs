using System;

namespace HsnSoft.Base.Threading;

public interface IAmbientScopeProvider<T>
{
    T GetValue(string contextKey);

    IDisposable BeginScope(string contextKey, T value);
}