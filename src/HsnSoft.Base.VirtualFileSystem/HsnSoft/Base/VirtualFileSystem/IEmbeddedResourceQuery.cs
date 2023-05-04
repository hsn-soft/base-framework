using System.IO;
using System.Reflection;
using JetBrains.Annotations;

namespace HsnSoft.Base.VirtualFileSystem;

public interface IEmbeddedResourceQuery
{
    [CanBeNull]
    Stream Read<T>(string resource);

    [CanBeNull]
    Stream Read(Assembly assembly, string resource, string resourceNamespace = null);

    [CanBeNull]
    Stream Read(string assemblyName, string resource, string resourceNamespace = null);
}