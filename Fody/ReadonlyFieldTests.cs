using NUnit.Framework;

[TestFixture]
public class ReadonlyFieldTests
{

    [Test]
    public void Verity_throws_an_exception()
    {
        var weavingException = Assert.Throws<WeavingException>(() => { new ModuleWeaverTestHelper(@"..\..\..\AssemblyWithReadOnly\bin\Debug\AssemblyWithReadOnly.dll"); });
        Assert.AreEqual("Could not add dispose for field 'WithReadOnly.stream' since it is marked as readonly. Please change this field to not be readonly.", weavingException.Message);
    }
}