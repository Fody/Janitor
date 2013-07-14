using System;
using System.IO;

public class Simple:IDisposable
{
    MemoryStream stream;

    public void Dispose()
    {
    }
    public Simple()
    {
        stream = new MemoryStream();
    }
    public void Method()
    {
        stream.ReadByte();
    }
    
}