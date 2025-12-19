namespace ReSys.Core.Feature.Admin.Stores;

public static partial class Models
{
    public sealed record ManageItem(Guid ProductId, bool Visible, bool Featured);

    public sealed record ListItem(
        Guid Id,
        Guid StoreId,
        Guid ProductId,
        string ProductName,
        bool Visible,
        bool Featured);

    public sealed record Detail(
        Guid Id,
        Guid StoreId,
        Guid ProductId,
        string ProductName,
        bool Visible,
        bool Featured);
}