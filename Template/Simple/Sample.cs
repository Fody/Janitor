using System;
using System.IO;
using System.Threading;

// ReSharper disable NotAccessedField.Local
namespace SimpleBefore
{

    public class Sample : IDisposable
    {
        MemoryStream stream;

        public Sample()
        {
            stream = new MemoryStream();
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

namespace SimpleAfter
{

    public class Sample : IDisposable
    {
        MemoryStream stream;
        volatile int disposeSignaled;
        bool disposed;

        public Sample()
        {
            stream = new MemoryStream();
        }

        public void Method()
        {
            ThrowIfDisposed();
            //Some code
        }

        void ThrowIfDisposed()
        {
            if (disposed)
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
            if (stream != null)
            {
                stream.Dispose();
                stream = null;
            }
            disposed = true;
        }

    }
}