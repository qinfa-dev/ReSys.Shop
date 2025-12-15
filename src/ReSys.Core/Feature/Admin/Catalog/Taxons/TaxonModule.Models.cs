using System.Text.Json.Serialization;

using Mapster;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using ReSys.Core.Common.Models.Wrappers.Queryable;
using ReSys.Core.Domain.Catalog.Taxonomies.Images;
using ReSys.Core.Domain.Catalog.Taxonomies.Rules;
using ReSys.Core.Domain.Catalog.Taxonomies.Taxa;

namespace ReSys.Core.Feature.Catalog.Taxons;

public static partial class TaxonModule
{
    public static class Models
    {
        public record Parameter : 
            IHasParameterizableName, 
            IHasUniqueName, 
            IHasPosition, 
            IHasMetadata
        {
            public Guid TaxonomyId { get; set; }
            public Guid? ParentId { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Presentation { get; set; } = string.Empty;
            public string? Description { get; set; }
            public bool HideFromNav { get; set; }
            public int Position { get; set; }
            public bool Automatic { get; set; }
            public string? RulesMatchPolicy { get; set; }
            public string? SortOrder { get; set; }
            public string? MetaTitle { get; set; }
            public string? MetaDescription { get; set; }
            public string? MetaKeywords { get; set; }
            public IDictionary<string, object?>? PublicMetadata { get; set; }
            public IDictionary<string, object?>? PrivateMetadata { get; set; }
        }

        public class HierarchyParameter : QueryableParams
        {
            public Guid[]? TaxonomyId { get; init; }
            public Guid? FocusedTaxonId { get; init; }
            public bool? IncludeLeavesOnly { get; init; }
            public bool IncludeHidden { get; init; }
            public int? MaxDepth { get; init; }
        }

        public class ImageParameter : IAsset
        {
            [FromForm(Name = "id")] public Guid? Id { get; set; }
            [FromForm(Name = "type")] public string Type { get; set; } = string.Empty;
            [FromForm(Name = "alt")] public string? Alt { get; set; }
            [FromForm(Name = "url")] public string? Url { get; set; }
            [FromForm(Name = "position")] public int Position { get; set; }
            [FromForm(Name = "file")] public IFormFile? File { get; init; }

            [JsonIgnore]
            public bool Attached => !string.IsNullOrWhiteSpace(value: Url);
        }

        public record RuleParameter
        {
            public string Type { get; set; } = null!;
            public string Value { get; set; } = null!;
            public string MatchPolicy { get; set; } = TaxonRule.Constraints.MatchPolicies[0];
            public string? PropertyName { get; set; }
        }

        public sealed class ParameterValidator : AbstractValidator<Parameter>
        {
            public ParameterValidator()
            {
                const string prefix = nameof(Taxon);

                RuleFor(expression: x => x.TaxonomyId)
                    .NotEmpty()
                    .WithErrorCode(errorCode: "Taxon.TaxonomyIdRequired")
                    .WithMessage(errorMessage: "Taxonomy ID is required for taxon.");

                RuleFor(expression: x => x.RulesMatchPolicy)
                    .Must(predicate: x => Taxon.Constraints.RulesMatchPolicies.Contains(value: x))
                    .When(predicate: x => x.Automatic)
                    .WithErrorCode(errorCode: "Taxon.InvalidRulesMatchPolicy")
                    .WithMessage(errorMessage: $"Rules match policy must be one of: {string.Join(separator: ", ", value: Taxon.Constraints.RulesMatchPolicies)}.");

                RuleFor(expression: x => x.SortOrder)
                    .Must(predicate: x => Taxon.Constraints.SortOrders.Contains(value: x))
                    .When(predicate: x => x.Automatic)
                    .WithErrorCode(errorCode: "Taxon.InvalidSortOrder")
                    .WithMessage(errorMessage: $"Sort order must be one of: {string.Join(separator: ", ", value: Taxon.Constraints.SortOrders)}.");

                this.AddParameterizableNameRules(prefix: prefix);
                this.AddMetadataSupportRules(prefix: prefix);
                this.AddPositionRules(prefix: prefix);
            }
        }

        public sealed class ImageParameterValidator : AbstractValidator<ImageParameter>
        {
            public ImageParameterValidator()
            {
               this.AddAssetRules(prefix: nameof(TaxonImage));
                RuleFor(expression: x => x.File)
                    .ApplyImageFileRules(condition: m => string.IsNullOrEmpty(value: m.Url));
            }
        }

        public sealed class RuleParameterValidator : AbstractValidator<RuleParameter>
        {
            public RuleParameterValidator()
            {
                RuleFor(expression: x => x.Type)
                    .NotEmpty()
                    .MaximumLength(maximumLength: TaxonRule.Constraints.TypeMaxLength);

                RuleFor(expression: x => x.Value)
                    .NotEmpty()
                    .MaximumLength(maximumLength: TaxonRule.Constraints.ValueMaxLength);

                RuleFor(expression: x => x.MatchPolicy)
                    .NotEmpty();

                RuleFor(expression: x => x.PropertyName)
                    .MaximumLength(maximumLength: TaxonRule.Constraints.PropertyNameMaxLength)
                    .When(predicate: x => !string.IsNullOrEmpty(value: x.PropertyName));
            }
        }

        public record SelectItem
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Presentation { get; set; } = string.Empty;
        }

