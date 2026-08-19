using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using TaindSoft.Core.Mapping.Abstractions;

namespace TaindSoft.Core.Mapping
{
    /// <summary>
    /// Default implementation of IObjectMapper using reflection and expression trees
    /// Supports both property-based and constructor-based mapping (immutable DTOs/records)
    /// </summary>
    public sealed class ObjectMapper : IObjectMapper
    {
        private readonly ConcurrentDictionary<(Type Source, Type Destination), MappingDelegate> _mappings;

        private delegate object MappingDelegate(object source, object? destination);

        public ObjectMapper()
        {
            _mappings = new ConcurrentDictionary<(Type, Type), MappingDelegate>();
        }

        public TDestination Map<TDestination>(object source) where TDestination : class
        {
            ArgumentNullException.ThrowIfNull(source);

            return Map<TDestination>(source, source.GetType());
        }

        public TDestination Map<TSource, TDestination>(TSource source)
            where TSource : class
            where TDestination : class
        {
            ArgumentNullException.ThrowIfNull(source);

            return (TDestination)InternalMap(source, typeof(TSource), typeof(TDestination), null)!;
        }

        public void Map<TSource, TDestination>(TSource source, TDestination destination)
            where TSource : class
            where TDestination : class
        {
            ArgumentNullException.ThrowIfNull(source);

            ArgumentNullException.ThrowIfNull(destination);

            InternalMap(source, typeof(TSource), typeof(TDestination), destination);
        }

        private TDestination Map<TDestination>(object source, Type sourceType) where TDestination : class
        {
            ArgumentNullException.ThrowIfNull(source);

            return (TDestination)InternalMap(source, sourceType, typeof(TDestination), null)!;
        }

        private object? InternalMap(object source, Type sourceType, Type destinationType, object? destination)
        {
            if (TryMapCollection(source, sourceType, destinationType, out object? collectionResult))
            {
                return collectionResult;
            }

            (Type sourceType, Type destinationType) key = (sourceType, destinationType);

            if (_mappings.TryGetValue(key, out MappingDelegate? mapper))
            {
                return mapper(source, destination);
            }

            // Auto-generate mapping if not exists
            MappingDelegate generatedMapper = GenerateMapper(sourceType, destinationType);
            _mappings.TryAdd(key, generatedMapper);

            return generatedMapper(source, destination);
        }

        private bool TryMapCollection(object source, Type sourceType, Type destinationType, out object? result)
        {
            result = null;

            if (!typeof(IEnumerable).IsAssignableFrom(sourceType) || sourceType == typeof(string))
            {
                return false;
            }

            if (!typeof(IEnumerable).IsAssignableFrom(destinationType) || destinationType == typeof(string))
            {
                return false;
            }

            Type? sourceElementType = GetCollectionElementType(sourceType);
            Type? destinationElementType = GetCollectionElementType(destinationType);

            if (sourceElementType == null || destinationElementType == null)
            {
                return false;
            }

            // Arrays
            if (destinationType.IsArray)
            {
                List<object?> sourceItems = ((IEnumerable)source).Cast<object?>().ToList();
                Array array = Array.CreateInstance(destinationElementType, sourceItems.Count);

                for (int i = 0; i < sourceItems.Count; i++)
                {
                    object? item = sourceItems[i];
                    if (item == null)
                    {
                        array.SetValue(null, i);
                        continue;
                    }

                    object? mappedItem = destinationElementType.IsAssignableFrom(item.GetType())
                        ? item
                        : InternalMap(item, sourceElementType, destinationElementType, null);

                    array.SetValue(mappedItem, i);
                }

                result = array;
                return true;
            }

            // Concrete list/collection types (e.g., List<T>)
            if (!destinationType.IsInterface && !destinationType.IsAbstract)
            {
                if (Activator.CreateInstance(destinationType) is IList destinationList)
                {
                    foreach (object? item in (IEnumerable)source)
                    {
                        if (item == null)
                        {
                            destinationList.Add(null);
                            continue;
                        }

                        object? mappedItem = destinationElementType.IsAssignableFrom(item.GetType())
                            ? item
                            : InternalMap(item, sourceElementType, destinationElementType, null);

                        destinationList.Add(mappedItem);
                    }

                    result = destinationList;
                    return true;
                }

                return false;
            }

            // Interface/abstract collection targets (IEnumerable<T>, IReadOnlyList<T>, ...)
            Type listType = typeof(List<>).MakeGenericType(destinationElementType);
            if (Activator.CreateInstance(listType) is not IList tempList)
            {
                return false;
            }

            foreach (object? item in (IEnumerable)source)
            {
                if (item == null)
                {
                    tempList.Add(null);
                    continue;
                }

                object? mappedItem = destinationElementType.IsAssignableFrom(item.GetType())
                    ? item
                    : InternalMap(item, sourceElementType, destinationElementType, null);

                tempList.Add(mappedItem);
            }

            result = tempList;
            return true;
        }

