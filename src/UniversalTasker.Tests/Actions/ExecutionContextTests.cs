using UniversalTasker.Core.Actions;

namespace UniversalTasker.Tests.Actions;

public class ExecutionContextTests
{
    [Fact]
    public void Constructor_InitializesBuiltInVariables()
    {
        var context = new ExecutionContext();

        Assert.True(context.Variables.ContainsKey("timestamp"));
        Assert.IsType<DateTime>(context.Variables["timestamp"]);
    }

    [Fact]
    public void Variables_CanBeReadAndWritten()
    {
        var context = new ExecutionContext();
        context.Variables["x"] = 42;

        Assert.Equal(42, context.Variables["x"]);
    }

    [Fact]
    public void Constructor_AcceptsExistingVariables()
    {
        var vars = new Dictionary<string, object?> { ["greeting"] = "hello" };
        var context = new ExecutionContext(variables: vars);

        Assert.Equal("hello", context.Variables["greeting"]);
    }

    [Fact]
    public void RegisterService_CanBeRetrieved()
    {
        var context = new ExecutionContext();
        var service = new TestService();
        context.RegisterService(service);

        Assert.Same(service, context.GetService<TestService>());
    }

    [Fact]
    public void GetService_ReturnsNull_WhenNotRegistered()
    {
        var context = new ExecutionContext();

        Assert.Null(context.GetService<TestService>());
    }

    [Fact]
    public void GetRequiredService_Throws_WhenNotRegistered()
    {
        var context = new ExecutionContext();

        Assert.Throws<InvalidOperationException>(() => context.GetRequiredService<TestService>());
    }

    [Fact]
    public void CancellationToken_DefaultIsNone()
    {
        var context = new ExecutionContext();

        Assert.Equal(CancellationToken.None, context.CancellationToken);
    }

    [Fact]
    public void CancellationToken_IsPassedThrough()
    {
        using var cts = new CancellationTokenSource();
        var context = new ExecutionContext(cts.Token);

        Assert.Equal(cts.Token, context.CancellationToken);
    }
}

file class TestService { }
