using System;
using System.IO;

public class WithUnmanagedAndDisposableField : IDisposable
{
    public MemoryStream DisposableField;

    public void Dispose()
    {
    }

    public void DisposeUnmanaged()
    {
        DisposeUnmanagedCalled = true;
        Property = "a";
        Method();
    }

    public string Property { get; set; }

    public bool DisposeUnmanagedCalled;

    public void Method()
    {
    }
}