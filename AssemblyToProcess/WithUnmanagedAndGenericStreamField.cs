using System;
using System.IO;

public class WithUnmanagedAndGenericStreamField<T> :
    IDisposable
    where T : Stream
{
    public void Dispose()
    {
    }

    void DisposeUnmanaged()
    {
    }

    public T Value { get; set; }
}