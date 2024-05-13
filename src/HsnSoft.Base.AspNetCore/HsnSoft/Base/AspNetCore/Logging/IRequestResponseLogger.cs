using HsnSoft.Base.Logging;

namespace HsnSoft.Base.AspNetCore.Logging;

public interface IRequestResponseLogger<in T> : IPersistentLogger<T> where T : IRequestResponseLog
{
}