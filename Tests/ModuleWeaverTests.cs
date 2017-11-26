using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;

[TestFixture]
public class ModuleWeaverTests
{
    ModuleWeaverTestHelper moduleWeaverTestHelper;

    public ModuleWeaverTests()    {        var inputAssembly = Path.Combine(TestContext.CurrentContext.TestDirectory, "AssemblyToProcess.dll");        moduleWeaverTestHelper = new ModuleWeaverTestHelper(inputAssembly);    }

    [Test]
    public void Simple()
    {
        var instance = GetInstance("Simple");
        var isDisposed = GetIsDisposed(instance);
        Assert.IsFalse(isDisposed);
        instance.Dispose();
        isDisposed = GetIsDisposed(instance);
        Assert.IsTrue(isDisposed);
    }

    [Test]
    public void EnsureExplicitDisposeMethodIsWeaved()
    {
        var instance = GetInstance("WithExplicitDisposeMethod");
        var child = instance.Child;
        var isDisposed = GetIsDisposed(instance);
        var isChildDisposed = GetIsDisposed(child);
        Assert.IsFalse(isDisposed);
        Assert.IsFalse(isChildDisposed);
        ((IDisposable)instance).Dispose();
        isDisposed = GetIsDisposed(instance);
        isChildDisposed = GetIsDisposed(child);
        Assert.IsTrue(isDisposed);
        Assert.IsTrue(isChildDisposed);
    }

    [Test]
    public void EnsurePublicPropertyThrows()
    {
        var instance = GetInstance("Simple");
        instance.Dispose();
        Assert.Throws<ObjectDisposedException>(() => instance.PublicProperty = "aString");
        // ReSharper disable once UnusedVariable
        Assert.Throws<ObjectDisposedException>(() => { var x = instance.PublicProperty; });
    }

    [Test]
    public void EnsureInternalPropertyThrows()
    {
        var instance = GetInstance("Simple");
        instance.Dispose();
        var type = (Type)instance.GetType();
        var setMethodInfo = type.GetMethod("set_InternalProperty", BindingFlags.Instance | BindingFlags.NonPublic);
        var getMethodInfo = type.GetMethod("get_InternalProperty", BindingFlags.Instance | BindingFlags.NonPublic);
        var setTargetInvocationException = Assert.Throws<TargetInvocationException>(() => setMethodInfo.Invoke(instance, new object[] { "aString" }));
        Assert.IsAssignableFrom<ObjectDisposedException>(setTargetInvocationException.InnerException);
        var getTargetInvocationException = Assert.Throws<TargetInvocationException>(() => getMethodInfo.Invoke(instance, null));
        Assert.IsAssignableFrom<ObjectDisposedException>(getTargetInvocationException.InnerException);
    }

    [Test]
    public void EnsureProtectedPropertyThrows()
    {
        var instance = GetInstance("Simple");
        instance.Dispose();
        var type = (Type)instance.GetType();
        var setMethodInfo = type.GetMethod("set_ProtectedProperty", BindingFlags.Instance | BindingFlags.NonPublic);
        var getMethodInfo = type.GetMethod("get_ProtectedProperty", BindingFlags.Instance | BindingFlags.NonPublic);
        var setTargetInvocationException = Assert.Throws<TargetInvocationException>(() => setMethodInfo.Invoke(instance, new object[] { "aString" }));
        Assert.IsAssignableFrom<ObjectDisposedException>(setTargetInvocationException.InnerException);
        var getTargetInvocationException = Assert.Throws<TargetInvocationException>(() => getMethodInfo.Invoke(instance, null));
        Assert.IsAssignableFrom<ObjectDisposedException>(getTargetInvocationException.InnerException);
    }

    [Test]
    public void WithTypeConstraint()
    {
        var instance = GetGenericInstance("WithTypeConstraint`1", typeof(int));
        instance.Dispose();
    }

