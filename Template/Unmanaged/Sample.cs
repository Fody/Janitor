using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace UnmanagedBefore
{

    public class Sample : IDisposable
    {
        IntPtr handle;

        public Sample()
        {
            handle = new IntPtr();
        }

        void DisposeUnmanaged()
        {
            CloseHandle(handle);
            handle = IntPtr.Zero;
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

namespace UnmanagedAfter
{
 
    public class Sample : IDisposable
    {
        IntPtr handle;
        volatile int disposeSignaled;

        public Sample()
        {
            handle = new IntPtr();
        }

        void DisposeUnmanaged()
        {
            CloseHandle(handle);
            handle = IntPtr.Zero;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern Boolean CloseHandle(IntPtr handle);

        public void Method()
        {
            ThrowIfDisposed();
            //Some code
        }

        void ThrowIfDisposed()
        {
            if (IsDisposed())
            {
                throw new ObjectDisposedException("Sample");
            }
        }

        public void Dispose()
        {
            if (IsDisposed())
            {
                return;
            }
            DisposeUnmanaged();
            GC.SuppressFinalize(this);
        }

        bool IsDisposed()
        {
            return Interlocked.Exchange(ref disposeSignaled, 1) != 0;
        }

        ~Sample()
        {
            Dispose();
        }
    }
}
