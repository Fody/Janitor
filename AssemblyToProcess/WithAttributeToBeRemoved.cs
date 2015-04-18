using Janitor;

[SkipWeaving]
public class WithAttributeToBeRemoved
{
    [SkipWeaving]
    bool DisposeManagedCalled;
}