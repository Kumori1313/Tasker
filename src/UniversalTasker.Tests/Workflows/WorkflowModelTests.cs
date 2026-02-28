using UniversalTasker.Core.Workflows;

namespace UniversalTasker.Tests.Workflows;

public class WorkflowModelTests
{
    [Fact]
    public void Workflow_HasDefaultValues()
    {
        var workflow = new Workflow();

        Assert.NotNull(workflow.Id);
        Assert.Equal("New Workflow", workflow.Name);
        Assert.Equal("", workflow.Description);
        Assert.Equal(1, workflow.Version);
        Assert.NotNull(workflow.Settings);
        Assert.NotNull(workflow.Variables);
        Assert.NotNull(workflow.Triggers);
        Assert.NotNull(workflow.Actions);
        Assert.Empty(workflow.Variables);
        Assert.Empty(workflow.Triggers);
        Assert.Empty(workflow.Actions);
    }

    [Fact]
    public void WorkflowSettings_HasDefaultValues()
    {
        var settings = new WorkflowSettings();

        Assert.True(settings.StopOnError);
        Assert.Equal(0, settings.MaxExecutionTimeSeconds);
        Assert.Equal(Microsoft.Extensions.Logging.LogLevel.Information, settings.LogLevel);
        Assert.True(settings.EnableTriggersOnStart);
        Assert.False(settings.AllowConcurrentExecution);
    }

    [Fact]
    public void Workflow_Id_IsUniquePerInstance()
    {
        var w1 = new Workflow();
        var w2 = new Workflow();

        Assert.NotEqual(w1.Id, w2.Id);
    }
}
