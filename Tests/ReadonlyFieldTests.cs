using Fody;
using Xunit;

public class ReadonlyFieldTests
{
    [Fact]
    public void Verity_throws_an_exception()
    {
        var weavingTask = new ModuleWeaver();
        var testResult = weavingTask.ExecuteTestRun("AssemblyWithReadOnly.dll");
        Assert.Equal("Could not add dispose for field 'WithReadOnly.stream' since it is marked as readonly. Change this field to not be readonly.", testResult.Errors[0].Text);
    }
}