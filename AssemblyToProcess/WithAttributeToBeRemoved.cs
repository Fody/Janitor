using Janitor;
#pragma warning disable 169

[SkipWeaving]
public class WithAttributeToBeRemoved
{
    [SkipWeaving]
    bool DisposeManagedCalled;
}