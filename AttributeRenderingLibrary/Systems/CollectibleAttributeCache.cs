using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace AttributeRenderingLibrary.Systems;

public class CollectibleAttributeCache : ModSystem
{
    private static readonly Dictionary<string, object> CachedAttributes = [];

    public override void Dispose()
    {
        CachedAttributes.Clear();
    }

    public static T? TryGet<T>(string key)
    {
        if (CachedAttributes.TryGetValue(key, out var value))
        {
            return (T)value;
        }

        return default;
    }

    public static T? GetOrCreate<T>(string key, CreateCachableObjectDelegate<T> onRequireCreate)
    {
        if (CachedAttributes.TryGetValue(key, out var value))
        {
            return (T?)value;
        }

        T? val = onRequireCreate();
        CachedAttributes[key] = val;
        return val;
    }

    public static bool Delete(string key)
    {
        if (key == null) return false;

        return CachedAttributes.Remove(key);
    }
}
