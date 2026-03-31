using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace AttributeRenderingLibrary;

public static class CollectibleAttributeExtensions
{
    public const string ARL_ATTRIBUTES_KEY = "ARL_attributes";

    public static JsonObject? GetTypedAttribute(JsonObject attributes, string attributeKey, ItemStack stack, Variants variants)
    {
        if (!variants.Any) return null;

        string cacheKey = $"ARL_attributes|{attributeKey}|{stack.Collectible.Code}|{variants}";

        return ARL_ObjectCacheUtil.GetOrCreate(cacheKey, () =>
        {
            if (attributes.Token?[ARL_ATTRIBUTES_KEY]?[attributeKey] is not JObject { } attributeContainer)
            {
                return null;
            }

            Dictionary<string, JsonObject> lookup = attributeContainer.Properties()
                .ToDictionary(p => p.Name, p => new JsonObject(p.Value));

            if (variants.FindByVariant(lookup, out JsonObject? attribute))
            {
                return variants.ReplacePlaceholders(attribute?.Clone());
            }
            return null;
        });
    }

    public static JsonObject GetAttribute(JsonObject attributes, string attributeKey, IItemStack? stack)
    {
        if (stack is null || !attributes.KeyExists(ARL_ATTRIBUTES_KEY) || attributeKey == ARL_ATTRIBUTES_KEY)
        {
            return attributes[attributeKey];
        }

        Variants variants = Variants.FromStack((ItemStack)stack);
        JsonObject? attribute = GetTypedAttribute(attributes, attributeKey, (ItemStack)stack, variants);
        return attribute ?? attributes[attributeKey];
    }

    public static bool GetKeyExists(JsonObject attributes, string attributeKey, IItemStack? stack)
    {
        if (stack is null || !attributes.KeyExists(ARL_ATTRIBUTES_KEY) || attributeKey == ARL_ATTRIBUTES_KEY)
        {
            return attributes.KeyExists(attributeKey);
        }

        Variants variants = Variants.FromStack((ItemStack)stack);
        bool? keyExists = GetTypedAttribute(attributes, attributeKey, (ItemStack)stack, variants)?.KeyExists(attributeKey);
        return keyExists ?? attributes.KeyExists(attributeKey);
    }

    public static bool GetIsTrue(JsonObject attributes, string attributeKey, IItemStack? stack)
    {
        if (stack is null || !attributes.KeyExists(ARL_ATTRIBUTES_KEY) || attributeKey == ARL_ATTRIBUTES_KEY)
        {
            return attributes.IsTrue(attributeKey);
        }

        Variants variants = Variants.FromStack((ItemStack)stack);
        bool? isTrue = GetTypedAttribute(attributes, attributeKey, (ItemStack)stack, variants)?.IsTrue(attributeKey);
        return isTrue ?? attributes.IsTrue(attributeKey);
    }
}