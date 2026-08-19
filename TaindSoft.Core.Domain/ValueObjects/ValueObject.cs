namespace TaindSoft.Core.Domain.ValueObjects
{

    /// <summary>
    /// Interface for value objects
    /// </summary>
    public interface IValueObject
    {
        IEnumerable<object?> GetComponentsForEqualityComparison();
    }


    /// <summary>
    /// Base class for value objects
    /// </summary>
    public abstract class ValueObject : IValueObject
    {
        public abstract IEnumerable<object?> GetComponentsForEqualityComparison();

        public override bool Equals(object? obj)
        {
            return obj is not null && obj.GetType() == GetType() && obj is ValueObject valueObject && GetComponentsForEqualityComparison()
                .SequenceEqual(valueObject.GetComponentsForEqualityComparison());
        }

        public override int GetHashCode()
        {
            return GetComponentsForEqualityComparison()
                .Aggregate(default(HashCode), (hashCode, obj) =>
                {
                    hashCode.Add(obj);
                    return hashCode;
                })
                .ToHashCode();
        }
    }
}
