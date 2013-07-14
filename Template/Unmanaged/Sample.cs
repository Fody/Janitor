using System;
using System.Runtime.InteropServices;

namespace UnmanagedBefore
{

    public class Sample : IDisposable
    {
        IntPtr handle;

        public Sample()
        {
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

        [DllImport("kernel32.dll", SetLastError=true)]
        static extern bool CloseHandle(IntPtr hObject);

    }

}

namespace UnmanagedAfter
{
 
    public class Sample : IDisposable
    {
        bool isDisposed;
        IntPtr handle;

        public Sample()
        {
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
            if (isDisposed)
            {
                return;
            }
            isDisposed = true;
            DisposeUnmanaged();
            GC.SuppressFinalize(this);
        }

        void DisposeUnmanaged()
        {
            CloseHandle(handle);
            handle = IntPtr.Zero;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern Boolean CloseHandle(IntPtr handle);

        ~Sample()
        {
            Dispose();
        }
    }
}
