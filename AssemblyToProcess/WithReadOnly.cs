using System;
using System.IO;
// ReSharper disable NotAccessedField.Local

public class WithReadOnly :
    IDisposable
{
    readonly MemoryStream stream;

    public void Dispose()
    {
    }

    public WithReadOnly()
    {
        stream = new MemoryStream();
    }
}