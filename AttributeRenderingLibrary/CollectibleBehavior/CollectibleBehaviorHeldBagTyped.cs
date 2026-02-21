using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace AttributeRenderingLibrary;

public class CollectibleBehaviorHeldBagTyped(CollectibleObject collObj) : CollectibleBehaviorHeldBag(collObj)
{
    public Dictionary<string, int> QuantitySlotsByType { get; protected set; }
    public Dictionary<string, string> SlotBgColorByType { get; protected set; }
    public Dictionary<string, EnumItemStorageFlags> StorageFlagsByType { get; protected set; }
    public Dictionary<string, TagSet> StorageTagsByType { get; protected set; }

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);

        LoadTags(properties);

        QuantitySlotsByType = properties["quantitySlots"].AsObject<Dictionary<string, int>>();
        SlotBgColorByType = properties["slotBgColor"].AsObject<Dictionary<string, string>>();
        StorageFlagsByType = properties["storageFlags"].AsObject<Dictionary<string, int>>()?.ToDictionary(x => x.Key, x => (EnumItemStorageFlags)x.Value);
    }

    public virtual void LoadTags(JsonObject properties)
    {
        var unresolvedTags = properties["tags"].AsObject<Dictionary<string, List<string>>>();
        if (unresolvedTags == null) return;

        StorageTagsByType = [];

        foreach ((string type, List<string> tags) in unresolvedTags)
        {
            TagRegistryError tagRegistryError = Core.Api.CollectibleTagRegistry.TryCreateTagSetAndLogIssues(out TagSet resolvedTags, tags);
            StorageTagsByType.Add(type, resolvedTags);
        }
    }

    public override TagSet GetStorageTags(ItemStack bagstack)
    {
        if (!bagstack.FindByVariant(StorageTagsByType, out TagSet storageTags) || storageTags.IsEmpty)
        {
            return base.GetStorageTags(bagstack);
        }
        return storageTags;
    }

    public override int GetQuantitySlots(ItemStack bagstack)
    {
        if (!bagstack.FindByVariant(QuantitySlotsByType, out int quantitySlots))
        {
            return base.GetQuantitySlots(bagstack);
        }
        return quantitySlots;
    }

    public override string GetSlotBgColor(ItemStack bagstack)
    {
        if (!bagstack.FindByVariant(SlotBgColorByType, out string slotBgColor, out Variants variants))
        {
            return base.GetSlotBgColor(bagstack);
        }
        return variants.ReplacePlaceholders(slotBgColor);
    }

    public override EnumItemStorageFlags GetStorageFlags(ItemStack bagstack)
    {
        if (!bagstack.FindByVariant(StorageFlagsByType, out EnumItemStorageFlags storageFlags))
        {
            return base.GetStorageFlags(bagstack);
        }
        return storageFlags;
    } 
}
