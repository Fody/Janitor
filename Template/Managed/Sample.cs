using System;
using System.IO;
using System.Threading;

namespace ManagedBefore
{

    public class Sample : IDisposable
    {
        MemoryStream stream;

        public Sample()
        {
            stream = new MemoryStream();
        }

        void DisposeManaged()
        {
            if (stream != null)
            {
                stream.Dispose();
                stream = null;
            }
        }

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

namespace ManagedAfter
{
 
    public class Sample : IDisposable
    {
        MemoryStream stream;
        volatile int disposeSignaled;

        public Sample()
        {
            stream = new MemoryStream();
        }

        void DisposeManaged()
        {
            if (stream != null)
            {
                stream.Dispose();
                stream = null;
            }
        }

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
            DisposeManaged();
        }

        bool IsDisposed()
        {
            return Interlocked.Exchange(ref disposeSignaled, 1) != 0;
        }
    }
}
