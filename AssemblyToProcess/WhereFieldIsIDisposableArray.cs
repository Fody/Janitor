using System;

public class WhereFieldIsIDisposableArray :
    IDisposable
{
    public IDisposable[] Field = Array.Empty<IDisposable>();

    public void Dispose()
    {
    }
}