        private static Type? GetCollectionElementType(Type type)
        {
            if (type.IsArray)
            {
                return type.GetElementType();
            }

            if (type.IsGenericType)
            {
                return type.GetGenericArguments()[0];
            }

            Type? enumerableInterface = type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

            return enumerableInterface?.GetGenericArguments()[0];
        }

        private MappingDelegate GenerateMapper(Type sourceType, Type destinationType)
        {
            // Capture InternalMap as a delegate so generated expressions can call it recursively
            Func<object, Type, Type, object?, object?> mapFunc = InternalMap;

            ParameterExpression sourceParam = Expression.Parameter(typeof(object), "source");
            ParameterExpression destParam = Expression.Parameter(typeof(object), "destination");
            UnaryExpression castSource = Expression.Convert(sourceParam, sourceType);

            // Get source properties that can be read
            List<PropertyInfo> sourceProperties = sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead)
                .ToList();

            // Check if destination is provided (update existing instance scenario)
            BinaryExpression destIsNull = Expression.Equal(destParam, Expression.Constant(null));

            // STRATEGY 1: Constructor-based mapping (for new instance creation)
            Expression constructorMapping = GenerateConstructorBasedMapping(
                sourceType,
                destinationType,
                sourceProperties,
                castSource,
                mapFunc);

            // STRATEGY 2: Property-based mapping (for existing instance update)
            BlockExpression? propertyMapping = GeneratePropertyBasedMapping(
                destinationType,
                sourceProperties,
                castSource,
                destParam,
                mapFunc);

            // If destination is null, use constructor-based; otherwise use property-based
            ConditionalExpression conditionalMapping = Expression.Condition(
                destIsNull,
                constructorMapping,
                propertyMapping ?? constructorMapping);

            Expression<Func<object, object?, object>> lambda = Expression.Lambda<Func<object, object?, object>>(
                conditionalMapping,
                sourceParam,
                destParam);

            Func<object, object?, object> compiled = lambda.Compile();
            return (src, dst) => compiled(src, dst)!;
        }

