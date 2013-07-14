using System;
using System.IO;

public class TemplateClass : IDisposable
{
    MemoryStream stream;
    bool isDisposed;
    IntPtr handle;

    public TemplateClass()
    {
        stream = new MemoryStream();
        handle = new IntPtr();
    }

    public void Method()
    {
        if (isDisposed)
        {
            throw new ObjectDisposedException("TemplateClass");
        }
        stream.ReadByte();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public void Dispose(bool disposing)
    {
        if (isDisposed)
        {
            return;
        }
        isDisposed = true;
        if (disposing)
        {
            DisposeManaged();
        }
        DisposeUnmanaged();
    }

    void DisposeUnmanaged()
    {
        CloseHandle(handle);
        handle = IntPtr.Zero;
    }

    void DisposeManaged()
    {
        if (stream != null)
        {
            stream.Dispose();
            stream = null;
        }
    }

    static extern Boolean CloseHandle(IntPtr handle);

    ~TemplateClass()
    {
        Dispose(false);
    }
}
