using System;
using System.IO;

public class WithReadOnly:IDisposable
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