        private static Expression GenerateConstructorBasedMapping(
            Type sourceType,
            Type destinationType,
            List<PropertyInfo> sourceProperties,
            Expression castSource,
            Func<object, Type, Type, object?, object?>? mapFunc = null)
        {
            // Find best constructor (with most parameters)
            List<ConstructorInfo> constructors = destinationType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .OrderByDescending(c => c.GetParameters().Length)
                .ToList();

            ConstructorInfo? bestConstructor = null;
            List<Expression>? bestConstructorArgs = null;
            List<PropertyInfo>? unmappedPropertiesForBest = null;

            foreach (ConstructorInfo constructor in constructors)
            {
                ParameterInfo[] parameters = constructor.GetParameters();
                List<Expression> constructorArgs = new();
                bool allParametersMatched = true;

                foreach (ParameterInfo param in parameters)
                {
                    // Try to find matching source property (case-insensitive)
                    PropertyInfo? sourceProp = sourceProperties.FirstOrDefault(p =>
                        string.Equals(p.Name, param.Name, StringComparison.OrdinalIgnoreCase));

                    if (sourceProp != null)
                    {
                        Expression? mappedExpression = null;

                        // Direct assignment (types match exactly)
                        if (param.ParameterType == sourceProp.PropertyType)
                        {
                            mappedExpression = Expression.Property(castSource, sourceProp);
                        }
                        // Assignable types (covariance/inheritance)
                        else if (param.ParameterType.IsAssignableFrom(sourceProp.PropertyType))
                        {
                            MemberExpression sourceAccess = Expression.Property(castSource, sourceProp);
                            mappedExpression = Expression.Convert(sourceAccess, param.ParameterType);
                        }
                        // Enum to string conversion
                        else if (sourceProp.PropertyType.IsEnum && param.ParameterType == typeof(string))
                        {
                            MemberExpression sourceAccess = Expression.Property(castSource, sourceProp);
                            mappedExpression = Expression.Call(sourceAccess, "ToString", Type.EmptyTypes);
                        }
                        // Nullable enum to string conversion
                        else if (Nullable.GetUnderlyingType(sourceProp.PropertyType)?.IsEnum == true && param.ParameterType == typeof(string))
                        {
                            MemberExpression sourceAccess = Expression.Property(castSource, sourceProp);
                            MemberExpression hasValue = Expression.Property(sourceAccess, "HasValue");
                            MemberExpression value = Expression.Property(sourceAccess, "Value");
                            MethodCallExpression toString = Expression.Call(value, "ToString", Type.EmptyTypes);
                            ConstantExpression nullString = Expression.Constant(null, typeof(string));
                            mappedExpression = Expression.Condition(hasValue, toString, nullString);
                        }
                        // Nullable to non-nullable (T? -> T) - use null-coalescing with default
                        else if (Nullable.GetUnderlyingType(sourceProp.PropertyType) == param.ParameterType)
                        {
                            MemberExpression sourceAccess = Expression.Property(castSource, sourceProp);
                            DefaultExpression defaultValue = Expression.Default(param.ParameterType);
                            mappedExpression = Expression.Coalesce(sourceAccess, defaultValue);
                        }
                        // Nullable reference type to non-nullable (string? -> string) - use ?? operator
                        else if (!param.ParameterType.IsValueType &&
                                 !sourceProp.PropertyType.IsValueType &&
                                 param.ParameterType == sourceProp.PropertyType)
                        {
                            MemberExpression sourceAccess = Expression.Property(castSource, sourceProp);
                            DefaultExpression defaultValue = Expression.Default(param.ParameterType);
                            mappedExpression = Expression.Coalesce(sourceAccess, defaultValue);
                        }

                        if (mappedExpression != null)
                        {
                            constructorArgs.Add(mappedExpression);
                        }
                        else
                        {
                            // Cannot convert - try next constructor
                            allParametersMatched = false;
                            break;
                        }
                    }
                    else
                    {
                        // Parameter cannot be matched - try next constructor
                        allParametersMatched = false;
                        break;
                    }
                }

                if (allParametersMatched)
                {
                    // Track the best matching constructor
                    bestConstructor = constructor;
                    bestConstructorArgs = constructorArgs;

                    unmappedPropertiesForBest = [.. sourceProperties
                        .Where(sp => !parameters.Any(p =>
                            string.Equals(p.Name, sp.Name, StringComparison.OrdinalIgnoreCase)))];

                    // Found best constructor with all parameters matched
                    break;
                }
            }

            if (bestConstructor != null && bestConstructorArgs != null)
            {
                // Found a usable constructor - generate NewExpression
                NewExpression newInstance = Expression.New(bestConstructor, bestConstructorArgs);

                // Check if there are additional properties to set (init-only or mutable)
                List<PropertyInfo> destProperties = destinationType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanWrite)
                    .ToList();

                if (unmappedPropertiesForBest != null && unmappedPropertiesForBest.Count != 0)
                {
                    // Create instance and set additional properties
                    ParameterExpression variable = Expression.Variable(destinationType, "dest");
                    BinaryExpression assignVariable = Expression.Assign(variable, newInstance);
                    List<Expression> propertyAssignments = new() { assignVariable };

                    foreach (PropertyInfo sourceProp in unmappedPropertiesForBest)
                    {
                        PropertyInfo? destProp = destProperties.FirstOrDefault(p =>
                            string.Equals(p.Name, sourceProp.Name, StringComparison.OrdinalIgnoreCase));

                        if (destProp == null)
                        {
                            continue;
                        }

                        MemberExpression srcAccess = Expression.Property(castSource, sourceProp);
                        MemberExpression dstAccess = Expression.Property(variable, destProp);

                        if (destProp.PropertyType.IsAssignableFrom(sourceProp.PropertyType))
                        {
                            propertyAssignments.Add(Expression.Assign(dstAccess, srcAccess));
                        }
                        else if (destProp.PropertyType.IsEnum && sourceProp.PropertyType == typeof(int))
                        {
                            // int → enum (e.g., FieldType = (FieldType)fieldType)
                            propertyAssignments.Add(Expression.Assign(dstAccess, Expression.Convert(srcAccess, destProp.PropertyType)));
                        }
                        else if (sourceProp.PropertyType.IsEnum && destProp.PropertyType == typeof(int))
                        {
                            // enum → int (e.g., fieldType = (int)FieldType)
                            propertyAssignments.Add(Expression.Assign(dstAccess, Expression.Convert(srcAccess, typeof(int))));
                        }
                        else if (mapFunc != null && !sourceProp.PropertyType.IsValueType && !destProp.PropertyType.IsValueType)
                        {
                            // Recursive mapping for reference types (nested classes, List<A> → List<B>, etc.)
                            ConstantExpression mapFuncConst = Expression.Constant(mapFunc, typeof(Func<object, Type, Type, object?, object?>));
                            UnaryExpression srcAsObj = Expression.Convert(srcAccess, typeof(object));
                            ConstantExpression srcTypeConst = Expression.Constant(sourceProp.PropertyType);
                            ConstantExpression dstTypeConst = Expression.Constant(destProp.PropertyType);
                            ConstantExpression nullConst = Expression.Constant(null, typeof(object));
                            InvocationExpression mapCallExpr = Expression.Invoke(mapFuncConst, srcAsObj, srcTypeConst, dstTypeConst, nullConst);
                            UnaryExpression castResult = Expression.Convert(mapCallExpr, destProp.PropertyType);
                            // Null guard: if source property is null, assign default instead of calling mapper
                            BinaryExpression isNullExpr = Expression.Equal(Expression.Convert(srcAccess, typeof(object)), Expression.Constant(null));
                            ConditionalExpression guardedExpr = Expression.Condition(isNullExpr, Expression.Default(destProp.PropertyType), castResult);
                            propertyAssignments.Add(Expression.Assign(dstAccess, guardedExpr));
                        }
                    }

                    propertyAssignments.Add(Expression.Convert(variable, typeof(object)));
                    BlockExpression block = Expression.Block([variable], propertyAssignments);
                    return block;
                }
                else
                {
                    // All properties mapped via constructor
                    return Expression.Convert(newInstance, typeof(object));
                }
            }

