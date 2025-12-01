using ReSys.Core.Domain.Identity.Permissions;

namespace ReSys.Core.Feature.Common.Security.Authorization.Permissions;

public static partial class Feature
{
    public static partial class Admin
    {
        public static partial class Catalog
        {
            public static class Variant
            {
                public static AccessPermission Create => AccessPermission.Create(name: "admin.variant.create",
                    displayName: "Create Product Variant",
                    description: "Allows creating a new product variant").Value;
                public static AccessPermission List => AccessPermission.Create(name: "admin.variant.list",
                    displayName: "View Product Variants",
                    description: "Allows viewing product variants").Value;
                public static AccessPermission View => AccessPermission.Create(name: "admin.variant.view",
                    displayName: "View Product Variant Details",
                    description: "Allows viewing detailed information about a product variant").Value;
                public static AccessPermission Update => AccessPermission.Create(name: "admin.variant.update",
                    displayName: "Update Product Variant",
                    description: "Allows updating an existing product variant").Value;
                public static AccessPermission Delete => AccessPermission.Create(name: "admin.variant.delete",
                    displayName: "Delete Product Variant",
                    description: "Allows deleting a product variant").Value;
                public static AccessPermission Discontinue => AccessPermission.Create(name: "admin.variant.discontinue",
                    displayName: "Discontinue Product Variant",
                    description: "Allows discontinuing a product variant").Value;

                public static AccessPermission Pricing_View => AccessPermission.Create(name: "admin.variant_pricing.view",
                    displayName: "View Product Variant Pricing",
                    description: "Allows viewing pricing for a product variant").Value;
                public static AccessPermission Pricing_Add => AccessPermission.Create(name: "admin.variant_pricing.add",
                    displayName: "Add Product Variant Price",
                    description: "Allows adding a price to a product variant").Value;
                public static AccessPermission Pricing_Update => AccessPermission.Create(name: "admin.variant_pricing.update",
                    displayName: "Update Product Variant Price",
                    description: "Allows updating a price for a product variant").Value;
                public static AccessPermission Pricing_Delete => AccessPermission.Create(name: "admin.variant_pricing.delete",
                    displayName: "Delete Product Variant Price",
                    description: "Allows deleting a price from a product variant").Value;

                public static AccessPermission Inventory_View => AccessPermission.Create(name: "admin.variant_inventory.view",
                    displayName: "View Product Variant Inventory",
                    description: "Allows viewing inventory for a product variant").Value;
                public static AccessPermission Inventory_Set => AccessPermission.Create(name: "admin.variant_inventory.set",
                    displayName: "Set Product Variant Inventory",
                    description: "Allows setting inventory levels for a product variant").Value;
                public static AccessPermission Inventory_Adjust => AccessPermission.Create(name: "admin.variant_inventory.adjust",
                    displayName: "Adjust Product Variant Inventory",
                    description: "Allows adjusting inventory levels for a product variant").Value;
                public static AccessPermission Inventory_Movements_View => AccessPermission.Create(name: "admin.variant_inventory_movements.view",
                    displayName: "View Product Variant Inventory Movements",
                    description: "Allows viewing inventory movement history for a product variant").Value;

                public static AccessPermission Options_View => AccessPermission.Create(name: "admin.variant_options.view",
                    displayName: "View Product Variant Options",
                    description: "Allows viewing options for a product variant").Value;
                public static AccessPermission Options_Set => AccessPermission.Create(name: "admin.variant_options.set",
                    displayName: "Set Product Variant Option",
                    description: "Allows setting an option for a product variant").Value;
                public static AccessPermission Options_Remove => AccessPermission.Create(name: "admin.variant_options.remove",
                    displayName: "Remove Product Variant Option",
                    description: "Allows removing an option from a product variant").Value;

                public static AccessPermission Images_View => AccessPermission.Create(name: "admin.variant_images.view",
                    displayName: "View Product Variant Images",
                    description: "Allows viewing images for a product variant").Value;
                public static AccessPermission Images_Upload => AccessPermission.Create(name: "admin.variant_images.upload",
                    displayName: "Upload Product Variant Image",
                    description: "Allows uploading an image to a product variant").Value;
                public static AccessPermission Images_Delete => AccessPermission.Create(name: "admin.variant_images.delete",
                    displayName: "Delete Product Variant Image",
                    description: "Allows deleting an image from a product variant").Value;

                public static AccessPermission[] All =>
                [
                    Create,
                    List,
                    View,
                    Update,
                    Delete,
                    Discontinue,
                    Pricing_View,
                    Pricing_Add,
                    Pricing_Update,
                    Pricing_Delete,
                    Inventory_View,
                    Inventory_Set,
                    Inventory_Adjust,
                    Inventory_Movements_View,
                    Options_View,
                    Options_Set,
                    Options_Remove,
                    Images_View,
                    Images_Upload,
                    Images_Delete
                ];
            }
        }
    }
}