        public record ListItem
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Presentation { get; set; } = string.Empty;
            public string TaxonomyName { get; set; } = string.Empty;
            public string? ParentName { get; set; }
            public string? Description { get; set; }
            public string Permalink { get; set; } = string.Empty;
            public string PrettyName { get; set; } = string.Empty;
            public bool HideFromNav { get; set; }
            public int Position { get; set; }
            public bool Automatic { get; set; }
            public string SortOrder { get; set; } = string.Empty;
            public string? MetaTitle { get; set; }
            public string? MetaDescription { get; set; }
            public string? MetaKeywords { get; set; }
            public int ChildrenCount { get; set; }
            public DateTimeOffset CreatedAt { get; set; }
            public DateTimeOffset? UpdatedAt { get; set; }
        }

        public record Detail : Parameter, IHasIdentity<Guid>
        {
            public Guid Id { get; set; }
        }

        public record RuleItem
        {
            public Guid Id { get; init; }
            public string Type { get; init; } = null!;
            public string Value { get; init; } = null!;
            public string MatchPolicy { get; init; } = TaxonRule.Constraints.MatchPolicies[0];
            public string? PropertyName { get; init; }
            public DateTime CreatedAt { get; init; }
            public DateTime? UpdatedAt { get; init; }
        }

        public record ImageItem
        {
            public Guid? Id { get; set; }
            public string Type { get; set; } = string.Empty;
            public string? Alt { get; set; }
            public string? Url { get; set; }
            public int Position { get; set; }
        }

        public record TreeListItem
        {
            /// <summary>
            /// The complete tree structure starting from root(s).
            /// </summary>
            public List<TreeNodeItem> Tree { get; init; } = new();

            /// <summary>
            /// Breadcrumb trail from root to focused taxon (if FocusedTaxonId was provided).
            /// </summary>
            public List<TreeNodeItem> Breadcrumbs { get; init; } = new();

            /// <summary>
            /// The focused taxon node (if FocusedTaxonId was provided).
            /// </summary>
            public TreeNodeItem? FocusedNode { get; init; }

            /// <summary>
            /// Subtree rooted at the focused taxon (if FocusedTaxonId was provided).
            /// </summary>
            public TreeNodeItem? FocusedSubtree { get; init; }

            /// <summary>
            /// Total number of nodes in the tree.
            /// </summary>
            public int TotalCount => CountNodes(nodes: Tree);

            /// <summary>
            /// Maximum depth of the tree.
            /// </summary>
            public int MaxDepth => CalculateMaxDepth(nodes: Tree, currentDepth: 0);

            private static int CountNodes(IEnumerable<TreeNodeItem> nodes)
            {
                var count = 0;
                foreach (var node in nodes)
                {
                    count++;
                    count += CountNodes(nodes: node.Children);
                }
                return count;
            }

            private static int CalculateMaxDepth(IEnumerable<TreeNodeItem> nodes, int currentDepth)
            {
                var maxDepth = currentDepth;
                foreach (var node in nodes)
                {
                    var childDepth = CalculateMaxDepth(nodes: node.Children, currentDepth: currentDepth + 1);
                    if (childDepth > maxDepth)
                        maxDepth = childDepth;
                }
                return maxDepth;
            }
        }

        public class TreeNodeItem
        {
            // === Identity & Naming ===

            /// <summary>
            /// Unique identifier for the taxon.
            /// </summary>
            public Guid Id { get; init; }

