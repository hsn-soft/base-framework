namespace HsnSoft.Base.MultiTenancy.Tests;

public class TenantAccessPathTests
{
    private static readonly Guid s_d = new("00000000-0000-7000-8000-0000000000d4");
    private static readonly Guid s_c = new("00000000-0000-7000-8000-0000000000c3");
    private static readonly Guid s_b = new("00000000-0000-7000-8000-0000000000b2");

    [Fact]
    public void ThePath_IsTheIdsFromTheRoot_InSmallHexDigits_EachEndingWithASlash()
    {
        string root = TenantAccessPath.Root(s_d);
        string child = TenantAccessPath.Child(root, s_c);
        string grandchild = TenantAccessPath.Child(child, s_b);

        Assert.Equal("/00000000000070008000" + "0000000000d4/", root);
        Assert.Equal($"/{s_d:N}/{s_c:N}/", child);
        Assert.Equal($"/{s_d:N}/{s_c:N}/{s_b:N}/", grandchild);
        Assert.Equal(grandchild, grandchild.ToLowerInvariant());
        Assert.Equal(TenantAccessPath.LengthFor(3), grandchild.Length);
    }

    [Fact]
    public void ATenant_IsSelfOrUnderItsAncestors_AndNotUnderItsSiblingsOrChildren()
    {
        string d = TenantAccessPath.Root(s_d);
        string c = TenantAccessPath.Child(d, s_c);
        string b = TenantAccessPath.Child(c, s_b);

        Assert.True(TenantAccessPath.IsSelfOrDescendantOf(b, b));
        Assert.True(TenantAccessPath.IsSelfOrDescendantOf(b, c));
        Assert.True(TenantAccessPath.IsSelfOrDescendantOf(b, d));
        Assert.True(TenantAccessPath.IsSelfOrDescendantOf(c, d));
        Assert.False(TenantAccessPath.IsSelfOrDescendantOf(c, b));   // a tenant does not see the one above it
        Assert.False(TenantAccessPath.IsSelfOrDescendantOf(TenantAccessPath.Root(s_c), d));
    }

    [Fact]
    public void ThePathOfOneTenant_NeverStartsThePathOfAnotherWhoseIdBeginsTheSame()
    {
        // the ids are of the same length and each ends with a slash: the way a path of names is wrong ("/A/CC" starts "/A/CCD") cannot happen
        Guid first = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        Guid second = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaab");

        Assert.False(TenantAccessPath.IsSelfOrDescendantOf(TenantAccessPath.Root(second), TenantAccessPath.Root(first)));
    }

    [Fact]
    public void TheIdsAndTheParent_AreReadFromThePath()
    {
        string path = TenantAccessPath.Child(TenantAccessPath.Child(TenantAccessPath.Root(s_d), s_c), s_b);

        Assert.Equal([s_d, s_c, s_b], TenantAccessPath.Ids(path));
        Assert.Equal(TenantAccessPath.Child(TenantAccessPath.Root(s_d), s_c), TenantAccessPath.Parent(path));
        Assert.Null(TenantAccessPath.Parent(TenantAccessPath.Root(s_d)));
    }

    [Fact]
    public void AMovedTenant_AndEverythingUnderIt_TakeTheNewStartOfThePath()
    {
        string oldParent = TenantAccessPath.Root(s_d);
        string newParent = TenantAccessPath.Root(s_b);
        string c = TenantAccessPath.Child(oldParent, s_c);
        string e = TenantAccessPath.Child(c, Guid.Parse("00000000-0000-7000-8000-0000000000e5"));

        Assert.Equal(TenantAccessPath.Child(newParent, s_c), TenantAccessPath.Move(c, oldParent, newParent));
        Assert.Equal(newParent + e[oldParent.Length..], TenantAccessPath.Move(e, oldParent, newParent));
        Assert.Throws<ArgumentException>(() => TenantAccessPath.Move(TenantAccessPath.Root(s_b), oldParent, newParent));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("/")]
    [InlineData("/A/CC/")]
    [InlineData("00000000000070008000000000000d4/")]
    [InlineData("/00000000-0000-7000-8000-0000000000d4/")]
    [InlineData("/00000000000070008000000000000D4/")]
    [InlineData("/0000000000007000800000000000d4/")]
    public void WhatIsNotAPath_IsNotValid(string? path)
    {
        Assert.False(TenantAccessPath.IsValid(path));
    }

    [Fact]
    public void WhatIsNotAPath_IsRefusedWhereAPathIsNeeded()
    {
        Assert.Throws<ArgumentException>(() => TenantAccessPath.Child("/A/", s_c));
        Assert.Throws<ArgumentException>(() => TenantAccessPath.Ids("/A/"));
        Assert.Throws<ArgumentException>(() => TenantAccessPath.Parent("x"));
        Assert.Throws<ArgumentNullException>(() => TenantAccessPath.IsSelfOrDescendantOf(null!, "/"));
        Assert.Throws<ArgumentNullException>(() => TenantAccessPath.IsSelfOrDescendantOf("/", null!));
    }
}
