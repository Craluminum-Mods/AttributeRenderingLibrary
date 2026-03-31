using System.Collections.Generic;
using Vintagestory.API.Util;

namespace AttributeRenderingLibrary;

public static class ARL_ObjectCacheUtil
{
    public static Dictionary<string, object> ARL_ObjectCache { get; } = [];

    public static T? TryGet<T>(string key)
    {
        if (ARL_ObjectCache.TryGetValue(key, out var value))
        {
            return (T)value;
        }

        return default;
    }

    public static T? GetOrCreate<T>(string key, CreateCachableObjectDelegate<T> onRequireCreate)
    {
        if (ARL_ObjectCache.TryGetValue(key, out var value))
        {
            return (T?)value;
        }

        T? val = onRequireCreate();
        ARL_ObjectCache[key] = val;
        return val;
    }

    public static bool Delete(string key)
    {
        if (key == null) return false;

        return ARL_ObjectCache.Remove(key);
    }
}
