namespace ApricotFramework.AccessAuthorization.Tests;

public class AccessContextTests
{
    private static readonly AccessSubject Subject = AccessSubject.Create("u1");

    [Fact]
    public void For_SubjectOnly_HasNoResourceAndAnEmptyEnvironment()
    {
        var context = AccessContext.For(Subject);

        Assert.Null(context.Resource);
        Assert.Same(AccessEnvironment.Empty, context.Environment);
    }

    [Fact]
    public void For_NullEnvironment_IsTheEmptyOne()
    {
        // Never null, so a rule reads context.Environment.Attributes without a guard.
        var context = AccessContext.For(Subject, null, null);

        Assert.Same(AccessEnvironment.Empty, context.Environment);
    }

    [Fact]
    public void For_Environment_KeepsItsAttributeTypes()
    {
        var environment = AccessEnvironment.For(new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["ipAddress"] = "203.0.113.7",
            ["duringBusinessHours"] = false,
        });

        var context = AccessContext.For(Subject, null, environment);

        Assert.Equal("203.0.113.7", context.Environment.Attributes["ipAddress"]);
        Assert.True(context.Environment.Attributes["duringBusinessHours"] is false);
    }

    [Fact]
    public void For_NullSubject_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => AccessContext.For(null!));
    }

    [Fact]
    public void Empty_Always_CarriesNothing()
    {
        Assert.Empty(AccessEnvironment.Empty.Attributes);
        Assert.Same(AccessEnvironment.Empty, AccessEnvironment.For(null));
        Assert.Same(AccessEnvironment.Empty, AccessEnvironment.For(new Dictionary<string, object?>()));
    }

    [Fact]
    public void For_BlankAttributeName_Throws()
    {
        Assert.Throws<ArgumentException>(() => AccessEnvironment.For(new Dictionary<string, object?> { [" "] = "x" }));
    }
}
