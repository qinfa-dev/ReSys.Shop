using ReSys.Core.Domain.Identity.Permissions;

namespace ReSys.Core.Feature.Common.Security.Authorization.Permissions.Constants;

public static partial class Feature
{
    public static partial class Admin
    {
        public static partial class Catalog
        {
            public static class Variant
            {
                public static AccessPermission Create => AccessPermission.Create(name: "admin.catalog.variant.create",
                    displayName: "Create Product Variant",
                    description: "Allows creating a new product variant").Value;
                public static AccessPermission List => AccessPermission.Create(name: "admin.catalog.variant.list",
                    displayName: "View Product Variants",
                    description: "Allows viewing product variants").Value;
                public static AccessPermission View => AccessPermission.Create(name: "admin.catalog.variant.view",
                    displayName: "View Product Variant Details",
                    description: "Allows viewing detailed information about a product variant").Value;
                public static AccessPermission Update => AccessPermission.Create(name: "admin.catalog.variant.update",
                    displayName: "Update Product Variant",
                    description: "Allows updating an existing product variant").Value;
                public static AccessPermission Delete => AccessPermission.Create(name: "admin.catalog.variant.delete",
                    displayName: "Delete Product Variant",
                    description: "Allows deleting a product variant").Value;
                public static AccessPermission Discontinue => AccessPermission.Create(name: "admin.catalog.variant.discontinue",
                    displayName: "Discontinue Product Variant",
                    description: "Allows discontinuing a product variant").Value;

                public static class Pricing
                {
                    public static AccessPermission View => AccessPermission.Create(name: "admin.catalog.variant.pricing.view",
                        displayName: "View Product Variant Pricing",
                        description: "Allows viewing pricing for a product variant").Value;
                    public static AccessPermission Add => AccessPermission.Create(name: "admin.catalog.variant.pricing.add",
                        displayName: "Add Product Variant Price",
                        description: "Allows adding a price to a product variant").Value;
                    public static AccessPermission Update => AccessPermission.Create(name: "admin.catalog.variant.pricing.update",
                        displayName: "Update Product Variant Price",
                        description: "Allows updating a price for a product variant").Value;
                    public static AccessPermission Remove => AccessPermission.Create(name: "admin.catalog.variant.pricing.delete",
                        displayName: "Delete Product Variant Price",
                        description: "Allows deleting a price from a product variant").Value;

                    public static AccessPermission[] All => [
                            View,
                            Add,
                            Update,
                            Remove
                    ];
                }

                public static class Inventories
                {
                    public static AccessPermission View => AccessPermission.Create(name: "admin.catalog.variant.inventory.view",
                        displayName: "View Product Variant Inventory",
                        description: "Allows viewing inventory for a product variant").Value;
                    public static AccessPermission Set => AccessPermission.Create(name: "admin.catalog.variant.inventory.set",
                        displayName: "Set Product Variant Inventory",
                        description: "Allows setting inventory levels for a product variant").Value;
                    public static AccessPermission Adjust => AccessPermission.Create(name: "admin.catalog.variant.inventory.adjust",
                        displayName: "Adjust Product Variant Inventory",
                        description: "Allows adjusting inventory levels for a product variant").Value;
                    public static AccessPermission MovementsView => AccessPermission.Create(name: "admin.catalog.variant.inventory_movements.view",
                        displayName: "View Product Variant Inventory Movements",
                        description: "Allows viewing inventory movement history for a product variant").Value;

                    public static AccessPermission[] All =>
                    [
                        View,
                        Set,
                        Adjust,
                        MovementsView
                    ];
                }

                public static class OptionValues
                {
                    public static AccessPermission View => AccessPermission.Create(name: "admin.catalog.variant.option_values.view",
                        displayName: "View Product Variant Options",
                        description: "Allows viewing options for a product variant").Value;
                    public static AccessPermission Set => AccessPermission.Create(name: "admin.catalog.variant.option_values.set",
                        displayName: "Set Product Variant Option",
                        description: "Allows setting an option for a product variant").Value;
                    public static AccessPermission Remove => AccessPermission.Create(name: "admin.catalog.variant.option_values.remove",
                        displayName: "Remove Product Variant Option",
                        description: "Allows removing an option from a product variant").Value;
                    public static AccessPermission[] All => [
                        View,
                        Set,
                        Remove
                    ];
                }

                public static class Images
                {
                    public static AccessPermission View => AccessPermission.Create(name: "admin.catalog.variant.images.view",
                        displayName: "View Product Variant Images",
                        description: "Allows viewing images for a product variant").Value;
                    public static AccessPermission Upload => AccessPermission.Create(name: "admin.catalog.variant.images.upload",
                        displayName: "Upload Product Variant Image",
                        description: "Allows uploading an image to a product variant").Value;
                    public static AccessPermission Remove => AccessPermission.Create(name: "admin.catalog.variant.images.delete",
                        displayName: "Delete Product Variant Image",
                        description: "Allows deleting an image from a product variant").Value;

                    public static AccessPermission[] All => [
                        Variant.View,
                        Upload,
                        Remove
                    ];
                }

                public static AccessPermission[] All =>
                [
                    Create,
                    List,
                    View,
                    Update,
                    Delete,
                    Discontinue,
                    View,
                    ..Pricing.All,
                    ..Inventories.All,
                    ..OptionValues.All,
                    ..Images.All
                ];
            }
        }
    }
}
