using System;
using System.IO;

public class WithExplicitDisposeMethod:IDisposable
{
    void IDisposable.Dispose()
    {
    }
}