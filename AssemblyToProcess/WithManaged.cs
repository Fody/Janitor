using System;

public class WithManaged :
    IDisposable
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

    public string Property { get; set; }

    public void Method()
    {
    }
}