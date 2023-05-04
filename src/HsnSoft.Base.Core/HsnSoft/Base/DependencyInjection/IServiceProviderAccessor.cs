using System;

namespace HsnSoft.Base.DependencyInjection;

public interface IServiceProviderAccessor
{
    IServiceProvider ServiceProvider { get; }
}