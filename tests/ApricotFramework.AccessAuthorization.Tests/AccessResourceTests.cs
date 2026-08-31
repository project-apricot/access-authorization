namespace ApricotFramework.AccessAuthorization.Tests;

public class AccessResourceTests
{
    private static Dictionary<string, object?> Attributes(params (string Key, object? Value)[] entries)
    {
        return entries.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);
    }

    [Fact]
    public void OfType_Always_HasNoInstanceId()
    {
        var resource = AccessResource.OfType("sales:order");

        Assert.Equal("sales:order", resource.Type);
        Assert.Null(resource.Id);
    }

    [Fact]
    public void Create_ExistingUrn_KeepsItVerbatim()
    {
        // An identifier the caller already has is stored as-is; the library mints no scheme of its own.
        var resource = AccessResource.Create("sales:order", "urn:acme:sales:order:42");

        Assert.Equal("urn:acme:sales:order:42", resource.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_MissingType_Throws(string? type)
    {
        Assert.ThrowsAny<ArgumentException>(() => AccessResource.Create(type!, "42"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_MissingId_Throws(string? id)
    {
        Assert.ThrowsAny<ArgumentException>(() => AccessResource.Create("sales:order", id!));
    }

    [Fact]
    public void Create_BlankAttributeName_Throws()
    {
        Assert.Throws<ArgumentException>(() => AccessResource.Create("sales:order", "42", Attributes((" ", "x"))));
    }

    [Fact]
    public void Attributes_KeepTheirDomainTypes()
    {
        // The reason these are loosely typed: a rule pattern-matches rather than parsing.
        var resource = AccessResource.Create("sales:order", "42", Attributes(
            ("ownerId", "u1"),
            ("isPublic", true),
            ("memberCount", 7),
            ("createdAt", new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero))));

        Assert.True(resource.Attributes["isPublic"] is true);
        Assert.True(resource.Attributes["memberCount"] is int count && count == 7);
        Assert.IsType<DateTimeOffset>(resource.Attributes["createdAt"]);
        Assert.IsType<string>(resource.Attributes["ownerId"]);
    }

    [Fact]
    public void Attributes_NullValue_IsPresentButUnset()
    {
        // Distinguishable from an absent attribute, which a rule may care about.
        var resource = AccessResource.Create("sales:order", "42", Attributes(("ownerId", null)));

        Assert.True(resource.Attributes.ContainsKey("ownerId"));
        Assert.Null(resource.Attributes["ownerId"]);
        Assert.False(resource.Attributes.ContainsKey("absent"));
    }

    [Fact]
    public void Attributes_MutatedAfterCreate_AreUnaffected()
    {
        var source = Attributes(("isPublic", true));
        var resource = AccessResource.Create("sales:order", "42", source);

        source["isPublic"] = false;

        Assert.True(resource.Attributes["isPublic"] is true);
    }

    [Fact]
    public void Equals_SameTypeAndId_IsTrue()
    {
        Assert.Equal(AccessResource.Create("sales:order", "42"), AccessResource.Create("sales:order", "42"));
    }

    [Fact]
    public void Equals_DifferingAttributes_IsStillTheSameObject()
    {
        // Attributes describe the object rather than identify it: a order reloaded with a changed
        // owner is still the same order.
        var first = AccessResource.Create("sales:order", "42", Attributes(("ownerId", "u1")));
        var second = AccessResource.Create("sales:order", "42", Attributes(("ownerId", "u2")));

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Equals_TypeAsWholeVersusInstance_IsFalse()
    {
        Assert.NotEqual(AccessResource.OfType("sales:order"), AccessResource.Create("sales:order", "42"));
    }

    [Fact]
    public void Equals_TypeAndIdSplitDifferently_IsFalse()
    {
        // Type and id are compared separately, so no delimiter shuffling makes two objects one.
        Assert.NotEqual(AccessResource.Create("t:4", "2"), AccessResource.Create("t", "4:2"));
    }

    [Fact]
    public void Equals_DifferingInCase_IsFalse()
    {
        Assert.NotEqual(AccessResource.Create("sales:order", "42"), AccessResource.Create("Content:Order", "42"));
    }

    [Fact]
    public void ToString_Always_IsReadable()
    {
        Assert.Equal("sales:order:42", AccessResource.Create("sales:order", "42").ToString());
        Assert.Equal("sales:order", AccessResource.OfType("sales:order").ToString());
    }
}
