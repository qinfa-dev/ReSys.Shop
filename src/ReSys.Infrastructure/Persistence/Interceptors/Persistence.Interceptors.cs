using System.Text.Json;

using MediatR;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Common.Domain.Events;
using ReSys.Core.Domain.Auditing;
using ReSys.Core.Feature.Common.Security.Authentication.Contexts.Interfaces;

namespace ReSys.Infrastructure.Persistence.Interceptors;

public static class PersistenceInterceptors
{
    internal sealed class ActionTracking(
    IUserContext userContext)
    : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            UpdateEntities(context: eventData.Context);
            return base.SavingChanges(eventData: eventData,
                result: result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            UpdateEntities(context: eventData.Context);
            return base.SavingChangesAsync(eventData: eventData,
                result: result,
                cancellationToken: cancellationToken);
        }

        private void UpdateEntities(DbContext? context)
        {
            if (context == null || !userContext.IsAuthenticated)
                return;

            string userString = string.IsNullOrWhiteSpace(value: userContext.UserName) ? "System" : userContext.UserName;
            IEnumerable<EntityEntry> entries = context.ChangeTracker
                .Entries()
                .Where(predicate: e =>
                    e.Entity is IHasAuditable ||
                    e.Entity is IHasAssignable ||
                    e.Entity is ISoftDeletable
                );

            foreach (EntityEntry entry in entries)
            {
                if (entry.Entity is IHasAuditable auditable)
                {
                    if (entry.State == EntityState.Added)
                    {
                        auditable.SetCreator(createdBy: userString);
                    }
                    else if (entry.State == EntityState.Modified || HasChangedOwnedEntities(entry: entry))
                    {
                        entry.Property(propertyName: nameof(IHasAuditable.CreatedAt)).IsModified = false;
                        entry.Property(propertyName: nameof(IHasAuditable.CreatedBy)).IsModified = false;

                        auditable.MarkAsUpdated(updatedBy: userString);
                    }
                }
                if (entry.Entity is IHasAssignable assignable)
                {
                    if (entry.State == EntityState.Added)
                    {
                        assignable.MarkAsAssigned(assignedTo: userString);
                    }
                    else if (entry.State == EntityState.Modified || HasChangedOwnedEntities(entry: entry))
                    {
                        entry.Property(propertyName: nameof(IHasAssignable.AssignedAt)).IsModified = false;
                        entry.Property(propertyName: nameof(IHasAssignable.AssignedBy)).IsModified = false;
                    }
                }
                if (entry.Entity is ISoftDeletable softDeletable)
                {
                    if (entry.State == EntityState.Deleted)
                    {
                        // Soft delete: mark as deleted, set metadata, and prevent hard delete
                        softDeletable.MarkAsDeleted(deletedBy: userString);
                        entry.State = EntityState.Modified;
                    }
                }
            }
        }

