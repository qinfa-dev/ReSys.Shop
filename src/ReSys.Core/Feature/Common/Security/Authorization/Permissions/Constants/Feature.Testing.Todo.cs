using ReSys.Core.Domain.Identity.Permissions;

namespace ReSys.Core.Feature.Common.Security.Authorization.Permissions.Constants;
public static partial class Feature
{
    public static partial class Testing
    {
        public static class TodoItems
        {
            public static AccessPermission Create => AccessPermission.Create(name: "testing.todo_items.create",
                displayName: "Create Todo Item",
                description: "Allows creating new todo items").Value;
            public static AccessPermission View => AccessPermission.Create(name: "testing.todo_items.view",
                displayName: "View Todo Item",
                description: "Allows viewing todo items").Value;
            public static AccessPermission Update => AccessPermission.Create(name: "testing.todo_items.update",
                displayName: "Update Todo Item",
                description: "Allows updating todo items").Value;
            public static AccessPermission Delete => AccessPermission.Create(name: "testing.todo_items.delete",
                displayName: "Delete Todo Item",
                description: "Allows deleting todo items").Value;
            public static AccessPermission Track => AccessPermission.Create(name: "testing.todo_items.track",
                displayName: "Track Todo Item",
                description: "Allows tracking todo items").Value;
            public static AccessPermission[] All =>
            [
                    Create,
                    View,
                    Update,
                    Delete,
                    Track
            ];
        }
        public static class TodoLists
        {
            public static AccessPermission Create => AccessPermission.Create(name: "testing.todo_lists.create",
                displayName: "Create Todo List",
                description: "Allows creating new todo lists").Value;
            public static AccessPermission List => AccessPermission.Create(name: "testing.todo_lists.list",
                displayName: "List Todo Lists",
                description: "Allows listing todo lists").Value;
            public static AccessPermission View => AccessPermission.Create(name: "testing.todo_lists.view",
                displayName: "View Todo List",
                description: "Allows viewing todo lists").Value;
            public static AccessPermission Update => AccessPermission.Create(name: "testing.todo_lists.update",
                displayName: "Update Todo List",
                description: "Allows updating todo lists").Value;
            public static AccessPermission Delete => AccessPermission.Create(name: "testing.todo_lists.delete",
                displayName: "Delete Todo List",
                description: "Allows deleting todo lists").Value;

            public static AccessPermission[] All =>
            [
                    Create,
                    List,
                    View,
                    Update,
                    Delete
            ];
        }
    }

}
