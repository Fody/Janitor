using System;
using System.IO;

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
        }

    }

}

namespace SimpleAfter
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
            if (stream != null)
            {
                stream.Dispose();
                stream = null;
            }
        }

    }
}


// ReSharper restore NotAccessedField.Local