using System;

public class WithManaged:IDisposable
{
    public void Dispose()
    {
    }
    public void DisposeManaged()
    {
        DisposeManagedCalled = true;
    }

    public bool DisposeManagedCalled;

}