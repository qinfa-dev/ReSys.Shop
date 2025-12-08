using ReSys.Core.Domain.Identity.Permissions;

namespace ReSys.Core.Feature.Common.Security.Authorization.Permissions.Constants;

public static partial class Feature
{
    public static partial class Storefront
    {
        public static class Cart
        {
            public static AccessPermission Add => AccessPermission.Create(name: "store.cart.add",
                displayName: "Add to Cart",
                description: "Allows adding items to cart").Value;
            public static AccessPermission Remove => AccessPermission.Create(name: "store.cart.remove",
                displayName: "Remove from Cart",
                description: "Allows removing items from cart").Value;
            public static AccessPermission Update => AccessPermission.Create(name: "store.cart.update",
                displayName: "Update Cart",
                description: "Allows updating cart items").Value;
            public static AccessPermission View => AccessPermission.Create(name: "store.cart.view",
                displayName: "View Cart",
                description: "Allows viewing cart").Value;
            public static AccessPermission Clear => AccessPermission.Create(name: "store.cart.clear",
                displayName: "Clear Cart",
                description: "Allows clearing cart").Value;

            public static AccessPermission[] All => [Add, Remove, Update, View, Clear];
        }
    }
}