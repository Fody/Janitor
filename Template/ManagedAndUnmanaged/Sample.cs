using System;
using System.IO;
using System.Runtime.InteropServices;

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

        public void Method()
        {
            //Some code
        }

        public void Dispose()
        {
            //must be empty
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

    }

}

namespace ManagedAndUnmanagedAfter
{
 
    public class Sample : IDisposable
    {
        MemoryStream stream;
        bool isDisposed;
        IntPtr handle;

        public Sample()
        {
            stream = new MemoryStream();
            handle = new IntPtr();
        }

        public void Method()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException("Sample");
            }
            //Some code
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

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern Boolean CloseHandle(IntPtr handle);

        ~Sample()
        {
            Dispose(false);
        }
    }
}