            /// <summary>
            /// Internal normalized name (slug-like).
            /// </summary>
            public required string Name { get; init; }

            /// <summary>
            /// Human-readable display name.
            /// </summary>
            public required string Presentation { get; init; }

            /// <summary>
            /// Display name with parent chain (e.g., "Electronics -> Computers -> Laptops").
            /// </summary>
            public string? PrettyName { get; init; }

            /// <summary>
            /// URL-friendly path (e.g., "electronics/computers/laptops").
            /// </summary>
            public string? Permalink { get; init; }

            // === Hierarchy ===

            /// <summary>
            /// ID of the parent taxon. Null for root nodes.
            /// </summary>
            public Guid? ParentId { get; init; }

            /// <summary>
            /// Child nodes in the hierarchy.
            /// </summary>
            public List<TreeNodeItem> Children { get; set; } = new();

            // === Ordering & Structure ===

            /// <summary>
            /// Position/order within siblings. Lower values come first.
            /// </summary>
            public int SortOrder { get; init; }

            /// <summary>
            /// Depth in the tree hierarchy. Root = 0.
            /// </summary>
            public int Depth { get; init; }

            /// <summary>
            /// Left value in nested set model (for efficient subtree queries).
            /// </summary>
            public int Lft { get; init; }

            /// <summary>
            /// Right value in nested set model (for efficient subtree queries).
            /// </summary>
            public int Rgt { get; init; }

            // === Metrics ===

            /// <summary>
            /// Number of products directly associated with this taxon.
            /// </summary>
            public int ProductCount { get; init; }

            /// <summary>
            /// Number of immediate children.
            /// </summary>
            public int ChildCount { get; set; }

            // === Images ===

            /// <summary>
            /// URL of the default image for this taxon.
            /// </summary>
            public string? ImageUrl { get; init; }

            /// <summary>
            /// URL of the square/thumbnail image for this taxon.
            /// </summary>
            public string? SquareImageUrl { get; init; }

            // === Computed Properties ===

            /// <summary>
            /// True if this node has any children.
            /// </summary>
            public bool HasChildren { get; set; }

            /// <summary>
            /// True if this is a leaf node (no children).
            /// </summary>
            public bool IsLeaf { get; set; }

            /// <summary>
            /// True if this is a root node (no parent).
            /// </summary>
            public bool IsRoot { get; set; }

            public bool IsChild => ParentId != null;

            // === UI State (Client-Side) ===

            /// <summary>
            /// UI flag: whether this node should be expanded in the tree view.
            /// Typically true for nodes in the active path to a focused node.
            /// </summary>
            public bool IsExpanded { get; set; }

            /// <summary>
            /// UI flag: whether this node is part of the active path to a focused node.
            /// </summary>
            public bool IsInActivePath { get; set; }

            // === Helper Methods ===

            /// <summary>
            /// Gets the total count of descendant nodes (children, grandchildren, etc.).
            /// </summary>
            public int GetDescendantCount()
            {
                var count = Children.Count;
                foreach (var child in Children)
                {
                    count += child.GetDescendantCount();
                }
                return count;
            }

            /// <summary>
            /// Gets all leaf nodes in this subtree.
            /// </summary>
            public IEnumerable<TreeNodeItem> GetLeaves()
            {
                if (IsLeaf)
                {
                    yield return this;
                }
                else
                {
                    foreach (var child in Children)
                    {
                        foreach (var leaf in child.GetLeaves())
                        {
                            yield return leaf;
                        }
                    }
                }
            }

            /// <summary>
            /// Finds a node by ID in this subtree.
            /// </summary>
            public TreeNodeItem? FindNode(Guid id)
            {
                if (Id == id)
                    return this;

                foreach (var child in Children)
                {
                    var found = child.FindNode(id: id);
                    if (found != null)
                        return found;
                }

                return null;
            }

            /// <summary>
            /// Gets the path from root to this node as a list of IDs.
            /// </summary>
            public List<Guid> GetPath()
            {
                var path = new List<Guid> { Id };
                return path;
            }
        }

        public sealed record FlatListItem
        {
            public Guid Id { get; init; }
            public required string Name { get; init; }
            public required string Presentation { get; init; }
            public string? PrettyName { get; init; }
            public int Depth { get; init; }
            public int ProductCount { get; init; }
            public bool HasChildren { get; init; }
            public bool IsExpanded { get; init; }

            public Guid? ParentId { get; set; }

