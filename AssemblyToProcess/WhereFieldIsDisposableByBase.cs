using System;
using DisposeInBase;

public class WhereFieldIsDisposableByBase :
    IDisposable
{
    public Child Child = new();

    public void Dispose()
    {
    }

    public void Method()
    {
    }
}