using System;

namespace Janitor;

/// <summary>
/// Used to skip weaving.
/// </summary>
[AttributeUsage(
    AttributeTargets.Class| AttributeTargets.Field,
    Inherited = false)]
public sealed class SkipWeaving :
    Attribute;