using System;
using System.IO;

namespace NamespaceToSkip
{
    public class WhereNamespaceShouldBeSkipped:IDisposable
    {
        public MemoryStream disposableField = new MemoryStream();

        public void Dispose()
        {
        }
    }
}