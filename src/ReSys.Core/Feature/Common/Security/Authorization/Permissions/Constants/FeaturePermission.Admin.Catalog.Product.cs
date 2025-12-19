using ReSys.Core.Domain.Identity.Permissions;

namespace ReSys.Core.Feature.Common.Security.Authorization.Permissions.Constants;

public static partial class FeaturePermission
{
    public static partial class Admin
    {
        public static partial class Catalog
        {
            public static class Product
            {
                public static AccessPermission Create => AccessPermission.Create(name: "admin.catalog.product.create",
                    displayName: "Create Product",
                    description: "Allows creating a new product").Value;
                public static AccessPermission List => AccessPermission.Create(name: "admin.catalog.product.list",
                    displayName: "View Products",
                    description: "Allows viewing products").Value;
                public static AccessPermission View => AccessPermission.Create(name: "admin.catalog.product.view",
                    displayName: "View Product Details",
                    description: "Allows viewing detailed information about a product").Value;
                public static AccessPermission Update => AccessPermission.Create(name: "admin.catalog.product.update",
                    displayName: "Update Product",
                    description: "Allows updating an existing product").Value;
                public static AccessPermission Delete => AccessPermission.Create(name: "admin.catalog.product.delete",
                    displayName: "Delete Product",
                    description: "Allows deleting a product").Value;
                public static AccessPermission Activate => AccessPermission.Create(name: "admin.catalog.product.activate",
                    displayName: "Activate Product",
                    description: "Allows activating a product").Value;
                public static AccessPermission Archive => AccessPermission.Create(name: "admin.catalog.product.archive",
                    displayName: "Archive Product",
                    description: "Allows archiving a product").Value;
                public static AccessPermission Draft => AccessPermission.Create(name: "admin.catalog.product.draft",
                    displayName: "Draft Product",
                    description: "Allows setting a product to draft status").Value;
                public static AccessPermission Discontinue => AccessPermission.Create(name: "admin.catalog.product.discontinue",
                    displayName: "Discontinue Product",
                    description: "Allows discontinuing a product").Value;

                public static class Images
                {
                    public static AccessPermission View => AccessPermission.Create(name: "admin.catalog.product.image.view",
                        displayName: "View Product Images",
                        description: "Allows viewing product images").Value;
                    public static AccessPermission Upload => AccessPermission.Create(name: "admin.catalog.product.image.upload",
                        displayName: "Upload Product Image",
                        description: "Allows uploading new images to a product").Value;
                    public static AccessPermission Update => AccessPermission.Create(name: "admin.catalog.product.image.update",
                        displayName: "Update Product Image",
                        description: "Allows updating an existing product image").Value;
                    public static AccessPermission Delete => AccessPermission.Create(name: "admin.catalog.product.image.delete",
                        displayName: "Delete Product Image",
                        description: "Allows deleting a product image").Value;
                    public static AccessPermission Reorder => AccessPermission.Create(name: "admin.catalog.product.image.reorder",
                        displayName: "Reorder Product Images",
                        description: "Allows reordering product images").Value;

                    public static AccessPermission[] All =>
                    [
                        View,
                        Upload,
                        Update,
                        Delete,
                        Reorder
                    ];
                }

                public static class Properties
                {
                    public static AccessPermission View => AccessPermission.Create(name: "admin.catalog.product.property.view",
                        displayName: "View Product Properties",
                        description: "Allows viewing product properties").Value;
                    public static AccessPermission Add => AccessPermission.Create(name: "admin.catalog.product.property.add",
                        displayName: "Add Product Property",
                        description: "Allows adding properties to a product").Value;
                    public static AccessPermission Update => AccessPermission.Create(name: "admin.catalog.product.property.update",
                        displayName: "Update Product Property",
                        description: "Allows updating an existing product property").Value;
                    public static AccessPermission Remove => AccessPermission.Create(name: "admin.catalog.product.property.remove",
                        displayName: "Remove Product Property",
                        description: "Allows removing a property from a product").Value;

                    public static AccessPermission[] All =>
                    [
                        View,
                        Add,
                        Update,
                        Remove
                    ];
                }

                public static class OptionTypes
                {
                    public static AccessPermission View => AccessPermission.Create(name: "admin.catalog.product.option_type.view",
                        displayName: "View Product Option Types",
                        description: "Allows viewing product option types").Value;
                    public static AccessPermission Add => AccessPermission.Create(name: "admin.catalog.product.option_type.add",
                        displayName: "Add Product Option Type",
                        description: "Allows adding option types to a product").Value;
                    public static AccessPermission Remove => AccessPermission.Create(name: "admin.catalog.product.option_type.remove",
                        displayName: "Remove Product Option Type",
                        description: "Allows removing an option type from a product").Value;

                    public static AccessPermission[] All =>
                    [
                        View,
                        Update,
                        Remove,
                    ];
                }

                public static class Categories
                {
                    public static AccessPermission View => AccessPermission.Create(name: "admin.catalog.product.category.view",
                        displayName: "View Product Categories",
                        description: "Allows viewing product categories").Value;
                    public static AccessPermission Add => AccessPermission.Create(name: "admin.catalog.product.category.add",
                        displayName: "Add Product Category",
                        description: "Allows adding categories to a product").Value;
                    public static AccessPermission Remove => AccessPermission.Create(name: "admin.catalog.product.category.remove",
                        displayName: "Remove Product Category",
                        description: "Allows removing a category from a product").Value;

                    public static AccessPermission[] All =>
                    [
                        View,
                        Update,
                        Remove,
                    ];
                }

                public static class Stores
                {
                    public static AccessPermission View => AccessPermission.Create(name: "admin.catalog.product.store.view",
                        displayName: "View Product Stores",
                        description: "Allows viewing product store associations").Value;
                    public static AccessPermission Add => AccessPermission.Create(name: "admin.catalog.product.store.add",
                        displayName: "Add Product To Store",
                        description: "Allows adding a product to a store").Value;
                    public static AccessPermission Update => AccessPermission.Create(name: "admin.catalog.product.store.update",
                        displayName: "Update Product Store Settings",
                        description: "Allows updating product settings for a specific store").Value;
                    public static AccessPermission Remove => AccessPermission.Create(name: "admin.catalog.product.store.remove",
                        displayName: "Remove Product From Store",
                        description: "Allows removing a product from a store").Value;

                    public static AccessPermission[] All =>
                    [
                        View,
                        Add,
                        Update,
                        Remove,
                    ];
                }

                public static AccessPermission AnalyticsView => AccessPermission.Create(name: "admin.catalog.product.analytics.view",
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
                    ..Images.All,
                    ..Properties.All,
                    ..OptionTypes.All,
                    ..Categories.All,
                    ..Stores.All,
                    AnalyticsView
                ];
            }
        }
    }
}