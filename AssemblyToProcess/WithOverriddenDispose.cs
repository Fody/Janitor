using System;

public class WithOverriddenDisposeManaged : WithProtectedDisposeManaged
{
    protected override void DisposeManaged()
    {
        Console.WriteLine($"In {nameof(WithOverriddenDisposeManaged)}.{nameof(DisposeManaged)}");
        base.DisposeManaged();
    }
}

public class WithOverriddenDisposeUnmanaged : WithProtectedDisposeUnmanaged
{
    protected override void DisposeUnmanaged()
    {
        Console.WriteLine($"In {nameof(WithOverriddenDisposeUnmanaged)}.{nameof(DisposeUnmanaged)}");
        base.DisposeUnmanaged();
    }
}

public class WithOverriddenDisposeManagedAndDisposeUnmanaged : WithProtectedDisposeManagedAndDisposeUnmanaged
{
    protected override void DisposeManaged()
    {
        Console.WriteLine($"In {nameof(WithOverriddenDisposeManagedAndDisposeUnmanaged)}.{nameof(DisposeManaged)}");
        base.DisposeManaged();
    }

    protected override void DisposeUnmanaged()
    {
        Console.WriteLine($"In {nameof(WithOverriddenDisposeManagedAndDisposeUnmanaged)}.{nameof(DisposeUnmanaged)}");
        base.DisposeUnmanaged();
    }
}

public class WithAbstractBaseClass : AbstractWithProtectedDisposeManaged
{
    protected override void DisposeManaged()
    {
        Console.WriteLine($"In {nameof(WithAbstractBaseClass)}.{nameof(DisposeManaged)}");
        base.DisposeManaged();
    }
}

public class WithAbstractDisposeManaged : AbstractWithAbstractDisposeManaged
{
    protected override void DisposeManaged()
    {
        Console.WriteLine($"In {nameof(WithAbstractDisposeManaged)}.{nameof(DisposeManaged)}");
    }
}
