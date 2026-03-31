using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace AttributeRenderingLibrary;

public static class CollectibleAttributeExtensions
{
    public const string STFA_ATTRIBUTES_KEY = "STFA_attributes";

    public static JsonObject? GetTypedAttribute(JsonObject collectibleAttributes, string attributeKey, Variants variants)
    {
        if (collectibleAttributes.Token?[STFA_ATTRIBUTES_KEY]?[attributeKey] is not JObject { } attributeContainer) return null;

        Dictionary<string, JsonObject> lookup = attributeContainer.Properties()
            .ToDictionary(p => p.Name, p => new JsonObject(p.Value));

        if (variants.FindByVariant(lookup, out JsonObject? attribute))
        {
            return variants.ReplacePlaceholders(attribute?.Clone());
        }
        return null;
    }

    public static bool? IsTrue(JsonObject collectibleAttributes, string attributeKey, Variants variants)
    {
        if (collectibleAttributes.Token?[STFA_ATTRIBUTES_KEY]?[attributeKey] is not JObject { } attributeContainer) return null;

        Dictionary<string, bool> lookup = attributeContainer.Properties()
            .ToDictionary(p => p.Name, p => (bool)p.Value);

        return variants.IsTrue(lookup);
    }

    public static JsonObject GetAttribute(JsonObject collectibleAttributes, string attributeKey, IItemStack? stack)
    {
        if (stack is null || !collectibleAttributes.KeyExists(STFA_ATTRIBUTES_KEY) || attributeKey == STFA_ATTRIBUTES_KEY)
        {
            return collectibleAttributes[attributeKey];
        }

        Variants variants = Variants.FromStack((ItemStack)stack);
        if (!variants.Any)
        {
            return collectibleAttributes[attributeKey];
        }

        JsonObject? typedAttribute = GetTypedAttribute(collectibleAttributes, attributeKey, variants);
        return typedAttribute ?? collectibleAttributes[attributeKey];
    }

    public static bool GetKeyExists(JsonObject collectibleAttributes, string attributeKey, IItemStack? stack)
    {
        if (stack is null || !collectibleAttributes.KeyExists(STFA_ATTRIBUTES_KEY) || attributeKey == STFA_ATTRIBUTES_KEY)
        {
            return collectibleAttributes.KeyExists(attributeKey);
        }

        Variants variants = Variants.FromStack((ItemStack)stack);
        if (!variants.Any)
        {
            return collectibleAttributes.KeyExists(attributeKey);
        }

        JsonObject? typedAttribute = GetTypedAttribute(collectibleAttributes, attributeKey, variants);
        if (typedAttribute != null) return true;

        return collectibleAttributes.KeyExists(attributeKey);
    }

    public static bool GetIsTrue(JsonObject collectibleAttributes, string attributeKey, IItemStack? stack)
    {
        if (stack is null || !collectibleAttributes.KeyExists(STFA_ATTRIBUTES_KEY) || attributeKey == STFA_ATTRIBUTES_KEY)
        {
            return collectibleAttributes.IsTrue(attributeKey);
        }

        Variants variants = Variants.FromStack((ItemStack)stack);
        if (!variants.Any)
        {
            return collectibleAttributes.IsTrue(attributeKey);
        }

        return IsTrue(collectibleAttributes, attributeKey, variants) ?? collectibleAttributes.IsTrue(attributeKey);
    }
}