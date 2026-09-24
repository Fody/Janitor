using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Fody;
using TestResult = Fody.TestResult;

// Console.SetOut is process wide
[NotInParallel]
public class ModuleWeaverTests
{
    static TestResult testResult;

    static ModuleWeaverTests()
    {
        var weaver = new ModuleWeaver();
#if(NET46)
        testResult = weaver.ExecuteTestRun("AssemblyToProcess.dll");
#else
        testResult = weaver.ExecuteTestRun("AssemblyToProcess.dll",false);
#endif
    }

    [Test]
    public async Task Simple()
    {
        var instance = testResult.GetInstance("Simple");
        var isDisposed = GetIsDisposed((object)instance);
        await Assert.That(isDisposed).IsFalse();
        instance.Dispose();
        isDisposed = GetIsDisposed((object)instance);
        await Assert.That(isDisposed).IsTrue();
    }

    [Test]
    public async Task EnsureExplicitDisposeMethodIsWeaved()
    {
        var instance = testResult.GetInstance("WithExplicitDisposeMethod");
        var child = instance.Child;
        var isDisposed = GetIsDisposed((object)instance);
        var isChildDisposed = GetIsDisposed((object)child);
        await Assert.That(isDisposed).IsFalse();
        await Assert.That(isChildDisposed).IsFalse();
        ((IDisposable)instance).Dispose();
        isDisposed = GetIsDisposed((object)instance);
        isChildDisposed = GetIsDisposed((object)child);
        await Assert.That(isDisposed).IsTrue();
        await Assert.That(isChildDisposed).IsTrue();
    }

    [Test]
    public async Task EnsurePublicPropertyThrows()
    {
        var instance = testResult.GetInstance("Simple");
        instance.Dispose();
        await Assert.That(new Action(() => instance.PublicProperty = "aString")).Throws<ObjectDisposedException>();
        // ReSharper disable once UnusedVariable
        await Assert.That(new Action(() =>
        {
            var x = instance.PublicProperty;
        })).Throws<ObjectDisposedException>();
    }

    [Test]
    public async Task EnsureInternalPropertyThrows()
    {
        var instance = testResult.GetInstance("Simple");
        instance.Dispose();
        var type = (Type)instance.GetType();
        var setMethodInfo = type.GetMethod("set_InternalProperty", BindingFlags.Instance | BindingFlags.NonPublic);
        var getMethodInfo = type.GetMethod("get_InternalProperty", BindingFlags.Instance | BindingFlags.NonPublic);
        var setTargetInvocationException = await Assert.That(new Action(() => setMethodInfo.Invoke(instance, new object[] {"aString"}))).Throws<TargetInvocationException>();
        await Assert.That(setTargetInvocationException!.InnerException).IsAssignableTo<ObjectDisposedException>();
        var getTargetInvocationException = await Assert.That(new Action(() => getMethodInfo.Invoke(instance, null))).Throws<TargetInvocationException>();
        await Assert.That(getTargetInvocationException!.InnerException).IsAssignableTo<ObjectDisposedException>();
    }

    [Test]
    public async Task EnsureProtectedPropertyThrows()
    {
        var instance = testResult.GetInstance("Simple");
        instance.Dispose();
        var type = (Type)instance.GetType();
        var setMethodInfo = type.GetMethod("set_ProtectedProperty", BindingFlags.Instance | BindingFlags.NonPublic);
        var getMethodInfo = type.GetMethod("get_ProtectedProperty", BindingFlags.Instance | BindingFlags.NonPublic);
        var setTargetInvocationException = await Assert.That(new Action(() => setMethodInfo.Invoke(instance, new object[] {"aString"}))).Throws<TargetInvocationException>();
        await Assert.That(setTargetInvocationException!.InnerException).IsAssignableTo<ObjectDisposedException>();
        var getTargetInvocationException = await Assert.That(new Action(() => getMethodInfo.Invoke(instance, null))).Throws<TargetInvocationException>();
        await Assert.That(getTargetInvocationException!.InnerException).IsAssignableTo<ObjectDisposedException>();
    }

    [Test]
    public async Task WithTypeConstraint()
    {
        var instance = testResult.GetGenericInstance("WithTypeConstraint`1", typeof(int));
        instance.Dispose();
    }

