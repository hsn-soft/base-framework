using System.IO;
using System.Reflection;
using JetBrains.Annotations;

namespace HsnSoft.Base.VirtualFileSystem;

public interface IEmbeddedFileProviderQuery
{
    [CanBeNull]
    Stream Read<T>(string assemblySubFullPath);

    [CanBeNull]
    Stream Read(Assembly assembly, string assemblySubFullPath);

    [CanBeNull]
    Stream Read(string assemblyName, string assemblySubFullPath);
}