namespace ReSys.Core.Common.Domain.Entities;

/// <summary>
/// Base class for Value Objects that provides equality comparison and hashing.
/// For simple value objects, prefer using record/record struct instead.
/// Use this class when you need complex value objects with custom behavior.
/// 
/// For record usage:
/// - Simple: public record struct Money(decimal Amount, string Currency);
/// - Complex: public record Money(decimal Amount, string Currency) : ValueObject;
/// 
/// References:
/// - https://nietras.com/2021/06/14/csharp-10-record-struct/
/// - https://enterprisecraftsmanship.com/posts/value-object-better-implementation/
/// </summary>
[Serializable]
public abstract class ValueObject : IEquatable<ValueObject>, IComparable<ValueObject>, IComparable
{
    private int? _cachedHashCode;

    /// <summary>
    /// Override this method to return the components that determine equality.
    /// </summary>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public virtual bool Equals(ValueObject? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(objA: this,
                objB: other)) return true;
        if (GetUnproxiedType(obj: this) != GetUnproxiedType(obj: other)) return false;

        return GetEqualityComponents().SequenceEqual(second: other.GetEqualityComponents());
    }

    public override bool Equals(object? obj)
    {
        return Equals(other: obj as ValueObject);
    }

    public override int GetHashCode()
    {
        if (_cachedHashCode.HasValue)
            return _cachedHashCode.Value;

        _cachedHashCode = GetEqualityComponents()
            .Aggregate(seed: 1,
                func: (current,
                    obj) =>
                {
                    unchecked
                    {
                        return current * 23 + (obj?.GetHashCode() ?? 0);
                    }
                });

        return _cachedHashCode.Value;
    }

    public virtual int CompareTo(ValueObject? other)
    {
        return CompareTo(obj: other as object);
    }

    public virtual int CompareTo(object? obj)
    {
        if (obj is null) return 1;

        Type thisType = GetUnproxiedType(obj: this);
        Type otherType = GetUnproxiedType(obj: obj);

        if (thisType != otherType)
            return string.Compare(strA: thisType.ToString(),
                strB: otherType.ToString(),
                comparisonType: StringComparison.Ordinal);

        if (obj is not ValueObject other)
            return 1;

        object?[] components = GetEqualityComponents().ToArray();
        object?[] otherComponents = other.GetEqualityComponents().ToArray();

        for (int i = 0; i < Math.Min(val1: components.Length,
                 val2: otherComponents.Length); i++)
        {
            int comparison = CompareComponents(object1: components[i],
                object2: otherComponents[i]);
            if (comparison != 0)
                return comparison;
        }

        return components.Length.CompareTo(value: otherComponents.Length);
    }

    private static int CompareComponents(object? object1, object? object2)
    {
        if (object1 is null && object2 is null) return 0;
        if (object1 is null) return -1;
        if (object2 is null) return 1;

        if (object1 is IComparable comparable1 && object2 is IComparable comparable2)
            return comparable1.CompareTo(obj: comparable2);

        return object1.Equals(obj: object2) ? 0 : string.Compare(strA: object1.ToString(),
            strB: object2.ToString(),
            comparisonType: StringComparison.Ordinal);
    }

    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        return left?.Equals(other: right) ?? right is null;
    }

    public static bool operator !=(ValueObject? left, ValueObject? right)
    {
        return !(left == right);
    }

    public static bool operator <(ValueObject? left, ValueObject? right)
    {
        return left?.CompareTo(other: right) < 0;
    }

    public static bool operator <=(ValueObject? left, ValueObject? right)
    {
        return left?.CompareTo(other: right) <= 0;
    }

    public static bool operator >(ValueObject? left, ValueObject? right)
    {
        return left?.CompareTo(other: right) > 0;
    }

    public static bool operator >=(ValueObject? left, ValueObject? right)
    {
        return left?.CompareTo(other: right) >= 0;
    }

    /// <summary>
    /// Gets the actual type, handling EF Core and NHibernate proxies.
    /// </summary>
    public static Type GetUnproxiedType(object obj)
    {
        ArgumentNullException.ThrowIfNull(argument: obj);

        const string EFCoreProxyPrefix = "Castle.Proxies.";
        const string NHibernateProxyPostfix = "Proxy";

        Type type = obj.GetType();
        string typeString = type.ToString();

        if (typeString.Contains(value: EFCoreProxyPrefix) || typeString.EndsWith(value: NHibernateProxyPostfix))
            return type.BaseType!;

        return type;
    }

    public override string ToString()
    {
        return $"{GetType().Name} {{ {string.Join(separator: ", ", values: GetEqualityComponents().Select(selector: x => x?.ToString() ?? "null"))} }}";
    }
}

/// <summary>
/// Extension methods to help with ValueObject usage in records.
/// </summary>
public static class ValueObjectExtensions
{
    /// <summary>
    /// Helper method for records to implement GetEqualityComponents easily.
    /// </summary>
    public static IEnumerable<object?> ToEqualityComponents<T>(this T value) where T : notnull
    {
        return typeof(T).GetProperties()
            .Where(predicate: p => p.CanRead)
            .Select(selector: p => p.GetValue(obj: value));
    }
}