            /// <summary>
            /// Indentation string for display (e.g., "  " repeated by depth).
            /// </summary>
            public string Indent => new string(c: ' ', count: Depth * 2);

            /// <summary>
            /// Display name with indentation prefix.
            /// </summary>
            public string DisplayName => $"{Indent}{Presentation}";
        }

        public sealed class Mapping : IRegister
        {
            public void Register(TypeAdapterConfig config)
            {
                // SelectItem mapping
                config.NewConfig<Taxon, SelectItem>()
                    .Map(dest => dest.Id, src => src.Id)
                    .Map(dest => dest.Name, src => src.Name)
                    .Map(dest => dest.Presentation, src => src.Presentation);

                // ListItem mapping
                config.NewConfig<Taxon, ListItem>()
                    .Map(dest => dest.Id, src => src.Id)
                    .Map(dest => dest.Name, src => src.Name)
                    .Map(dest => dest.Presentation, src => src.Presentation)
                    .Map(dest => dest.TaxonomyName, src => src.Taxonomy.Name)
                    .Map(dest => dest.ParentName, src => src.Parent != null ? src.Parent.Name : null)
                    .Map(dest => dest.Description, src => src.Description)
                    .Map(dest => dest.Permalink, src => src.Permalink)
                    .Map(dest => dest.PrettyName, src => src.PrettyName)
                    .Map(dest => dest.HideFromNav, src => src.HideFromNav)
                    .Map(dest => dest.Position, src => src.Position)
                    .Map(dest => dest.Automatic, src => src.Automatic)
                    .Map(dest => dest.SortOrder, src => src.SortOrder ?? string.Empty)
                    .Map(dest => dest.MetaTitle, src => src.MetaTitle)
                    .Map(dest => dest.MetaDescription, src => src.MetaDescription)
                    .Map(dest => dest.MetaKeywords, src => src.MetaKeywords)
                    .Map(dest => dest.ChildrenCount, src => src.Children.Count)
                    .Map(dest => dest.CreatedAt, src => src.CreatedAt)
                    .Map(dest => dest.UpdatedAt, src => src.UpdatedAt);

                // Detail mapping (inherits from ListItem)
                config.NewConfig<Taxon, Detail>()
                    .Map(dest => dest.Id, src => src.Id)
                    .Map(dest => dest.TaxonomyId, src => src.TaxonomyId)
                    .Map(dest => dest.ParentId, src => src.ParentId)
                    .Map(dest => dest.Name, src => src.Name)
                    .Map(dest => dest.Presentation, src => src.Presentation)
                    .Map(dest => dest.Description, src => src.Description)
                    .Map(dest => dest.HideFromNav, src => src.HideFromNav)
                    .Map(dest => dest.Position, src => src.Position)
                    .Map(dest => dest.Automatic, src => src.Automatic)
                    .Map(dest => dest.RulesMatchPolicy, src => src.RulesMatchPolicy)
                    .Map(dest => dest.SortOrder, src => src.SortOrder)
                    .Map(dest => dest.MetaTitle, src => src.MetaTitle)
                    .Map(dest => dest.MetaDescription, src => src.MetaDescription)
                    .Map(dest => dest.MetaKeywords, src => src.MetaKeywords)
                    .Map(dest => dest.PublicMetadata, src => src.PublicMetadata)
                    .Map(dest => dest.PrivateMetadata, src => src.PrivateMetadata);

                // Parameter mapping (for updates/creates from Taxon)
                config.NewConfig<Taxon, Parameter>()
                    .Map(dest => dest.TaxonomyId, src => src.TaxonomyId)
                    .Map(dest => dest.ParentId, src => src.ParentId)
                    .Map(dest => dest.Name, src => src.Name)
                    .Map(dest => dest.Presentation, src => src.Presentation)
                    .Map(dest => dest.Description, src => src.Description)
                    .Map(dest => dest.HideFromNav, src => src.HideFromNav)
                    .Map(dest => dest.Position, src => src.Position)
                    .Map(dest => dest.Automatic, src => src.Automatic)
                    .Map(dest => dest.RulesMatchPolicy, src => src.RulesMatchPolicy)
                    .Map(dest => dest.SortOrder, src => src.SortOrder)
                    .Map(dest => dest.MetaTitle, src => src.MetaTitle)
                    .Map(dest => dest.MetaDescription, src => src.MetaDescription)
                    .Map(dest => dest.MetaKeywords, src => src.MetaKeywords)
                    .Map(dest => dest.PublicMetadata, src => src.PublicMetadata)
                    .Map(dest => dest.PrivateMetadata, src => src.PrivateMetadata);

                // TreeNodeItem mapping
                config.NewConfig<Taxon, TreeNodeItem>()
                    .Map(dest => dest.Id, src => src.Id)
                    .Map(dest => dest.Name, src => src.Name)
                    .Map(dest => dest.Presentation, src => src.Presentation)
                    .Map(dest => dest.PrettyName, src => src.PrettyName)
                    .Map(dest => dest.Permalink, src => src.Permalink)
                    .Map(dest => dest.ParentId, src => src.ParentId)
                    .Map(dest => dest.SortOrder, src => src.Position)
                    .Map(dest => dest.Depth, src => src.Depth)
                    .Map(dest => dest.Lft, src => src.Lft)
                    .Map(dest => dest.Rgt, src => src.Rgt)
                    .Map(dest => dest.ProductCount, src => src.Classifications.Count)
                    .Map(dest => dest.ChildCount, src => src.Children.Count)
                    .Map(dest => dest.ImageUrl, src => src.Image != null ? src.Image.Url : null)
                    .Map(dest => dest.SquareImageUrl, src => src.SquareImage != null ? src.SquareImage.Url : null)
                    .Map(dest => dest.HasChildren, src => src.Children.Any())
                    .Map(dest => dest.IsLeaf, src => !src.Children.Any())
                    .Map(dest => dest.IsRoot, src => src.ParentId == null)
                    .Map(dest => dest.Children, src => new List<TreeNodeItem>())
                    .Map(dest => dest.IsExpanded, src => false)
                    .Map(dest => dest.IsInActivePath, src => false);

                // FlatListItem mapping
                config.NewConfig<Taxon, FlatListItem>()
                    .Map(dest => dest.Id, src => src.Id)
                    .Map(dest => dest.Name, src => src.Name)
                    .Map(dest => dest.Presentation, src => src.Presentation)
                    .Map(dest => dest.PrettyName, src => src.PrettyName)
                    .Map(dest => dest.Depth, src => src.Depth)
                    .Map(dest => dest.ProductCount, src => src.Classifications.Count)
                    .Map(dest => dest.HasChildren, src => src.Children.Any())
                    .Map(dest => dest.IsExpanded, src => false)
                    .Map(dest => dest.ParentId, src => src.ParentId);

                // RuleItem mapping
                config.NewConfig<TaxonRule, RuleItem>()
                    .Map(dest => dest.Id, src => src.Id)
                    .Map(dest => dest.Type, src => src.Type)
                    .Map(dest => dest.Value, src => src.Value)
                    .Map(dest => dest.MatchPolicy, src => src.MatchPolicy)
                    .Map(dest => dest.PropertyName, src => src.PropertyName)
                    .Map(dest => dest.CreatedAt, src => src.CreatedAt)
                    .Map(dest => dest.UpdatedAt, src => src.UpdatedAt);

                // RuleParameter mapping (for updates/creates from TaxonRule)
                config.NewConfig<TaxonRule, RuleParameter>()
                    .Map(dest => dest.Type, src => src.Type)
                    .Map(dest => dest.Value, src => src.Value)
                    .Map(dest => dest.MatchPolicy, src => src.MatchPolicy)
                    .Map(dest => dest.PropertyName, src => src.PropertyName);

                // ImageItem mapping (assuming there's a TaxonImage or similar entity)
                config.NewConfig<TaxonImage, ImageItem>()
                    .Map(dest => dest.Id, src => src.Id)
                    .Map(dest => dest.Type, src => src.Type)
                    .Map(dest => dest.Alt, src => src.Alt)
                    .Map(dest => dest.Url, src => src.Url)
                    .Map(dest => dest.Position, src => src.Position);

                // ImageParameter mapping (for updates/creates from TaxonImage)
                config.NewConfig<TaxonImage, ImageParameter>()
                    .Map(dest => dest.Id, src => src.Id)
                    .Map(dest => dest.Type, src => src.Type)
                    .Map(dest => dest.Alt, src => src.Alt)
                    .Map(dest => dest.Url, src => src.Url)
                    .Map(dest => dest.Position, src => src.Position)
                    .Map(dest => dest.File, src => (IFormFile?)null);
            }
        }
    }
}