using System.IO;
using System.Reflection;
using JetBrains.Annotations;

namespace HsnSoft.Base.VirtualFileSystem;

public class EmbeddedResourceQuery : IEmbeddedResourceQuery
{
    public Stream Read<T>(string resource)
    {
        var assembly = typeof(T).Assembly;
        var resourceNamespace = typeof(T).Namespace;
        return ReadInternal(assembly, resourceNamespace, resource);
    }

    public Stream Read(Assembly assembly, string resource, string resourceNamespace = null)
    {
        if (string.IsNullOrWhiteSpace(resourceNamespace))
        {
            resourceNamespace = assembly.GetName().Name;
        }

        return ReadInternal(assembly, resourceNamespace, resource);
    }

    public Stream Read(string assemblyName, string resource, string resourceNamespace = null)
    {
        var assembly = Assembly.Load(assemblyName);
        if (string.IsNullOrWhiteSpace(resourceNamespace))
        {
            resourceNamespace = assembly.GetName().Name;
        }

        return ReadInternal(assembly, resourceNamespace, resource);
    }

    [CanBeNull]
    internal static Stream ReadInternal(Assembly assembly, string resourceNamespace, string resourceInfo)
    {
        return assembly.GetManifestResourceStream($"{resourceNamespace}.{resourceInfo}");
    }
}