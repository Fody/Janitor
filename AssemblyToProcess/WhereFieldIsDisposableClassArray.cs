using System;

public class WhereFieldIsDisposableClassArray :
    IDisposable
{
    public Disposable[] Field = Array.Empty<Disposable>();

    public void Dispose()
    {
    }

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}