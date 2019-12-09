using System;

public class WithUnmanagedAndGenericField<T> :
    IDisposable
{
    public void Dispose()
    {
    }

    void DisposeUnmanaged()
    {
    }

    public T Value { get; set; }
}