using System;

public class WhereFieldIsDisposableClassArray :
    IDisposable
{
    public Disposable[] Field = new Disposable[0];

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