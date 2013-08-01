using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace ManagedAndUnmanagedBefore
{

    public class Sample : IDisposable
    {
        MemoryStream stream;
        IntPtr handle;

        public Sample()
        {
            stream = new MemoryStream();
            handle = new IntPtr();
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

        [DllImport("kernel32.dll", SetLastError=true)]
        static extern bool CloseHandle(IntPtr hObject);

        public void Method()
        {
            //Some code
        }

        public void Dispose()
        {
            //must be empty
        }
    }

}

namespace ManagedAndUnmanagedAfter
{
 
    public class Sample : IDisposable
    {
        MemoryStream stream;
        IntPtr handle;
        volatile int disposeSignaled;

        public Sample()
        {
            stream = new MemoryStream();
            handle = new IntPtr();
        }

        public void Method()
        {
            ThrowIfDisposed();
            //Some code
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
        static extern Boolean CloseHandle(IntPtr handle);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void ThrowIfDisposed()
        {
            if (IsDisposed())
            {
                throw new ObjectDisposedException("Sample");
            }
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

        ~Sample()
        {
            Dispose(false);
        }
    }
}