    [Test]
    public async Task EnsurePrivatePropertyDoesNotThrow()
    {
        var instance = testResult.GetInstance("Simple");
        instance.Dispose();
        var type = (Type)instance.GetType();
        var setMethodInfo = type.GetMethod("set_PrivateProperty", BindingFlags.Instance | BindingFlags.NonPublic);
        var getMethodInfo = type.GetMethod("get_PrivateProperty", BindingFlags.Instance | BindingFlags.NonPublic);
        setMethodInfo.Invoke(instance, new object[] {"aString"});
        getMethodInfo.Invoke(instance, null);
    }

    [Test]
    public async Task EnsureStaticPropertyDoesNotThrow()
    {
        var instance = testResult.GetInstance("Simple");
        instance.Dispose();
        var type = (Type)instance.GetType();
        var setMethodInfo = type.GetMethod("set_StaticProperty", BindingFlags.Static | BindingFlags.Public);
        var getMethodInfo = type.GetMethod("get_StaticProperty", BindingFlags.Static | BindingFlags.Public);
        setMethodInfo.Invoke(null, ["aString"]);
        getMethodInfo.Invoke(null, null);
    }

    [Test]
    public async Task EnsurePublicMethodThrows()
    {
        var instance = testResult.GetInstance("Simple");
        instance.Dispose();
        await Assert.That(new Action(() =>
        {
            instance.PublicMethod();
        })).Throws<ObjectDisposedException>();
    }

    [Test]
    public async Task EnsureInternalMethodThrows()
    {
        var instance = testResult.GetInstance("Simple");
        instance.Dispose();
        var type = (Type)instance.GetType();
        var methodInfo = type.GetMethod("InternalMethod", BindingFlags.Instance | BindingFlags.NonPublic);
        var targetInvocationException = await Assert.That(new Action(() => methodInfo.Invoke(instance, null))).Throws<TargetInvocationException>();
        await Assert.That(targetInvocationException!.InnerException).IsAssignableTo<ObjectDisposedException>();
    }

    [Test]
    public async Task EnsureProtectedMethodThrows()
    {
        var instance = testResult.GetInstance("Simple");
        instance.Dispose();
        var type = (Type)instance.GetType();
        var methodInfo = type.GetMethod("ProtectedMethod", BindingFlags.Instance | BindingFlags.NonPublic);
        var targetInvocationException = await Assert.That(new Action(() => methodInfo.Invoke(instance, null))).Throws<TargetInvocationException>();
        await Assert.That(targetInvocationException!.InnerException).IsAssignableTo<ObjectDisposedException>();
    }

    [Test]
    public async Task EnsurePrivateMethodDoesNotThrow()
    {
        var instance = testResult.GetInstance("Simple");
        instance.Dispose();
        var type = (Type)instance.GetType();
        var methodInfo = type.GetMethod("PrivateMethod", BindingFlags.Instance | BindingFlags.NonPublic);
        methodInfo.Invoke(instance, null);
    }

    [Test]
    public async Task EnsureStaticMethodDoesNotThrow()
    {
        var instance = testResult.GetInstance("Simple");
        instance.Dispose();
        var type = (Type)instance.GetType();
        var methodInfo = type.GetMethod("StaticMethod", BindingFlags.Static | BindingFlags.Public);
        methodInfo.Invoke(null, null);
    }

    [Test]
    public async Task WithManagedAndUnmanaged()
    {
        var instance = testResult.GetInstance("WithManagedAndUnmanaged");
        var isDisposed = GetIsDisposed((object)instance);
        await Assert.That(isDisposed).IsFalse();
        instance.Dispose();
        isDisposed = GetIsDisposed((object)instance);

        await Assert.That(isDisposed).IsTrue();
        await Assert.That((bool)instance.DisposeManagedCalled).IsTrue();
        await Assert.That((bool)instance.DisposeUnmanagedCalled).IsTrue();
        await Assert.That(new Action(() =>
        {
            instance.Method();
        })).Throws<ObjectDisposedException>();
    }

    [Test]
    public async Task WithManaged()
    {
        var instance = testResult.GetInstance("WithManaged");
        var isDisposed = GetIsDisposed((object)instance);
        await Assert.That(isDisposed).IsFalse();
        instance.Dispose();
        isDisposed = GetIsDisposed((object)instance);

        await Assert.That(isDisposed).IsTrue();
        await Assert.That((bool)instance.DisposeManagedCalled).IsTrue();
        await Assert.That(new Action(() =>
        {
            instance.Method();
        })).Throws<ObjectDisposedException>();
    }

