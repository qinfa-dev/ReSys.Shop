namespace ReSys.Core.Feature.Common.Systems.Options;
/// <summary>
/// Admin panel configuration options.
/// </summary>
public sealed class AdminPanelOption : SystemOptionBase
{
    public const string Section = "AdminPanel";

    public AdminPanelOption()
    {
        SystemName = "Ruanfa.AdminPanel";
        DefaultPage = "/dashboard";
    }
}