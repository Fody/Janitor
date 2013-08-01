using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

public class TemplateClass : IDisposable
{
    MemoryStream stream;
    IntPtr handle;
    volatile int disposeSignaled;

    public TemplateClass()
    {
        stream = new MemoryStream();
        handle = new IntPtr();
    }

    public void Method()
    {
        ThrowIfDisposed();
        stream.ReadByte();
    }

    void ThrowIfDisposed()
    {
        if (IsDisposed())
        {
            throw new ObjectDisposedException("TemplateClass");
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public void Dispose(bool disposing)
    {
        if (IsDisposed())
        {
            return;
        }
        if (disposing)
        {
            DisposeManaged();
        }
        DisposeUnmanaged();
    }

    bool IsDisposed()
    {
        return Interlocked.Exchange(ref disposeSignaled, 1) != 0;
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
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool CloseHandle(IntPtr handle);

    ~TemplateClass()
    {
        Dispose(false);
    }
}
