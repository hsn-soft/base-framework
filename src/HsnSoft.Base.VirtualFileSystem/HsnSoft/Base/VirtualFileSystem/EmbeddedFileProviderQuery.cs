using System.IO;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.Extensions.FileProviders;

namespace HsnSoft.Base.VirtualFileSystem;

public class EmbeddedFileProviderQuery : IEmbeddedFileProviderQuery
{
    public Stream Read<T>(string assemblySubFullPath)
    {
        return ReadInternal(typeof(T).Assembly, assemblySubFullPath);
    }

    public Stream Read(Assembly assembly, string assemblySubFullPath)
    {
        return ReadInternal(assembly, assemblySubFullPath);
    }

    public Stream Read(string assemblyName, string assemblySubFullPath)
    {
        var assembly = Assembly.Load(assemblyName);
        return ReadInternal(assembly, assemblySubFullPath);
    }

    [CanBeNull]
    internal static Stream ReadInternal(Assembly assembly, string assemblySubFullPath)
    {
        var embeddedProvider = new EmbeddedFileProvider(assembly);
        return embeddedProvider.GetFileInfo(assemblySubFullPath).CreateReadStream();
    }
}