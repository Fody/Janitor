using System;
using System.IO;
using System.Reflection;
using Fody;
using Xunit;
#pragma warning disable 618

public class ModuleWeaverTests
{
    static Assembly assembly;
    static TestResult testResult;

    static ModuleWeaverTests()
    {
        var weavingTask = new ModuleWeaver();
        testResult = weavingTask.ExecuteTestRun("AssemblyToProcess.dll");
        assembly = testResult.Assembly;
    }

    [Fact]
    public void Simple()
    {
        var instance = GetInstance("Simple");
        var isDisposed = GetIsDisposed(instance);
        Assert.False(isDisposed);
        instance.Dispose();
        isDisposed = GetIsDisposed(instance);
        Assert.True(isDisposed);
    }

    [Fact]
    public void EnsureExplicitDisposeMethodIsWeaved()
    {
        var instance = GetInstance("WithExplicitDisposeMethod");
        var child = instance.Child;
        var isDisposed = GetIsDisposed(instance);
        var isChildDisposed = GetIsDisposed(child);
        Assert.False(isDisposed);
        Assert.False(isChildDisposed);
        ((IDisposable)instance).Dispose();
        isDisposed = GetIsDisposed(instance);
        isChildDisposed = GetIsDisposed(child);
        Assert.True(isDisposed);
        Assert.True(isChildDisposed);
    }

    [Fact]
    public void EnsurePublicPropertyThrows()
    {
        var instance = GetInstance("Simple");
        instance.Dispose();
        Assert.Throws<ObjectDisposedException>(() => instance.PublicProperty = "aString");
        // ReSharper disable once UnusedVariable
        Assert.Throws<ObjectDisposedException>(() =>
        {
            var x = instance.PublicProperty;
        });
    }

    [Fact]
    public void EnsureInternalPropertyThrows()
    {
        var instance = GetInstance("Simple");
        instance.Dispose();
        var type = (Type)instance.GetType();
        var setMethodInfo = type.GetMethod("set_InternalProperty", BindingFlags.Instance | BindingFlags.NonPublic);
        var getMethodInfo = type.GetMethod("get_InternalProperty", BindingFlags.Instance | BindingFlags.NonPublic);
        var setTargetInvocationException = Assert.Throws<TargetInvocationException>(() => setMethodInfo.Invoke(instance, new object[] {"aString"}));
        Assert.IsAssignableFrom<ObjectDisposedException>(setTargetInvocationException.InnerException);
        var getTargetInvocationException = Assert.Throws<TargetInvocationException>(() => getMethodInfo.Invoke(instance, null));
        Assert.IsAssignableFrom<ObjectDisposedException>(getTargetInvocationException.InnerException);
    }

    [Fact]
    public void EnsureProtectedPropertyThrows()
    {
        var instance = GetInstance("Simple");
        instance.Dispose();
        var type = (Type)instance.GetType();
        var setMethodInfo = type.GetMethod("set_ProtectedProperty", BindingFlags.Instance | BindingFlags.NonPublic);
        var getMethodInfo = type.GetMethod("get_ProtectedProperty", BindingFlags.Instance | BindingFlags.NonPublic);
        var setTargetInvocationException = Assert.Throws<TargetInvocationException>(() => setMethodInfo.Invoke(instance, new object[] {"aString"}));
        Assert.IsAssignableFrom<ObjectDisposedException>(setTargetInvocationException.InnerException);
        var getTargetInvocationException = Assert.Throws<TargetInvocationException>(() => getMethodInfo.Invoke(instance, null));
        Assert.IsAssignableFrom<ObjectDisposedException>(getTargetInvocationException.InnerException);
    }

    [Fact]
    public void WithTypeConstraint()
    {
        var instance = GetGenericInstance("WithTypeConstraint`1", typeof(int));
        instance.Dispose();
    }

    [Fact]
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

    [Fact]
    public void EnsureStaticPropertyDoesNotThrow()
    {
        var instance = GetInstance("Simple");
        instance.Dispose();
        var type = (Type)instance.GetType();
        var setMethodInfo = type.GetMethod("set_StaticProperty", BindingFlags.Static | BindingFlags.Public);
        var getMethodInfo = type.GetMethod("get_StaticProperty", BindingFlags.Static | BindingFlags.Public);
        setMethodInfo.Invoke(null, new object[] {"aString"});
        getMethodInfo.Invoke(null, null);
    }

    [Fact]
    public void EnsurePublicMethodThrows()
    {
        var instance = GetInstance("Simple");
        instance.Dispose();
        Assert.Throws<ObjectDisposedException>(() => instance.PublicMethod());
    }

