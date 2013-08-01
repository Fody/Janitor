using System;

public class WithUnmanaged:IDisposable
{
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