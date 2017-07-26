using System;

namespace Janitor.Fody
{
    [AttributeUsage(
        AttributeTargets.Assembly,
        AllowMultiple = true)]
    public sealed class SkipWeavingNamespace : Attribute
    {
        /// <summary>
        /// Skips weaving for all types in the specified namespace.
        /// </summary>
        /// <param name="namespaceToSkip">The namespace which should be skipped.</param>
        public SkipWeavingNamespace(string namespaceToSkip)
        {
            this.namespaceToSkip = namespaceToSkip;
        }

        private string namespaceToSkip;
    }
}