using System.IO;
using NUnit.Framework;

[TestFixture]
public class ReadonlyFieldTests
{
    [Test]
    public void Verity_throws_an_exception()
    {
        var inputAssembly = Path.Combine(TestContext.CurrentContext.TestDirectory, "AssemblyWithReadOnly.dll");
        var testHelper = new ModuleWeaverTestHelper(inputAssembly);
        Assert.AreEqual("Could not add dispose for field 'WithReadOnly.stream' since it is marked as readonly. Change this field to not be readonly.", testHelper.Errors[0]);
    }
}