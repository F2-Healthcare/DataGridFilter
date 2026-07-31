using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace FilterDataGrid;

internal static class PropertyPathAccessor
{
    private static readonly ConcurrentDictionary<(Type Type, string PropertyName), PropertyInfo> PropertyCache = [];
    private static readonly ConcurrentDictionary<(Type Type, string Path), Func<object, object>> AccessorCache = [];

    public static object GetValue(object source, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Property path cannot be null or empty.", nameof(path));

        return source is null ? null : GetAccessor(source.GetType(), path)(source);
    }

    public static Func<object, object> GetAccessor(Type sourceType, string path)
    {
        if (sourceType is null)
            throw new ArgumentNullException(nameof(sourceType));
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Property path cannot be null or empty.", nameof(path));

        return AccessorCache.GetOrAdd((sourceType, path), key => BuildAccessor(key.Type, key.Path));
    }

    public static PropertyInfo GetPropertyInfo(Type sourceType, string path)
    {
        if (sourceType is null)
            throw new ArgumentException("Value cannot be null.", nameof(sourceType));
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Property path cannot be null or empty.", nameof(path));

        var currentType = sourceType;
        PropertyInfo propertyInfo = null;

        foreach (var segment in SplitPath(path))
        {
            propertyInfo = GetCachedProperty(currentType, segment);
            if (propertyInfo is null)
                return null;

            currentType = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;
        }

        return propertyInfo;
    }

    public static Type GetPathType(Type sourceType, string path)
    {
        var propertyInfo = GetPropertyInfo(sourceType, path);
        return propertyInfo is null
            ? null
            : Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;
    }

    private static PropertyInfo GetCachedProperty(Type type, string propertyName)
    {
        var key = (type, propertyName);
        if (PropertyCache.TryGetValue(key, out var propertyInfo))
            return propertyInfo;

        propertyInfo = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (propertyInfo is not null)
            PropertyCache.TryAdd(key, propertyInfo);

        return propertyInfo;
    }

    private static Func<object, object> BuildAccessor(Type sourceType, string path)
    {
        var segments = SplitPath(path).ToArray();

        // Dictionary paths are resolved dynamically because their keys and value types are not
        // represented by CLR properties.
        if (typeof(IDictionary).IsAssignableFrom(sourceType) ||
            typeof(IDictionary<string, object>).IsAssignableFrom(sourceType))
            return source => GetDynamicValue(source, segments);

        var getters = new List<Func<object, object>>(segments.Length);
        var currentType = sourceType;

        foreach (var segment in segments)
        {
            var propertyInfo = GetCachedProperty(currentType, segment);
            if (propertyInfo is null)
                return _ => null;

            getters.Add(CreateGetter(propertyInfo));
            currentType = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;
        }

        return source =>
        {
            var current = source;
            foreach (var getter in getters)
            {
                if (current is null)
                    return null;

                current = getter(current);
            }

            return current;
        };
    }

    private static Func<object, object> CreateGetter(PropertyInfo propertyInfo)
    {
        var source = Expression.Parameter(typeof(object), "source");
        var instance = Expression.Convert(source, propertyInfo.DeclaringType!);
        var property = Expression.Property(instance, propertyInfo);
        var box = Expression.Convert(property, typeof(object));
        return Expression.Lambda<Func<object, object>>(box, source).Compile();
    }

    private static object GetDynamicValue(object source, IReadOnlyList<string> segments)
    {
        var current = source;
        foreach (var segment in segments)
        {
            if (current is null)
                return null;

            if (TryGetDictionaryValue(current, segment, out var dictionaryValue))
            {
                current = dictionaryValue;
                continue;
            }

            current = GetAccessor(current.GetType(), segment)(current);
        }

        return current;
    }

    private static IEnumerable<string> SplitPath(string path)
    {
        return path.Split('.').Select(segment => segment.Trim()).Where(segment => segment.Length > 0);
    }

    private static bool TryGetDictionaryValue(object source, string key, out object value)
    {
        if (source is IDictionary<string, object> genericDictionary)
            return genericDictionary.TryGetValue(key, out value);

        if (source is IDictionary dictionary && dictionary.Contains(key))
        {
            value = dictionary[key];
            return true;
        }

        value = null;
        return false;
    }
}