    [Fact]
    public void EnsureInternalMethodThrows()
    {
        var instance = GetInstance("Simple");
        instance.Dispose();
        var type = (Type)instance.GetType();
        var methodInfo = type.GetMethod("InternalMethod", BindingFlags.Instance | BindingFlags.NonPublic);
        var targetInvocationException = Assert.Throws<TargetInvocationException>(() => methodInfo.Invoke(instance, null));
        Assert.IsAssignableFrom<ObjectDisposedException>(targetInvocationException.InnerException);
    }

    [Fact]
    public void EnsureProtectedMethodThrows()
    {
        var instance = GetInstance("Simple");
        instance.Dispose();
        var type = (Type)instance.GetType();
        var methodInfo = type.GetMethod("ProtectedMethod", BindingFlags.Instance | BindingFlags.NonPublic);
        var targetInvocationException = Assert.Throws<TargetInvocationException>(() => methodInfo.Invoke(instance, null));
        Assert.IsAssignableFrom<ObjectDisposedException>(targetInvocationException.InnerException);
    }

    [Fact]
    public void EnsurePrivateMethodDoesNotThrow()
    {
        var instance = GetInstance("Simple");
        instance.Dispose();
        var type = (Type)instance.GetType();
        var methodInfo = type.GetMethod("PrivateMethod", BindingFlags.Instance | BindingFlags.NonPublic);
        methodInfo.Invoke(instance, null);
    }

    [Fact]
    public void EnsureStaticMethodDoesNotThrow()
    {
        var instance = GetInstance("Simple");
        instance.Dispose();
        var type = (Type)instance.GetType();
        var methodInfo = type.GetMethod("StaticMethod", BindingFlags.Static | BindingFlags.Public);
        methodInfo.Invoke(null, null);
    }

    [Fact]
    public void WithManagedAndUnmanaged()
    {
        var instance = GetInstance("WithManagedAndUnmanaged");
        var isDisposed = GetIsDisposed(instance);
        Assert.False(isDisposed);
        instance.Dispose();
        isDisposed = GetIsDisposed(instance);

        Assert.True(isDisposed);
        Assert.True(instance.DisposeManagedCalled);
        Assert.True(instance.DisposeUnmanagedCalled);
        Assert.Throws<ObjectDisposedException>(() => instance.Method());
    }

    [Fact]
    public void WithManaged()
    {
        var instance = GetInstance("WithManaged");
        var isDisposed = GetIsDisposed(instance);
        Assert.False(isDisposed);
        instance.Dispose();
        isDisposed = GetIsDisposed(instance);

        Assert.True(isDisposed);
        Assert.True(instance.DisposeManagedCalled);
        Assert.Throws<ObjectDisposedException>(() => instance.Method());
    }

    [Fact]
    public void WithUnmanaged()
    {
        var instance = GetInstance("WithUnmanaged");
        var isDisposed = GetIsDisposed(instance);
        Assert.False(isDisposed);
        instance.Dispose();
        isDisposed = GetIsDisposed(instance);

        Assert.True(isDisposed);
        Assert.True(instance.DisposeUnmanagedCalled);
        Assert.Throws<ObjectDisposedException>(() => instance.Method());
    }

    [Fact]
    public void WithUnmanagedAndDisposableField()
    {
        var instance = GetInstance("WithUnmanagedAndDisposableField");
        var isDisposed = GetIsDisposed(instance);
        Assert.False(isDisposed);
        instance.Dispose();
        isDisposed = GetIsDisposed(instance);

        Assert.True(isDisposed);
        Assert.True(instance.DisposeUnmanagedCalled);
        Assert.Null(instance.DisposableField);
        Assert.Throws<ObjectDisposedException>(() => instance.Method());
    }

    [Fact]
    public void WhereFieldIsDisposableByBase()
    {
        var instance = GetInstance("WhereFieldIsDisposableByBase");
        var child = instance.Child;
        var isChildDisposed = GetIsDisposed(child);
        Assert.False(isChildDisposed);
        instance.Dispose();
        isChildDisposed = GetIsDisposed(child);
        Assert.True(isChildDisposed);
        Assert.Throws<ObjectDisposedException>(() => instance.Method());
    }

    [Fact]
    public void WhereFieldIsIDisposable()
    {
        var instance = GetInstance("WhereFieldIsIDisposable");
        var field = instance.Field;
        var isFieldDisposed = GetIsDisposed(field);
        Assert.False(isFieldDisposed);
        instance.Dispose();
        isFieldDisposed = GetIsDisposed(field);
        Assert.True(isFieldDisposed);
    }

    [Fact]
    public void WhereFieldIsValueType()
    {
        Assert.Contains(testResult.Errors, x => x.Text.Contains("WhereFieldIsValueType"));
    }

    [Fact]
    public void WhereFieldIsIDisposableArray()
    {
        var instance = GetInstance("WhereFieldIsIDisposableArray");
        instance.Dispose();
        Assert.NotNull(instance.Field);
    }

