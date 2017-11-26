using System;

public class WithUnmanagedAndGenericIDisposableField<T> : IDisposable
    where T : IDisposable
{
    public void Dispose()
    {
    }

    void DisposeUnmanaged()
    {
    }

    public T Value { get; set; }
}