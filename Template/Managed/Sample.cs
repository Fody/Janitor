using System;
using System.IO;

namespace ManagedBefore
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

        void DisposeManaged()
        {
            if (stream != null)
            {
                stream.Dispose();
                stream = null;
            }
        }
    }

}

namespace ManagedAfter
{
 
    public class Sample : IDisposable
    {
        MemoryStream stream;
        bool isDisposed;

        public Sample()
        {
            stream = new MemoryStream();
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
            DisposeManaged();
        }

        void DisposeManaged()
        {
            if (stream != null)
            {
                stream.Dispose();
                stream = null;
            }
        }

    }
}