    [Test]
    public void EnsurePrivatePropertyDoesNotThrow()
    {
        var instance = GetInstance("Simple");
        instance.Dispose();
        var type = (Type)instance.GetType();
        var setMethodInfo = type.GetMethod("set_PrivateProperty", BindingFlags.Instance | BindingFlags.NonPublic);
        var getMethodInfo = type.GetMethod("get_PrivateProperty", BindingFlags.Instance | BindingFlags.NonPublic);
        setMethodInfo.Invoke(instance, new object[] { "aString" });
        getMethodInfo.Invoke(instance, null);
    }

    [Test]
    public void EnsureStaticPropertyDoesNotThrow()
    {
        var instance = GetInstance("Simple");
        instance.Dispose();
        var type = (Type)instance.GetType();
        var setMethodInfo = type.GetMethod("set_StaticProperty", BindingFlags.Static | BindingFlags.Public);
        var getMethodInfo = type.GetMethod("get_StaticProperty", BindingFlags.Static | BindingFlags.Public);
        setMethodInfo.Invoke(null, new object[] { "aString" });
        getMethodInfo.Invoke(null, null);
    }

    [Test]
    public void EnsurePublicMethodThrows()
    {
        var instance = GetInstance("Simple");
        instance.Dispose();
        Assert.Throws<ObjectDisposedException>(() => instance.PublicMethod());
    }

    [Test]
    public void EnsureInternalMethodThrows()
    {
        var instance = GetInstance("Simple");
        instance.Dispose();
        var type = (Type)instance.GetType();
        var methodInfo = type.GetMethod("InternalMethod", BindingFlags.Instance | BindingFlags.NonPublic);
        var targetInvocationException = Assert.Throws<TargetInvocationException>(() => methodInfo.Invoke(instance, null));
        Assert.IsAssignableFrom<ObjectDisposedException>(targetInvocationException.InnerException);
    }

    [Test]
    public void EnsureProtectedMethodThrows()
    {
        var instance = GetInstance("Simple");
        instance.Dispose();
        var type = (Type)instance.GetType();
        var methodInfo = type.GetMethod("ProtectedMethod", BindingFlags.Instance | BindingFlags.NonPublic);
        var targetInvocationException = Assert.Throws<TargetInvocationException>(() => methodInfo.Invoke(instance, null));
        Assert.IsAssignableFrom<ObjectDisposedException>(targetInvocationException.InnerException);
    }

    [Test]
    public void EnsurePrivateMethodDoesNotThrow()
    {
        var instance = GetInstance("Simple");
        instance.Dispose();
        var type = (Type)instance.GetType();
        var methodInfo = type.GetMethod("PrivateMethod", BindingFlags.Instance | BindingFlags.NonPublic);
        methodInfo.Invoke(instance, null);
    }

    [Test]
    public void EnsureStaticMethodDoesNotThrow()
    {
        var instance = GetInstance("Simple");
        instance.Dispose();
        var type = (Type)instance.GetType();
        var methodInfo = type.GetMethod("StaticMethod", BindingFlags.Static | BindingFlags.Public);
        methodInfo.Invoke(null, null);
    }

    [Test]
    public void WithManagedAndUnmanaged()
    {
        var instance = GetInstance("WithManagedAndUnmanaged");
        var isDisposed = GetIsDisposed(instance);
        Assert.IsFalse(isDisposed);
        instance.Dispose();
        isDisposed = GetIsDisposed(instance);

        Assert.IsTrue(isDisposed);
        Assert.IsTrue(instance.DisposeManagedCalled);
        Assert.IsTrue(instance.DisposeUnmanagedCalled);
        Assert.Throws<ObjectDisposedException>(() => instance.Method());
    }

    [Test]
    public void WithManaged()
    {
        var instance = GetInstance("WithManaged");
        var isDisposed = GetIsDisposed(instance);
        Assert.IsFalse(isDisposed);
        instance.Dispose();
        isDisposed = GetIsDisposed(instance);

        Assert.IsTrue(isDisposed);
        Assert.IsTrue(instance.DisposeManagedCalled);
        Assert.Throws<ObjectDisposedException>(() => instance.Method());
    }

