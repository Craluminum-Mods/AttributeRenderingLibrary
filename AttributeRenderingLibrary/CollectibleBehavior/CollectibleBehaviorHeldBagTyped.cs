using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace AttributeRenderingLibrary;

public class CollectibleBehaviorHeldBagTyped : CollectibleBehaviorHeldBag
{
    public Dictionary<string, int> quantitySlotsByType = new();
    public Dictionary<string, string> slotBgColorByType = new();
    public Dictionary<string, EnumItemStorageFlags> storageFlagsByType = new();

    public CollectibleBehaviorHeldBagTyped(CollectibleObject collObj) : base(collObj) { }

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);

        quantitySlotsByType = properties["quantitySlots"].AsObject(defaultValue: new Dictionary<string, int>());
        slotBgColorByType = properties["slotBgColor"].AsObject(defaultValue: new Dictionary<string, string>());
        storageFlagsByType = properties["storageFlags"].AsObject(defaultValue: new Dictionary<string, int>()).ToDictionary(x => x.Key, x => (EnumItemStorageFlags)x.Value);
    }

    public override int GetQuantitySlots(ItemStack bagstack)
    {
        Variants variants = Variants.FromStack(bagstack);
        bool found = variants.FindByVariant(quantitySlotsByType, out int quantitySlots);

        if (!found)
        {
            return base.GetQuantitySlots(bagstack);
        }
        return quantitySlots;
    }

    public override string GetSlotBgColor(ItemStack bagstack)
    {
        Variants variants = Variants.FromStack(bagstack);
        bool found = variants.FindByVariant(slotBgColorByType, out string slotBgColor);

        if (!found)
        {
            return base.GetSlotBgColor(bagstack);
        }

        slotBgColor = variants.ReplacePlaceholders(slotBgColor);
        return slotBgColor;
    }

    public override EnumItemStorageFlags GetStorageFlags(ItemStack bagstack)
    {
        Variants variants = Variants.FromStack(bagstack);
        bool found = variants.FindByVariant(storageFlagsByType, out EnumItemStorageFlags storageFlags);

        if (!found)
        {
            return base.GetStorageFlags(bagstack);
        }
        return storageFlags;
    }
}
