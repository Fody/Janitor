using System;

public class WithExplicitDisposeMethod :
    IDisposable
{
    public Explicit Child = new();

    void IDisposable.Dispose()
    {
    }

    public class Explicit : IDisposable
    {
        void IDisposable.Dispose()
        {
        }
    }
}