    [Fact]
    public void WhereFieldIsDisposableClassArray()
    {
        var instance = GetInstance("WhereFieldIsDisposableClassArray");
        instance.Dispose();
        Assert.NotNull(instance.Field);
    }

    [Theory]
    [InlineData("WithProtectedDisposeManaged",
        "In WithProtectedDisposeManaged.DisposeManaged\r\n")]
    [InlineData("WithOverriddenDisposeManaged",
        "In WithOverriddenDisposeManaged.DisposeManaged\r\n" +
        "In WithProtectedDisposeManaged.DisposeManaged\r\n")]
    [InlineData("WithProtectedDisposeUnmanaged",
        "In WithProtectedDisposeUnmanaged.DisposeUnmanaged\r\n")]
    [InlineData("WithOverriddenDisposeUnmanaged",
        "In WithOverriddenDisposeUnmanaged.DisposeUnmanaged\r\n" +
        "In WithProtectedDisposeUnmanaged.DisposeUnmanaged\r\n")]
    [InlineData("WithProtectedDisposeManagedAndDisposeUnmanaged",
        "In WithProtectedDisposeManagedAndDisposeUnmanaged.DisposeManaged\r\n" +
        "In WithProtectedDisposeManagedAndDisposeUnmanaged.DisposeUnmanaged\r\n")]
    [InlineData("WithOverriddenDisposeManagedAndDisposeUnmanaged",
        "In WithOverriddenDisposeManagedAndDisposeUnmanaged.DisposeManaged\r\n" +
        "In WithProtectedDisposeManagedAndDisposeUnmanaged.DisposeManaged\r\n" +
        "In WithOverriddenDisposeManagedAndDisposeUnmanaged.DisposeUnmanaged\r\n" +
        "In WithProtectedDisposeManagedAndDisposeUnmanaged.DisposeUnmanaged\r\n")]
    [InlineData("WithAbstractBaseClass",
        "In WithAbstractBaseClass.DisposeManaged\r\n" +
        "In AbstractWithProtectedDisposeManaged.DisposeManaged\r\n")]
    [InlineData("WithAbstractDisposeManaged",
        "In WithAbstractDisposeManaged.DisposeManaged\r\n")]
    public void ProtectedDisposableTest(string className, string expectedValue)
    {
        var writer = new StringWriter();
        Console.SetOut(writer);

        var instance = GetInstance(className);
        Assert.False(GetIsDisposed(instance));
        instance.Dispose();
        Assert.True(GetIsDisposed(instance));
        Assert.Equal(writer.ToString(), expectedValue);
    }

    [Fact]
    public void EnsureTasksAreNotDisposed()
    {
        var instance = GetInstance("WithTask");
        instance.Dispose();
        Assert.NotNull(instance.taskField);
        instance.taskCompletionSource.SetResult(42);
        Assert.Equal(instance.taskField.Result, 42);
    }

    [Fact]
    public void EnsureClassesInSkippedNamespacesAreNotDisposed()
    {
        var instance = GetInstance("NamespaceToSkip.WhereNamespaceShouldBeSkipped");
        instance.Dispose();
        Assert.NotNull(instance.disposableField);
    }

    [Fact]
    public void WithUnmanagedAndGenericField()
    {
        var instance = GetGenericInstance("WithUnmanagedAndGenericField`1", typeof(string));
        Assert.False(GetIsDisposed(instance));
        instance.Dispose();
        Assert.True(GetIsDisposed(instance));
    }

    [Fact]
    public void WithUnmanagedAndGenericIDisposableField()
    {
        var instance = GetGenericInstance("WithUnmanagedAndGenericIDisposableField`1", typeof(Stream));
        Assert.False(GetIsDisposed(instance));
        instance.Dispose();
        Assert.True(GetIsDisposed(instance));
    }

    [Fact]
    public void WithUnmanagedAndGenericStreamField()
    {
        var instance = GetGenericInstance("WithUnmanagedAndGenericStreamField`1", typeof(MemoryStream));
        Assert.False(GetIsDisposed(instance));
        instance.Dispose();
        Assert.True(GetIsDisposed(instance));
    }

    [Fact]
    public void SimpleWithGenericField()
    {
        var instance = GetGenericInstance("SimpleWithGenericField`1", typeof(Stream));
        Assert.False(GetIsDisposed(instance));
        instance.Dispose();
        Assert.True(GetIsDisposed(instance));
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
        var type = assembly.GetType(className, true);
        return Activator.CreateInstance(type);
    }

    public dynamic GetGenericInstance(string className, params Type[] types)
    {
        var type = assembly.GetType(className, true);
        var genericType = type.MakeGenericType(types);
        return Activator.CreateInstance(genericType);
    }
}