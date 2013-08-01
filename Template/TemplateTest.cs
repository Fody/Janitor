using NUnit.Framework;

[TestFixture]
public class TemplateTest
{
    [Test]
    public void Run()
    {
        var templateClass = new TemplateClass();
        templateClass.Dispose();
        templateClass.Dispose();
    }
}