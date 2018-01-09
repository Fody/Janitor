using Xunit;

public class TemplateTest
{
    [Fact]
    public void Run()
    {
        var templateClass = new TemplateClass();
        templateClass.Dispose();
        templateClass.Dispose();
    }
}