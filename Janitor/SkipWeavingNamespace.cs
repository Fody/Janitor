using System;

namespace Janitor;

/// <summary>
/// Skips weaving for all types in the specified namespace.
/// </summary>
[AttributeUsage(
    AttributeTargets.Assembly,
    AllowMultiple = true)]
public sealed class SkipWeavingNamespace :
    Attribute
{
    /// <summary>
    /// Constructs a new instance of <see cref="SkipWeavingNamespace"/>.
    /// </summary>
    /// <param name="namespaceToSkip">The namespace which should be skipped.</param>
    public SkipWeavingNamespace(string namespaceToSkip)
    {
    }
}