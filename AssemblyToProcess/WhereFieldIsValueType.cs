using System;

public class WhereFieldIsValueType :
    IDisposable
{
    public Disposable Field = new Disposable();

    public void Dispose()
    {
    }

    public struct Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}