    [Test]
    public async Task WithUnmanaged()
    {
        var instance = testResult.GetInstance("WithUnmanaged");
        var isDisposed = GetIsDisposed((object)instance);
        await Assert.That(isDisposed).IsFalse();
        instance.Dispose();
        isDisposed = GetIsDisposed((object)instance);

        await Assert.That(isDisposed).IsTrue();
        await Assert.That((bool)instance.DisposeUnmanagedCalled).IsTrue();
        await Assert.That(new Action(() =>
        {
           instance.Method();
        })).Throws<ObjectDisposedException>();
    }

    [Test]
    public async Task WithUnmanagedAndDisposableField()
    {
        var instance = testResult.GetInstance("WithUnmanagedAndDisposableField");
        var isDisposed = GetIsDisposed((object)instance);
        await Assert.That(isDisposed).IsFalse();
        instance.Dispose();
        isDisposed = GetIsDisposed((object)instance);

        await Assert.That(isDisposed).IsTrue();
        await Assert.That((bool)instance.DisposeUnmanagedCalled).IsTrue();
        await Assert.That((object?)instance.DisposableField).IsNull();
        await Assert.That(new Action(() =>
        {
            instance.Method();
        })).Throws<ObjectDisposedException>();
    }

    [Test]
    public async Task WhereFieldIsDisposableByBase()
    {
        var instance = testResult.GetInstance("WhereFieldIsDisposableByBase");
        var child = instance.Child;
        var isChildDisposed = GetIsDisposed((object)child);
        await Assert.That(isChildDisposed).IsFalse();
        instance.Dispose();
        isChildDisposed = GetIsDisposed((object)child);
        await Assert.That(isChildDisposed).IsTrue();
        await Assert.That(new Action(() =>
        {
            instance.Method();
        })).Throws<ObjectDisposedException>();
    }

    [Test]
    public async Task WhereFieldIsIDisposable()
    {
        var instance = testResult.GetInstance("WhereFieldIsIDisposable");
        var field = instance.Field;
        var isFieldDisposed = GetIsDisposed((object)field);
        await Assert.That(isFieldDisposed).IsFalse();
        instance.Dispose();
        isFieldDisposed = GetIsDisposed((object)field);
        await Assert.That(isFieldDisposed).IsTrue();
    }

    [Test]
    public async Task WhereFieldIsValueType()
    {
        await Assert.That(testResult.Errors.Any(_ => _.Text.Contains("WhereFieldIsValueType"))).IsTrue();
    }

    [Test]
    public async Task WhereFieldIsIDisposableArray()
    {
        var instance = testResult.GetInstance("WhereFieldIsIDisposableArray");
        instance.Dispose();
        await Assert.That((object?)instance.Field).IsNotNull();
    }

    [Test]
    public async Task WhereFieldIsDisposableClassArray()
    {
        var instance = testResult.GetInstance("WhereFieldIsDisposableClassArray");
        instance.Dispose();
        await Assert.That((object?)instance.Field).IsNotNull();
    }

