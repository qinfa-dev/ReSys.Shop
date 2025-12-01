using ReSys.Core.Domain.Identity.Permissions;

namespace ReSys.Core.Feature.Common.Security.Authorization.Permissions;

public static partial class Feature
{
    public static partial class Admin
    {
        public static partial class Catalog
        {
            public static class Product
            {
                public static AccessPermission Create  => AccessPermission.Create(name: "admin.product.create",
                    displayName: "Create Product",
                    description: "Allows creating a new product").Value;
                public static AccessPermission List  => AccessPermission.Create(name: "admin.product.list",
                    displayName: "View Products",
                    description: "Allows viewing products").Value;
                public static AccessPermission View  => AccessPermission.Create(name: "admin.product.view",
                    displayName: "View Product Details",
                    description: "Allows viewing detailed information about a product").Value;
                public static AccessPermission Update  => AccessPermission.Create(name: "admin.product.update",
                    displayName: "Update Product",
                    description: "Allows updating an existing product").Value;
                public static AccessPermission Delete  => AccessPermission.Create(name: "admin.product.delete",
                    displayName: "Delete Product",
                    description: "Allows deleting a product").Value;
                public static AccessPermission Activate  => AccessPermission.Create(name: "admin.product.activate",
                    displayName: "Activate Product",
                    description: "Allows activating a product").Value;
                public static AccessPermission Archive  => AccessPermission.Create(name: "admin.product.archive",
                    displayName: "Archive Product",
                    description: "Allows archiving a product").Value;
                public static AccessPermission Draft  => AccessPermission.Create(name: "admin.product.draft",
                    displayName: "Draft Product",
                    description: "Allows setting a product to draft status").Value;
                public static AccessPermission Discontinue  => AccessPermission.Create(name: "admin.product.discontinue",
                    displayName: "Discontinue Product",
                    description: "Allows discontinuing a product").Value;

                public static AccessPermission ImageView  => AccessPermission.Create(name: "admin.product_image.view",
                    displayName: "View Product Images",
                    description: "Allows viewing product images").Value;
                public static AccessPermission ImageUpload  => AccessPermission.Create(name: "admin.product_image.upload",
                    displayName: "Upload Product Image",
                    description: "Allows uploading new images to a product").Value;
                public static AccessPermission Image_Update  => AccessPermission.Create(name: "admin.product_image.update",
                    displayName: "Update Product Image",
                    description: "Allows updating an existing product image").Value;
                public static AccessPermission Image_Delete  => AccessPermission.Create(name: "admin.product_image.delete",
                    displayName: "Delete Product Image",
                    description: "Allows deleting a product image").Value;
                public static AccessPermission Image_Reorder  => AccessPermission.Create(name: "admin.product_image.reorder",
                    displayName: "Reorder Product Images",
                    description: "Allows reordering product images").Value;

                public static AccessPermission Property_View  => AccessPermission.Create(name: "admin.product_property.view",
                    displayName: "View Product Properties",
                    description: "Allows viewing product properties").Value;
                public static AccessPermission Property_Add  => AccessPermission.Create(name: "admin.product_property.add",
                    displayName: "Add Product Property",
                    description: "Allows adding properties to a product").Value;
                public static AccessPermission Property_Update  => AccessPermission.Create(name: "admin.product_property.update",
                    displayName: "Update Product Property",
                    description: "Allows updating an existing product property").Value;
                public static AccessPermission Property_Remove  => AccessPermission.Create(name: "admin.product_property.remove",
                    displayName: "Remove Product Property",
                    description: "Allows removing a property from a product").Value;

                public static AccessPermission OptionType_View  => AccessPermission.Create(name: "admin.product_option_type.view",
                    displayName: "View Product Option Types",
                    description: "Allows viewing product option types").Value;
                public static AccessPermission OptionType_Add  => AccessPermission.Create(name: "admin.product_option_type.add",
                    displayName: "Add Product Option Type",
                    description: "Allows adding option types to a product").Value;
                public static AccessPermission OptionType_Remove  => AccessPermission.Create(name: "admin.product_option_type.remove",
                    displayName: "Remove Product Option Type",
                    description: "Allows removing an option type from a product").Value;

                public static AccessPermission Category_View  => AccessPermission.Create(name: "admin.product_category.view",
                    displayName: "View Product Categories",
                    description: "Allows viewing product categories").Value;
                public static AccessPermission Category_Add  => AccessPermission.Create(name: "admin.product_category.add",
                    displayName: "Add Product Category",
                    description: "Allows adding categories to a product").Value;
                public static AccessPermission Category_Remove  => AccessPermission.Create(name: "admin.product_category.remove",
                    displayName: "Remove Product Category",
                    description: "Allows removing a category from a product").Value;

                public static AccessPermission Store_View  => AccessPermission.Create(name: "admin.product_store.view",
                    displayName: "View Product Stores",
                    description: "Allows viewing product store associations").Value;
                public static AccessPermission Store_Add  => AccessPermission.Create(name: "admin.product_store.add",
                    displayName: "Add Product To Storefront",
                    description: "Allows adding a product to a store").Value;
                public static AccessPermission Store_Update  => AccessPermission.Create(name: "admin.product_store.update",
                    displayName: "Update Product Storefront Settings",
                    description: "Allows updating product settings for a specific store").Value;
                public static AccessPermission Store_Remove  => AccessPermission.Create(name: "admin.product_store.remove",
                    displayName: "Remove Product From Storefront",
                    description: "Allows removing a product from a store").Value;

                public static AccessPermission Analytics_View  => AccessPermission.Create(name: "admin.product_analytics.view",
                    displayName: "View Product Analytics",
                    description: "Allows viewing product analytics data").Value;

                public static AccessPermission[] All =>
                [
                    Create,
                    List,
                    View,
                    Update,
                    Delete,
                    Activate,
                    Archive,
                    Draft,
                    Discontinue,
                    ImageView,
                    ImageUpload,
                    Image_Update,
                    Image_Delete,
                    Image_Reorder,
                    Property_View,
                    Property_Add,
                    Property_Update,
                    Property_Remove,
                    OptionType_View,
                    OptionType_Add,
                    OptionType_Remove,
                    Category_View,
                    Category_Add,
                    Category_Remove,
                    Store_View,
                    Store_Add,
                    Store_Update,
                    Store_Remove,
                    Analytics_View
                ];
            }
        }
    }
}