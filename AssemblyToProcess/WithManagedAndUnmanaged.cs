using System;

public class WithManagedAndUnmanaged:IDisposable
{
    public void Dispose()
    {
    }

    public void DisposeManaged()
    {
        DisposeManagedCalled = true;
        Property = "a";
        Method();
    }

    public bool DisposeManagedCalled;

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