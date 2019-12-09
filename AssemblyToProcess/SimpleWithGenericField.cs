using System;

public class SimpleWithGenericField<T> :
    IDisposable
    where T : IDisposable
{
    public void Dispose()
    {
    }

    public T Value { get; set; }
}