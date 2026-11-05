namespace HsnSoft.Base.BlobStoring.FileSystem;

/// <summary>Settings of the blob store on the local disk; section <c>FileSystem</c>.</summary>
public sealed class FileSystemBlobOptions
{
    public const string SectionName = "FileSystem";

    /// <summary>The folder every container is a folder of. Required and absolute (<c>/var/lib/app/blobs</c>); it is created when it does not exist. Nothing is ever written outside of it.</summary>
    public string RootPath { get; set; } = string.Empty;
}
