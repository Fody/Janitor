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
    }

    public bool DisposeUnmanagedCalled;
    public void Method()
    {
    }
}