    [Test]
    public void WithUnmanaged()
    {
        var instance = GetInstance("WithUnmanaged");
        var isDisposed = GetIsDisposed(instance);
        Assert.IsFalse(isDisposed);
        instance.Dispose();
        isDisposed = GetIsDisposed(instance);

        Assert.IsTrue(isDisposed);
        Assert.IsTrue(instance.DisposeUnmanagedCalled);
        Assert.Throws<ObjectDisposedException>(() => instance.Method());
    }

    [Test]
    public void WithUnmanagedAndDisposableField()
    {
        var instance = GetInstance("WithUnmanagedAndDisposableField");
        var isDisposed = GetIsDisposed(instance);
        Assert.IsFalse(isDisposed);
        instance.Dispose();
        isDisposed = GetIsDisposed(instance);

        Assert.IsTrue(isDisposed);
        Assert.IsTrue(instance.DisposeUnmanagedCalled);
        Assert.IsNull(instance.DisposableField);
        Assert.Throws<ObjectDisposedException>(() => instance.Method());
    }

    [Test]
    public void WhereFieldIsDisposableByBase()
    {
        var instance = GetInstance("WhereFieldIsDisposableByBase");
        var child = instance.Child;
        var isChildDisposed = GetIsDisposed(child);
        Assert.IsFalse(isChildDisposed);
        instance.Dispose();
        isChildDisposed = GetIsDisposed(child);
        Assert.IsTrue(isChildDisposed);
        Assert.Throws<ObjectDisposedException>(() => instance.Method());
    }

    [Test]
    public void WhereFieldIsIDisposable()
    {
        var instance = GetInstance("WhereFieldIsIDisposable");
        var field = instance.Field;
        var isFieldDisposed = GetIsDisposed(field);
        Assert.IsFalse(isFieldDisposed);
        instance.Dispose();
        isFieldDisposed = GetIsDisposed(field);
        Assert.IsTrue(isFieldDisposed);
    }

    [Test]
    public void WhereFieldIsIDisposableArray()
    {
        var instance = GetInstance("WhereFieldIsIDisposableArray");
        instance.Dispose();
        Assert.IsNotNull(instance.Field);
    }

    [Test]
    public void WhereFieldIsDisposableClassArray()
    {
        var instance = GetInstance("WhereFieldIsDisposableClassArray");
        instance.Dispose();
        Assert.IsNotNull(instance.Field);
    }


    [TestCase("WithProtectedDisposeManaged",
        "In WithProtectedDisposeManaged.DisposeManaged\r\n")]
    [TestCase("WithOverriddenDisposeManaged",
        "In WithOverriddenDisposeManaged.DisposeManaged\r\n" +
        "In WithProtectedDisposeManaged.DisposeManaged\r\n")]
    [TestCase("WithProtectedDisposeUnmanaged",
        "In WithProtectedDisposeUnmanaged.DisposeUnmanaged\r\n")]
    [TestCase("WithOverriddenDisposeUnmanaged",
        "In WithOverriddenDisposeUnmanaged.DisposeUnmanaged\r\n" +
        "In WithProtectedDisposeUnmanaged.DisposeUnmanaged\r\n")]
    [TestCase("WithProtectedDisposeManagedAndDisposeUnmanaged",
        "In WithProtectedDisposeManagedAndDisposeUnmanaged.DisposeManaged\r\n" +
        "In WithProtectedDisposeManagedAndDisposeUnmanaged.DisposeUnmanaged\r\n")]
    [TestCase("WithOverriddenDisposeManagedAndDisposeUnmanaged",
        "In WithOverriddenDisposeManagedAndDisposeUnmanaged.DisposeManaged\r\n" +
        "In WithProtectedDisposeManagedAndDisposeUnmanaged.DisposeManaged\r\n" +
        "In WithOverriddenDisposeManagedAndDisposeUnmanaged.DisposeUnmanaged\r\n" +
        "In WithProtectedDisposeManagedAndDisposeUnmanaged.DisposeUnmanaged\r\n")]
    [TestCase("WithAbstractBaseClass",
        "In WithAbstractBaseClass.DisposeManaged\r\n" +
        "In AbstractWithProtectedDisposeManaged.DisposeManaged\r\n")]
    [TestCase("WithAbstractDisposeManaged",
        "In WithAbstractDisposeManaged.DisposeManaged\r\n")]
    public void ProtectedDisposableTest(string className, string expectedValue)
    {
        var writer = new StringWriter();
        Console.SetOut(writer);

        var instance = GetInstance(className);
        Assert.That(GetIsDisposed(instance), Is.False);
        instance.Dispose();
        Assert.That(GetIsDisposed(instance), Is.True);
        Assert.That(writer.ToString(), Is.EqualTo(expectedValue));
    }

