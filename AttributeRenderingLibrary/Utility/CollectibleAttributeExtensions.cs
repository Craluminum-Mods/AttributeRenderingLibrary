using AttributeRenderingLibrary.Systems;
using Newtonsoft.Json.Linq;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace AttributeRenderingLibrary;

public static class CollectibleAttributeExtensions
{
    public const string ARL_ATTRIBUTES_KEY = "ARL_attributes";

    public static JsonObject? GetAttribute(JsonObject attributes, string attributeKey, IItemStack? stack)
    {
        if (attributes == null) return null;

        if (stack is not ItemStack itemStack || !attributes.KeyExists(ARL_ATTRIBUTES_KEY) || attributeKey == ARL_ATTRIBUTES_KEY)
        {
            return attributes[attributeKey];
        }

        Variants variants = Variants.FromStack(itemStack);
        return GetTypedAttribute(attributes, attributeKey, itemStack, variants) ?? attributes[attributeKey];
    }

    public static bool GetKeyExists(JsonObject attributes, string attributeKey, IItemStack? stack)
    {
        return GetAttribute(attributes, attributeKey, stack) != null;
    }

    public static bool GetIsTrue(JsonObject attributes, string attributeKey, IItemStack? stack)
    {
        return GetAttribute(attributes, attributeKey, stack)?.AsBool() ?? false;
    }

    public static JsonObject? GetTypedAttribute(JsonObject attributes, string attributeKey, ItemStack stack, Variants variants)
    {
        if (!variants.Any) return null;

        if (attributes.Token?[ARL_ATTRIBUTES_KEY]?[attributeKey] is not JObject container) return null;

        var lookup = container.Properties().ToDictionary(p => p.Name, p => new JsonObject(p.Value));

        if (!variants.FindByVariant(lookup, out JsonObject? rawValue, out string matchedKey)) return null;

        if (rawValue?.Token == null) return null;

        string rawString = rawValue.ToString()!;
        bool isDynamic = rawString.IndexOfAny(['{', '}']) != -1;

        string cacheKey = isDynamic
            ? string.Concat("dynamic:", attributeKey, "-code:", stack.Collectible.Id, "-types:", variants)
            : string.Concat("static:", attributeKey, "-types:", matchedKey);

        return CollectibleAttributeCache.GetOrCreate(cacheKey, () =>
        {
            return isDynamic
                ? variants.ReplacePlaceholders(rawValue.Clone())
                : rawValue;
        });
    }
}