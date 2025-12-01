namespace ReSys.Core.Feature.Common.Systems.Options;
/// <summary>
/// Storefront configuration options.
/// </summary>
public sealed class StorefrontOption : SystemOptionBase
{
    public const string Section = "Storefront";
    public StorefrontOption()
    {
        SystemName = "Ruanfa.Storefront";
        DefaultPage = "/home";
    }
}