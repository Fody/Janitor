using System;

public class WhereFieldIsIDisposableArray :
    IDisposable
{
    public IDisposable[] Field = [];

    public void Dispose()
    {
    }
}