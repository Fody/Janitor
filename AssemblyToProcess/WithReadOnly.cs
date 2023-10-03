using System;
using System.IO;
// ReSharper disable NotAccessedField.Local

public class WithReadOnly :
    IDisposable
{
    readonly MemoryStream stream = new();

    public void Dispose()
    {
    }
}