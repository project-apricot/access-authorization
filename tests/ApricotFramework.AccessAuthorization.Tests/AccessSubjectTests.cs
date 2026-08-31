namespace ApricotFramework.AccessAuthorization.Tests;

public class AccessSubjectTests
{
    [Fact]
    public void Create_IdOnly_HasNoAttributes()
    {
        var subject = AccessSubject.Create("u1");

        Assert.Equal("u1", subject.Id);
        Assert.Empty(subject.Attributes);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_MissingId_Throws(string? id)
    {
        Assert.ThrowsAny<ArgumentException>(() => AccessSubject.Create(id!));
    }

    [Fact]
    public void Create_BlankAttributeName_Throws()
    {
        Assert.Throws<ArgumentException>(() => AccessSubject.Create("u1", new Dictionary<string, string> { [" "] = "acme" }));
    }

    [Fact]
    public void Create_NullAttributeValue_Throws()
    {
        Assert.Throws<ArgumentException>(() => AccessSubject.Create("u1", new Dictionary<string, string> { ["organization"] = null! }));
    }

    [Fact]
    public void Attributes_MutatedAfterCreate_AreUnaffected()
    {
        var source = new Dictionary<string, string> { ["organization"] = "acme" };
        var subject = AccessSubject.Create("u1", source);

        source["organization"] = "other";

        Assert.Equal("acme", subject.Attributes["organization"]);
    }

    [Fact]
    public void Equals_SameAttributesInDifferentOrder_IsTrue()
    {
        var first = AccessSubject.Create("u1", new Dictionary<string, string> { ["a"] = "1", ["b"] = "2" });
        var second = AccessSubject.Create("u1", new Dictionary<string, string> { ["b"] = "2", ["a"] = "1" });

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentAttributeValue_IsFalse()
    {
        var first = AccessSubject.Create("u1", new Dictionary<string, string> { ["organization"] = "acme" });
        var second = AccessSubject.Create("u1", new Dictionary<string, string> { ["organization"] = "other" });

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Equals_DifferentCaseId_IsFalse()
    {
        Assert.NotEqual(AccessSubject.Create("u1"), AccessSubject.Create("U1"));
    }

    [Fact]
    public void GetKey_AdversarialInputs_NeverCollide()
    {
        // The key is a cache key, so a collision between two distinct subjects is a privilege
        // escalation: one subject would read the other's cached grants.
        var subjects = new List<AccessSubject>
        {
            AccessSubject.Create("a"),
            AccessSubject.Create("a:b"),
            AccessSubject.Create("a|b"),
            AccessSubject.Create("1:a"),
            AccessSubject.Create("a", new Dictionary<string, string> { ["b"] = string.Empty }),
            AccessSubject.Create("a", new Dictionary<string, string> { ["b"] = "c" }),
            AccessSubject.Create("a", new Dictionary<string, string> { ["bc"] = string.Empty }),
            AccessSubject.Create("a", new Dictionary<string, string> { ["b"] = string.Empty, ["c"] = string.Empty }),
            AccessSubject.Create("ab", new Dictionary<string, string> { ["c"] = string.Empty }),
            AccessSubject.Create("a1:1:b1:c"),
            AccessSubject.Create("a", new Dictionary<string, string> { ["1:b1:c"] = string.Empty }),
            AccessSubject.Create("a", new Dictionary<string, string> { ["b"] = "c:d" }),
            AccessSubject.Create("a", new Dictionary<string, string> { ["b:c"] = "d" }),
            AccessSubject.Create("añ"),
            AccessSubject.Create("a b"),
        };

        var keys = subjects.Select(subject => subject.GetKey()).ToList();

        Assert.Equal(keys.Count, keys.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void GetKey_LargeAttributeBag_IsStableAcrossCalls()
    {
        var attributes = Enumerable.Range(0, 500).ToDictionary(index => $"k{index}", index => $"v{index}");
        var subject = AccessSubject.Create("u1", attributes);

        Assert.Equal(subject.GetKey(), subject.GetKey());
        Assert.Equal(subject, AccessSubject.Create("u1", attributes));
    }
}
