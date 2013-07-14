using System;
using DisposeInBase;

public class WhereFieldIsDisposableByBase:IDisposable
{
    public Child Child;

    public void Dispose()
    {
    }
    public WhereFieldIsDisposableByBase()
    {
        Child = new Child();
    }
    
}