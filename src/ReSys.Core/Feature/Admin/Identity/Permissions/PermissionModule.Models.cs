using Mapster;

using ReSys.Core.Domain.Identity.Permissions;

namespace ReSys.Core.Feature.Admin.Identity.Permissions;

public static partial class PermissionModule
{
    public static class Models
    {
        public record SelectItem
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
        }

        public record ListItem
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Area { get; set; } = string.Empty;
            public string Resource { get; set; } = string.Empty;
            public string Action { get; set; } = string.Empty;
            public string? DisplayName { get; set; }
            public string? Description { get; set; }
            public string? Value { get; set; }
            public AccessPermission.PermissionCategory? Category { get; set; }
            public DateTimeOffset CreatedAt { get; set; }
            public DateTimeOffset? UpdatedAt { get; set; }
        }

        public record Detail : ListItem
        {
        }

        public sealed class Mapping : IRegister
        {
            public void Register(TypeAdapterConfig config)
            {
                config.NewConfig<AccessPermission, SelectItem>()
                    .Map(dest => dest.Id, src => src.Id)
                    .Map(dest => dest.Name, src => src.Name)
                    .Map(dest => dest.DisplayName, src => src.DisplayName);

                config.NewConfig<AccessPermission, ListItem>()
                    .Map(dest => dest.Id, src => src.Id)
                    .Map(dest => dest.Name, src => src.Name)
                    .Map(dest => dest.Area, src => src.Area)
                    .Map(dest => dest.Resource, src => src.Resource)
                    .Map(dest => dest.Action, src => src.Action)
                    .Map(dest => dest.DisplayName, src => src.DisplayName)
                    .Map(dest => dest.Description, src => src.Description)
                    .Map(dest => dest.Value, src => src.Value)
                    .Map(dest => dest.Category, src => src.Category)
                    .Map(dest => dest.CreatedAt, src => src.CreatedAt)
                    .Map(dest => dest.UpdatedAt, src => src.UpdatedAt);

                config.NewConfig<AccessPermission, Detail>()
                    .Inherits<AccessPermission, ListItem>();
            }
        }
    }
}