            // No suitable constructor found - throw descriptive error
            string availableConstructors = string.Join(", ",
                constructors.Select(c => $"ctor({string.Join(", ", c.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"))})"));

            throw new InvalidOperationException(
                $"Cannot map {sourceType.Name} to {destinationType.Name}: " +
                $"No suitable constructor found that matches source properties. " +
                $"Available constructors: {availableConstructors}. " +
                $"Source properties: {string.Join(", ", sourceProperties.Select(p => $"{p.PropertyType.Name} {p.Name}"))}");
        }

        private static BlockExpression? GeneratePropertyBasedMapping(
            Type destinationType,
            List<PropertyInfo> sourceProperties,
            Expression castSource,
            ParameterExpression destParam,
            Func<object, Type, Type, object?, object?>? mapFunc = null)
        {
            // For existing instance mapping (when destination is provided)
            UnaryExpression castDest = Expression.Convert(destParam, destinationType);
            ParameterExpression variable = Expression.Variable(destinationType, "dest");
            List<Expression> blockExpressions = new()
            {
                Expression.Assign(variable, castDest)
            };

            List<PropertyInfo> destProperties = destinationType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .ToList();

            foreach (PropertyInfo sourceProp in sourceProperties)
            {
                PropertyInfo? destProp = destProperties.FirstOrDefault(p =>
                    string.Equals(p.Name, sourceProp.Name, StringComparison.OrdinalIgnoreCase));

                if (destProp == null)
                {
                    continue;
                }

                MemberExpression srcAccess = Expression.Property(castSource, sourceProp);
                MemberExpression dstAccess = Expression.Property(variable, destProp);

                if (destProp.PropertyType.IsAssignableFrom(sourceProp.PropertyType))
                {
                    blockExpressions.Add(Expression.Assign(dstAccess, srcAccess));
                }
                else if (destProp.PropertyType.IsEnum && sourceProp.PropertyType == typeof(int))
                {
                    blockExpressions.Add(Expression.Assign(dstAccess, Expression.Convert(srcAccess, destProp.PropertyType)));
                }
                else if (sourceProp.PropertyType.IsEnum && destProp.PropertyType == typeof(int))
                {
                    blockExpressions.Add(Expression.Assign(dstAccess, Expression.Convert(srcAccess, typeof(int))));
                }
                else if (mapFunc != null && !sourceProp.PropertyType.IsValueType && !destProp.PropertyType.IsValueType)
                {
                    ConstantExpression mapFuncConst = Expression.Constant(mapFunc, typeof(Func<object, Type, Type, object?, object?>));
                    UnaryExpression srcAsObj = Expression.Convert(srcAccess, typeof(object));
                    ConstantExpression srcTypeConst = Expression.Constant(sourceProp.PropertyType);
                    ConstantExpression dstTypeConst = Expression.Constant(destProp.PropertyType);
                    ConstantExpression nullConst = Expression.Constant(null, typeof(object));
                    InvocationExpression mapCallExpr = Expression.Invoke(mapFuncConst, srcAsObj, srcTypeConst, dstTypeConst, nullConst);
                    UnaryExpression castResult = Expression.Convert(mapCallExpr, destProp.PropertyType);
                    BinaryExpression isNullExpr = Expression.Equal(Expression.Convert(srcAccess, typeof(object)), Expression.Constant(null));
                    ConditionalExpression guardedExpr = Expression.Condition(isNullExpr, Expression.Default(destProp.PropertyType), castResult);
                    blockExpressions.Add(Expression.Assign(dstAccess, guardedExpr));
                }
            }

            blockExpressions.Add(Expression.Convert(variable, typeof(object)));

            BlockExpression block = Expression.Block([variable], blockExpressions);
            return block;
        }
    }
}
