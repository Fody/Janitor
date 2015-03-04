namespace Janitor.Fody.Resharper
{
    using JetBrains.Annotations;
    using JetBrains.Application;
    using JetBrains.ReSharper.Daemon.UsageChecking;
    using JetBrains.ReSharper.Psi;

    [ShellComponent]
    public class MarkDisposeManagedAndUnmanagedAsImplicitelyUsed : IUsageInspectionsSuppressor
    {
        public bool SuppressUsageInspectionsOnElement(IDeclaredElement element, out ImplicitUseKindFlags flags)
        {
            flags = 0;
            var method = element as IMethod;
            if (method == null)
                return false;
            var containingType = method.GetContainingType() as IClass;
            if (containingType == null)
                return false;
            if ((method.ShortName != "DisposeManaged" && method.ShortName != "DisposeUnmanaged") || method.IsStatic)
                return false;
            flags = ImplicitUseKindFlags.Default;
            return true;
        }
    }
}