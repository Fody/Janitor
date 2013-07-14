using System;

namespace Janitor
{
    /// <summary>
    /// Used to skip weaving.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Class| AttributeTargets.Field, 
        AllowMultiple = false,
        Inherited = false)]
    public sealed class SkipWeaving:Attribute
    {
    }
}
