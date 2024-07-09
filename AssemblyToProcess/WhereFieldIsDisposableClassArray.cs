using System;

public class WhereFieldIsDisposableClassArray :
    IDisposable
{
    public Disposable[] Field = [];

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