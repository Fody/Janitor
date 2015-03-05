namespace Janitor.Fody.Resharper
{
    using JetBrains.Application.BuildScript.Application.Zones;
    using JetBrains.ReSharper.Psi;

    [ZoneMarker]
    public class ZoneMarker : IRequire<IClrPsiLanguageZone>
    {
    }
}