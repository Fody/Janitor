using System;
using System.Reflection;
using NUnit.Framework;

[TestFixture]
public class ModuleWeaverTests
{
    ModuleWeaverTestHelper moduleWeaverTestHelper;

    public ModuleWeaverTests()
    {
        moduleWeaverTestHelper = new ModuleWeaverTestHelper(@"..\..\..\AssemblyToProcess\bin\Debug\AssemblyToProcess.dll");
    }

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
        var isDisposed = GetIsDisposed(instance);
        Assert.IsFalse(isDisposed);
        ((IDisposable)instance).Dispose();
        isDisposed = GetIsDisposed(instance);
        Assert.IsTrue(isDisposed);
    }

    [Test]
    public void EnsurePublicPropertyThrows()
    {
        var instance = GetInstance("Simple");
        instance.Dispose();
        Assert.Throws<ObjectDisposedException>(() => instance.PublicProperty = "aString");
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
    public void EnsurePrivatePropertyDoesNotThrow()
    {
        var instance = GetInstance("Simple");
        instance.Dispose();
        var type = (Type)instance.GetType();
        var setMethodInfo = type.GetMethod("set_PrivateProperty", BindingFlags.Instance | BindingFlags.NonPublic);
        var getMethodInfo = type.GetMethod("get_PrivateProperty", BindingFlags.Instance | BindingFlags.NonPublic);
        setMethodInfo.Invoke(instance, new object[] {"aString"});
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
        var methodInfo = type.GetMethod("PrivateMethod",BindingFlags.Instance|BindingFlags.NonPublic);
        methodInfo.Invoke(instance,null);
    }

    [Test]
    public void EnsureStaticMethodDoesNotThrow()
    {
        var instance = GetInstance("Simple");
        instance.Dispose();
        var type = (Type)instance.GetType();
        var methodInfo = type.GetMethod("StaticMethod",BindingFlags.Static|BindingFlags.Public);
        methodInfo.Invoke(null,null);
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

    bool GetIsDisposed(dynamic instance)
    {
        Type type = instance.GetType();
        FieldInfo fieldInfo = null;
        while (fieldInfo == null && type != null)
        {
            fieldInfo = type.GetField("disposeSignaled", BindingFlags.NonPublic | BindingFlags.Instance);
            type = type.BaseType;
        }
        var disposeCount = (int)fieldInfo.GetValue(instance);
        return disposeCount > 0;
    }

    public dynamic GetInstance(string className)
    {
        var type = moduleWeaverTestHelper.Assembly.GetType(className, true);
        return Activator.CreateInstance(type);
    }

    [Test]
    public void PeVerify()
    {
        Verifier.Verify(moduleWeaverTestHelper.BeforeAssemblyPath, moduleWeaverTestHelper.AfterAssemblyPath);
    }
}