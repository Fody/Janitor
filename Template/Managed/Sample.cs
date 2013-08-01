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
            if (disposeSignaled != 0)
            {
                throw new ObjectDisposedException("TemplateClass");
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposeSignaled, 1) != 0)
            {
                return;
            }
            DisposeManaged();
        }

    }
}