    [Test]
    [Arguments("WithProtectedDisposeManaged",
        "In WithProtectedDisposeManaged.DisposeManaged\r\n")]
    [Arguments("WithOverriddenDisposeManaged",
        "In WithOverriddenDisposeManaged.DisposeManaged\r\n" +
        "In WithProtectedDisposeManaged.DisposeManaged\r\n")]
    [Arguments("WithProtectedDisposeUnmanaged",
        "In WithProtectedDisposeUnmanaged.DisposeUnmanaged\r\n")]
    [Arguments("WithOverriddenDisposeUnmanaged",
        "In WithOverriddenDisposeUnmanaged.DisposeUnmanaged\r\n" +
        "In WithProtectedDisposeUnmanaged.DisposeUnmanaged\r\n")]
    [Arguments("WithProtectedDisposeManagedAndDisposeUnmanaged",
        "In WithProtectedDisposeManagedAndDisposeUnmanaged.DisposeManaged\r\n" +
        "In WithProtectedDisposeManagedAndDisposeUnmanaged.DisposeUnmanaged\r\n")]
    [Arguments("WithOverriddenDisposeManagedAndDisposeUnmanaged",
        "In WithOverriddenDisposeManagedAndDisposeUnmanaged.DisposeManaged\r\n" +
        "In WithProtectedDisposeManagedAndDisposeUnmanaged.DisposeManaged\r\n" +
        "In WithOverriddenDisposeManagedAndDisposeUnmanaged.DisposeUnmanaged\r\n" +
        "In WithProtectedDisposeManagedAndDisposeUnmanaged.DisposeUnmanaged\r\n")]
    [Arguments("WithAbstractBaseClass",
        "In WithAbstractBaseClass.DisposeManaged\r\n" +
        "In AbstractWithProtectedDisposeManaged.DisposeManaged\r\n")]
    [Arguments("WithAbstractDisposeManaged",
        "In WithAbstractDisposeManaged.DisposeManaged\r\n")]
    [SuppressMessage("Usage", "TUnit0055", Justification = "Console output is what the weaved code writes to")]
    public async Task ProtectedDisposableTest(string className, string expectedValue)
    {
        var writer = new StringWriter();
        Console.SetOut(writer);

        var instance = testResult.GetInstance(className);
        await Assert.That(GetIsDisposed((object)instance)).IsFalse();
        instance.Dispose();
        await Assert.That(GetIsDisposed((object)instance)).IsTrue();
        await Assert.That(writer.ToString()).IsEqualTo(expectedValue);
    }

    [Test]
    public async Task EnsureTasksAreNotDisposed()
    {
        var instance = testResult.GetInstance("WithTask");
        instance.Dispose();
        await Assert.That((object?)instance.taskField).IsNotNull();
        instance.taskCompletionSource.SetResult(42);
        await Assert.That((int)instance.taskField.Result).IsEqualTo(42);
    }

    [Test]
    public async Task EnsureClassesInSkippedNamespacesAreNotDisposed()
    {
        var instance = testResult.GetInstance("NamespaceToSkip.WhereNamespaceShouldBeSkipped");
        instance.Dispose();
        await Assert.That((object?)instance.disposableField).IsNotNull();
    }

    [Test]
    public async Task WithUnmanagedAndGenericField()
    {
        var instance = testResult.GetGenericInstance("WithUnmanagedAndGenericField`1", typeof(string));
        await Assert.That(GetIsDisposed((object)instance)).IsFalse();
        instance.Dispose();
        await Assert.That(GetIsDisposed((object)instance)).IsTrue();
    }

    [Test]
    public async Task Verity_throws_an_exception()
    {
        var instance = testResult.GetInstance("WithReadOnly");
        await Assert.That(GetIsDisposed((object)instance)).IsFalse();
        instance.Dispose();
        await Assert.That(GetIsDisposed((object)instance)).IsTrue();
        await Assert.That(testResult.Errors.Select(_ => _.Text).Any(_ => _.Contains("WithReadOnly"))).IsFalse();
    }

    [Test]
    public async Task WithUnmanagedAndGenericIDisposableField()
    {
        var instance = testResult.GetGenericInstance("WithUnmanagedAndGenericIDisposableField`1", typeof(Stream));
        await Assert.That(GetIsDisposed((object)instance)).IsFalse();
        instance.Dispose();
        await Assert.That(GetIsDisposed((object)instance)).IsTrue();
    }

    [Test]
    public async Task WithUnmanagedAndGenericStreamField()
    {
        var instance = testResult.GetGenericInstance("WithUnmanagedAndGenericStreamField`1", typeof(MemoryStream));
        await Assert.That(GetIsDisposed((object)instance)).IsFalse();
        instance.Dispose();
        await Assert.That(GetIsDisposed((object)instance)).IsTrue();
    }

    [Test]
    public async Task SimpleWithGenericField()
    {
        var instance = testResult.GetGenericInstance("SimpleWithGenericField`1", typeof(Stream));
        await Assert.That(GetIsDisposed((object)instance)).IsFalse();
        instance.Dispose();
        await Assert.That(GetIsDisposed((object)instance)).IsTrue();
    }

    [Test]
    public async Task WithDisposableLocalFunction()
    {
        var instance = testResult.GetInstance("WithDisposableLocalFunction");
        IEnumerable types = instance.MethodWithLocalFunction();
        await Assert.That((object)types).IsNotNull();
        await Assert.That(types).IsNotEmpty();
    }

    static bool GetIsDisposed(object instance)
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
}