        private static bool HasChangedOwnedEntities(EntityEntry entry) =>
            entry.References.Any(predicate: r =>
                r.TargetEntry != null &&
                r.TargetEntry.Metadata.IsOwned() &&
                (r.TargetEntry.State == EntityState.Added || r.TargetEntry.State == EntityState.Modified));
    }

    internal class DispatchDomainEvent(IMediator mediator) : SaveChangesInterceptor
    {
        public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
        {
            // Synchronously dispatch domain events after save
            DispatchDomainEvents(context: eventData.Context).GetAwaiter().GetResult();
            return base.SavedChanges(eventData: eventData, result: result);
        }

        public override async ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
        {
            // Dispatch domain events after async save
            await DispatchDomainEvents(context: eventData.Context, cancellationToken: cancellationToken);
            return await base.SavedChangesAsync(eventData: eventData, result: result, cancellationToken: cancellationToken);
        }

        private async Task DispatchDomainEvents(DbContext? context, CancellationToken cancellationToken = default)
        {
            if (context == null) return;

            // Find entities that have domain events
            var entitiesWithEvents = context.ChangeTracker
                .Entries<IHasDomainEvents>()
                .Where(predicate: e => e.Entity.DomainEvents.Any())
                .ToList();

            if (!entitiesWithEvents.Any()) return;

            // Flatten all domain events
            var domainEvents = entitiesWithEvents
                .SelectMany(selector: e => e.Entity.DomainEvents)
                .ToList();

            // Clear events to avoid re-dispatching
            foreach (var entity in entitiesWithEvents)
            {
                entity.Entity.ClearDomainEvents();
            }

            // Publish each event via MediatR
            foreach (var domainEvent in domainEvents)
            {
                await mediator.Publish(notification: domainEvent, cancellationToken: cancellationToken);
            }
        }
    }

    public sealed class AuditingLog(
    IUserContext currentUserContext,
    IHttpContextAccessor httpContextAccessor)
    : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            UpdateAuditFields(context: eventData.Context);
            CreateAuditLogs(context: eventData.Context);
            return base.SavingChanges(eventData: eventData, result: result);
        }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            UpdateAuditFields(context: eventData.Context);
            CreateAuditLogs(context: eventData.Context);
            return await base.SavingChangesAsync(eventData: eventData, result: result, cancellationToken: cancellationToken);
        }

        private void UpdateAuditFields(DbContext? context)
        {
            if (context == null) return;

            var entries = context.ChangeTracker.Entries<AuditableEntity<Guid>>();
            var userName = currentUserContext.UserName;
            var timestamp = DateTimeOffset.UtcNow;

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = timestamp;
                    entry.Entity.CreatedBy = userName;
                }

                if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = timestamp;
                    entry.Entity.UpdatedBy = userName;
                }
            }
        }

        private void CreateAuditLogs(DbContext? context)
        {
            if (context == null) return;

            var userId = currentUserContext.UserId;
            var userName = currentUserContext.UserName;
            var ipAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

            var entries = context.ChangeTracker.Entries()
                .Where(predicate: e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
                .Where(predicate: e => e.Entity is not AuditLog) // Don't audit the audit logs
                .ToList();

            foreach (var entry in entries)
            {
                var entityName = entry.Entity.GetType().Name;
                var entityId = GetEntityId(entity: entry.Entity);

                if (entityId == Guid.Empty) continue;

                string action = entry.State switch
                {
                    EntityState.Added => AuditAction.Created,
                    EntityState.Modified => AuditAction.Updated,
                    EntityState.Deleted => AuditAction.Deleted,
                    _ => "Unknown"
                };

                var oldValues = entry.State == EntityState.Modified || entry.State == EntityState.Deleted
                    ? SerializeEntity(values: entry.OriginalValues)
                    : null;

                var newValues = entry.State == EntityState.Added || entry.State == EntityState.Modified
                    ? SerializeEntity(values: entry.CurrentValues)
                    : null;

                var changedProperties = entry.State == EntityState.Modified
                    ? JsonSerializer.Serialize(value: entry.Properties.Where(predicate: p => p.IsModified).Select(selector: p => p.Metadata.Name).ToList())
                    : null;

                var auditLog = AuditLog.Create(
                    entityId: entityId,
                    entityName: entityName,
                    action: action,
                    userId: userId,
                    userName: userName,
                    oldValues: oldValues,
                    newValues: newValues,
                    changedProperties: changedProperties,
                    ipAddress: ipAddress
                );

                if (!auditLog.IsError)
                {
                    context.Set<AuditLog>().Add(entity: auditLog.Value);
                }
            }
        }

        private static Guid GetEntityId(object entity)
        {
            var idProperty = entity.GetType().GetProperty(name: "Id");
            if (idProperty == null) return Guid.Empty;

            var idValue = idProperty.GetValue(obj: entity);
            return idValue is Guid guid ? guid : Guid.Empty;
        }

        private static string SerializeEntity(PropertyValues values)
        {
            var dict = new Dictionary<string, object?>();
            foreach (var property in values.Properties)
            {
                dict[key: property.Name] = values[property: property];
            }
            return JsonSerializer.Serialize(value: dict);
        }
    }


}
