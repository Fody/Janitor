using System;

public class WithUnmanaged:IDisposable
{
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