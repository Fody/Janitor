using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class WhereFieldIsIDisposable : IDisposable
{
    public IDisposable Field;

    public WhereFieldIsIDisposable()
    {
        Field = new Disposable();
    }

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