    [Test]
    public void EnsureTasksAreNotDisposed()
    {
        var instance = GetInstance("WithTask");
        instance.Dispose();
        Assert.IsNotNull(instance.taskField);
        instance.taskCompletionSource.SetResult(42);
        Assert.That(instance.taskField.Result, Is.EqualTo(42));
    }

    [Test]
    public void EnsureClassesInSkippedNamespacesAreNotDisposed()
    {
        var instance = GetInstance("NamespaceToSkip.WhereNamespaceShouldBeSkipped");
        instance.Dispose();
        Assert.IsNotNull(instance.disposableField);
    }

    [Test]
    public void WithUnmanagedAndGenericField()
    {
        var instance = GetGenericInstance("WithUnmanagedAndGenericField`1", typeof(string));
        Assert.That(GetIsDisposed(instance), Is.False);
        instance.Dispose();
        Assert.That(GetIsDisposed(instance), Is.True);
    }

    [Test]
    public void WithUnmanagedAndGenericIDisposableField()
    {
        var instance = GetGenericInstance("WithUnmanagedAndGenericIDisposableField`1", typeof(Stream));
        Assert.That(GetIsDisposed(instance), Is.False);
        instance.Dispose();
        Assert.That(GetIsDisposed(instance), Is.True);
    }

    [Test]
    public void WithUnmanagedAndGenericStreamField()
    {
        var instance = GetGenericInstance("WithUnmanagedAndGenericStreamField`1", typeof(MemoryStream));
        Assert.That(GetIsDisposed(instance), Is.False);
        instance.Dispose();
        Assert.That(GetIsDisposed(instance), Is.True);
    }

    [Test]
    public void SimpleWithGenericField()
    {
        var instance = GetGenericInstance("SimpleWithGenericField`1", typeof(Stream));
        Assert.That(GetIsDisposed(instance), Is.False);
        instance.Dispose();
        Assert.That(GetIsDisposed(instance), Is.True);
    }

    bool GetIsDisposed(dynamic instance)
    {
        Type type = instance.GetType();
        var fieldInfo = GetSignaledField(type);
        var disposeCount = (int)fieldInfo.GetValue(instance);
        return disposeCount > 0;
    }

    static FieldInfo GetSignaledField(Type type)
    {
        FieldInfo fieldInfo = null;
        while (fieldInfo == null && type != null)
        {
            fieldInfo = type.GetField("disposeSignaled", BindingFlags.NonPublic | BindingFlags.Instance);
            type = type.BaseType;
        }
        return fieldInfo;
    }

    public dynamic GetInstance(string className)
    {
        var type = moduleWeaverTestHelper.Assembly.GetType(className, true);
        return Activator.CreateInstance(type);
    }

    public dynamic GetGenericInstance(string className, params Type[] types)
    {
        var type = moduleWeaverTestHelper.Assembly.GetType(className, true);
        var genericType = type.MakeGenericType(types);
        return Activator.CreateInstance(genericType);
    }

    [Test]
    public void PeVerify()
    {
        Verifier.Verify(moduleWeaverTestHelper.BeforeAssemblyPath, moduleWeaverTestHelper.AfterAssemblyPath);
    }
}