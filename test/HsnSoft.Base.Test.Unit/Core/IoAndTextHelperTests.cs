using System.Text;
using FluentAssertions;
using HsnSoft.Base.Collections;
using HsnSoft.Base.Communication;
using HsnSoft.Base.Content;
using HsnSoft.Base.DependencyInjection;
using HsnSoft.Base.Internal;
using HsnSoft.Base.IO;
using HsnSoft.Base.Text;

namespace HsnSoft.Base.Test.Unit.Core;

public class IoAndTextHelperTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "hsn-base-" + Guid.NewGuid().ToString("N"));

    public IoAndTextHelperTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    [Fact]
    public void DirectoryHelper_CreateAndDelete_Work()
    {
        var dir = Path.Combine(_root, "a");
        DirectoryHelper.CreateIfNotExists(dir);
        DirectoryHelper.CreateIfNotExists(dir);
        Directory.Exists(dir).Should().BeTrue();

        var info = new DirectoryInfo(Path.Combine(_root, "b"));
        DirectoryHelper.CreateIfNotExists(info);
        DirectoryHelper.CreateIfNotExists(info);
        info.Exists.Should().BeTrue();

        DirectoryHelper.DeleteIfExists(dir);
        DirectoryHelper.DeleteIfExists(dir);
        Directory.Exists(dir).Should().BeFalse();

        var nested = Path.Combine(_root, "c", "d");
        Directory.CreateDirectory(nested);
        DirectoryHelper.DeleteIfExists(Path.Combine(_root, "c"), true);
        DirectoryHelper.DeleteIfExists(Path.Combine(_root, "c"), true);
        Directory.Exists(Path.Combine(_root, "c")).Should().BeFalse();
    }

    [Fact]
    public void DirectoryHelper_IsSubDirectoryOf_Works()
    {
        DirectoryHelper.IsSubDirectoryOf(_root, Path.Combine(_root, "x", "y")).Should().BeTrue();
        DirectoryHelper.IsSubDirectoryOf(_root, _root).Should().BeTrue();
        DirectoryHelper.IsSubDirectoryOf(Path.Combine(_root, "x"), _root).Should().BeFalse();
        DirectoryHelper.IsSubDirectoryOf(_root, Path.GetPathRoot(_root)!).Should().BeFalse();
        FluentActions.Invoking(() => DirectoryHelper.IsSubDirectoryOf((string)null, _root)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => DirectoryHelper.IsSubDirectoryOf(_root, (string)null)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => DirectoryHelper.IsSubDirectoryOf((DirectoryInfo)null, new DirectoryInfo(_root))).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => DirectoryHelper.IsSubDirectoryOf(new DirectoryInfo(_root), (DirectoryInfo)null)).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void DirectoryHelper_ChangeCurrentDirectory_Works()
    {
        var original = Directory.GetCurrentDirectory();
        try
        {
            ReferenceEquals(DirectoryHelper.ChangeCurrentDirectory(original), NullDisposable.Instance).Should().BeTrue();
            using (DirectoryHelper.ChangeCurrentDirectory(_root))
            {
                Directory.GetCurrentDirectory().Should().EndWith(Path.GetFileName(_root));
            }

            Directory.GetCurrentDirectory().Should().Be(original);
        }
        finally
        {
            Directory.SetCurrentDirectory(original);
        }
    }

    [Fact]
    public async Task FileHelper_Works()
    {
        var file = Path.Combine(_root, "f.txt");
        FileHelper.DeleteIfExists(file).Should().BeFalse();
        await File.WriteAllTextAsync(file, "l1\nl2");
        (await FileHelper.ReadAllTextAsync(file)).Should().Be("l1\nl2");
        (await FileHelper.ReadAllBytesAsync(file)).Should().Equal(Encoding.UTF8.GetBytes("l1\nl2"));
        (await FileHelper.ReadAllLinesAsync(file)).Should().Equal("l1", "l2");
        (await FileHelper.ReadAllLinesAsync(file, Encoding.ASCII)).Should().Equal("l1", "l2");
        FileHelper.DeleteIfExists(file).Should().BeTrue();

        var bom = Path.Combine(_root, "bom.txt");
        await File.WriteAllBytesAsync(bom, new byte[] { 0xEF, 0xBB, 0xBF, (byte)'h', (byte)'i' });
        (await FileHelper.ReadFileWithoutBomAsync(bom)).Should().Be("hi");

        FileHelper.GetExtension("a.b.txt").Should().Be("txt");
        FileHelper.GetExtension("noext").Should().BeNull();
        FluentActions.Invoking(() => FileHelper.GetExtension(null)).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void StringHelper_Works()
    {
        StringHelper.ConvertFromBytesWithoutBom(null).Should().BeNull();
        StringHelper.ConvertFromBytesWithoutBom(Encoding.UTF8.GetBytes("abc")).Should().Be("abc");
        StringHelper.ConvertFromBytesWithoutBom(new byte[] { 0xEF, 0xBB, 0xBF, 65 }, Encoding.UTF8).Should().Be("A");
        StringHelper.ConvertFromBytesWithoutBom(new byte[] { 1 }).Should().Be("\u0001");

        StringHelper.ReplaceInvalidChars(" ").Should().BeEmpty();
        StringHelper.ReplaceInvalidChars("a b-c.d_e1").Should().Be("ab-c.d_e1");
        StringHelper.ReplaceInvalidChars("a b", false, "x").Should().Be("axb");
        StringHelper.ReplaceInvalidChars("a+b@c", true).Should().Be("a+b@c");
        StringHelper.ReplaceInvalidChars("a+b@c", false).Should().Be("abc");

        StringHelper.FirstCharCapitalize(null).Should().BeEmpty();
        StringHelper.FirstCharCapitalize("hello-world_foo").Should().Be("Hello World Foo");
        StringHelper.FirstCharCapitalize("a b").Should().Be("A B");
        StringHelper.FirstCharCapitalize("a  b", new[] { "," }).Should().Be("A B");
        StringHelper.FirstCharCapitalize("x,yy", new[] { "," }).Should().Be("X Yy");

        StringHelper.SplitFirstValue(null, ",").Should().BeNull();
        StringHelper.SplitFirstValue("a,b", " ").Should().Be("a,b");
        StringHelper.SplitFirstValue("a,b", ",").Should().Be("a");
        StringHelper.SplitFirstValue("ab", ",").Should().Be("ab");

        StringHelper.Base64Decode(StringHelper.Base64Encode("héllo")).Should().Be("héllo");
        StringHelper.Normalize(null).Should().BeEmpty();
        StringHelper.Normalize(" çaĞ ").Should().Be("CAG");
        StringHelper.Minimize(null).Should().BeEmpty();
        StringHelper.Minimize(" ÇaĞ ").Should().Be("cag");
        StringHelper.SlugKeyNormalize("Hello World!").Should().Be("hello_world");
        StringHelper.SlugKeyNormalize("Hello World", '-').Should().Be("hello-world");
    }

    [Fact]
    public void Utf8Helper_Works()
    {
        Utf8Helper.ReadStringFromStream(new MemoryStream(new byte[] { 0xEF, 0xBB, 0xBF, 65 })).Should().Be("A");
        Utf8Helper.ReadStringFromStream(new MemoryStream(new byte[] { 65, 66 })).Should().Be("AB");
        Utf8Helper.ReadStringFromStream(new MemoryStream(new byte[] { 0xEF, 0x00, 0x00, 65 })).Should().Be("�\0\0A");
    }

    private class Base { }
    private class Derived : Base { }

    [Fact]
    public void TypeList_Works()
    {
        var list = new TypeList<Base>();
        list.Add<Base>();
        list.TryAdd<Base>().Should().BeFalse();
        list.TryAdd<Derived>().Should().BeTrue();
        list.Count.Should().Be(2);
        list.IsReadOnly.Should().BeFalse();
        list.Contains<Derived>().Should().BeTrue();
        list.Contains(typeof(Derived)).Should().BeTrue();
        list.IndexOf(typeof(Derived)).Should().Be(1);
        list[0].Should().Be(typeof(Base));
        list[0] = typeof(Derived);
        list[0].Should().Be(typeof(Derived));
        list.Add(typeof(Base));
        list.Insert(0, typeof(Base));
        list.Count.Should().Be(4);
        list.Remove<Derived>();
        list.Remove(typeof(Base)).Should().BeTrue();
        list.RemoveAt(0);
        var copy = new Type[list.Count];
        list.CopyTo(copy, 0);
        copy.Should().Equal(list);
        ((System.Collections.IEnumerable)list).GetEnumerator().Should().NotBeNull();
        list.Clear();
        list.Should().BeEmpty();

        FluentActions.Invoking(() => list.Add(typeof(string))).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => list.Insert(0, typeof(string))).Should().Throw<ArgumentException>();
        list.Add<Base>();
        FluentActions.Invoking(() => list[0] = typeof(string)).Should().Throw<ArgumentException>();

        var untyped = new TypeList();
        untyped.Add<string>();
        untyped.Should().Equal(typeof(string));
    }

    [Fact]
    public void BaseResponse_Works()
    {
        var r = new BaseResponse { StatusCode = 200, TraceId = "t", ErrorCode = "e" };
        r.StatusMessages.Add("a");
        r.StatusMessages.Add("b");
        r.StatusMessagesToSingleMessage().Should().Be("a, b");
        r.StatusCode.Should().Be(200);
        r.TraceId.Should().Be("t");
        r.ErrorCode.Should().Be("e");
        new BaseResponse<int> { Payload = 5 }.Payload.Should().Be(5);
    }

    [Fact]
    public void RemoteStreamContent_Works()
    {
        var ms = new MemoryStream(new byte[] { 1, 2, 3 });
        ms.Position = 1;
        var c = new RemoteStreamContent(ms, "f.bin", "text/plain");
        c.FileName.Should().Be("f.bin");
        c.ContentType.Should().Be("text/plain");
        c.ContentLength.Should().Be(2);
        c.GetStream().Should().BeSameAs(ms);
        c.Dispose();
        c.Dispose();
        ms.CanRead.Should().BeFalse();

        var ms2 = new MemoryStream();
        var c2 = new RemoteStreamContent(ms2, readOnlyLength: 9, disposeStream: false);
        c2.ContentType.Should().Be("application/octet-stream");
        c2.ContentLength.Should().Be(9);
        c2.Dispose();
        ms2.CanRead.Should().BeTrue();

        new RemoteStreamContent(new NoSeekStream()).ContentLength.Should().BeNull();
    }

    private sealed class NoSeekStream : MemoryStream
    {
        public override bool CanSeek => false;
    }

    [Fact]
    public void ObjectAccessor_Works()
    {
        new ObjectAccessor<string>().Value.Should().BeNull();
        var a = new ObjectAccessor<string>("x");
        a.Value.Should().Be("x");
        a.Value = "y";
        a.Value.Should().Be("y");
    }
}
