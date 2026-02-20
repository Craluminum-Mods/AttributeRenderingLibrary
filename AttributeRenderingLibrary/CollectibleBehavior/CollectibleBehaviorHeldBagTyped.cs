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
    public Dictionary<string, JsonObject> StorageTagsByType { get; protected set; }

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);

        QuantitySlotsByType = properties["quantitySlots"].AsObject<Dictionary<string, int>>();
        SlotBgColorByType = properties["slotBgColor"].AsObject<Dictionary<string, string>>();
        StorageFlagsByType = properties["storageFlags"].AsObject<Dictionary<string, int>>()?.ToDictionary(x => x.Key, x => (EnumItemStorageFlags)x.Value);
        StorageTagsByType = properties["tags"].AsObject<Dictionary<string, JsonObject>>();
    }

    public override TagSet GetStorageTags(ItemStack bagstack)
    {
        if (StorageTagsByType == null || StorageTagsByType.Count == 0)
        {
            return base.GetStorageTags(bagstack);
        }

        Variants variants = Variants.FromStack(bagstack);
        if (variants.FindByVariant(StorageTagsByType, out JsonObject storageTags) && storageTags != null)
        {
            return CollectibleTagSetConverter.ProxyInstance.ReadJson(storageTags.Token);
        }
        return base.GetStorageTags(bagstack);
    }

    public override int GetQuantitySlots(ItemStack bagstack)
    {
        if (QuantitySlotsByType == null || QuantitySlotsByType.Count == 0)
        {
            return base.GetQuantitySlots(bagstack);
        }

        Variants variants = Variants.FromStack(bagstack);
        bool found = variants.FindByVariant(QuantitySlotsByType, out int quantitySlots);

        if (!found)
        {
            return base.GetQuantitySlots(bagstack);
        }
        return quantitySlots;
    }

    public override string GetSlotBgColor(ItemStack bagstack)
    {
        if (SlotBgColorByType == null || SlotBgColorByType.Count == 0)
        {
            return base.GetSlotBgColor(bagstack);
        }

        Variants variants = Variants.FromStack(bagstack);
        bool found = variants.FindByVariant(SlotBgColorByType, out string slotBgColor);

        if (!found)
        {
            return base.GetSlotBgColor(bagstack);
        }

        slotBgColor = variants.ReplacePlaceholders(slotBgColor);
        return slotBgColor;
    }

    public override EnumItemStorageFlags GetStorageFlags(ItemStack bagstack)
    {
        if (StorageFlagsByType == null || StorageFlagsByType.Count == 0)
        {
            return base.GetStorageFlags(bagstack);
        }

        Variants variants = Variants.FromStack(bagstack);
        bool found = variants.FindByVariant(StorageFlagsByType, out EnumItemStorageFlags storageFlags);

        if (!found)
        {
            return base.GetStorageFlags(bagstack);
        }
        return storageFlags;
    } 
}
