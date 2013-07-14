using System;

public class WithManagedAndUnmanaged:IDisposable
{
    public void Dispose()
    {
    }
    public void DisposeManaged()
    {
        DisposeManagedCalled = true;
    }

    public bool DisposeManagedCalled;

    public void DisposeUnmanaged()
    {
        DisposeUnmanagedCalled = true;
    }

    public bool DisposeUnmanagedCalled;
}