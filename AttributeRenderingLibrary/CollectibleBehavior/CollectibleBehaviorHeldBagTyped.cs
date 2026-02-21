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
        if (!bagstack.FindByVariant(StorageTagsByType, out JsonObject storageTags) || storageTags == null)
        {
            return base.GetStorageTags(bagstack);
        }
        return CollectibleTagSetConverter.ProxyInstance.ReadJson(storageTags.Token);
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
