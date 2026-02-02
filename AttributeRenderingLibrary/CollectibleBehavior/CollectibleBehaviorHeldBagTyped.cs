using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace AttributeRenderingLibrary;

public class CollectibleBehaviorHeldBagTyped(CollectibleObject collObj) : CollectibleBehaviorHeldBag(collObj)
{
    public VariantSelectionList<int> QuantitySlotsByType { get; protected set; }
    public VariantSelectionList<string> SlotBgColorByType { get; protected set; }
    public VariantSelectionList<EnumItemStorageFlags> StorageFlagsByType { get; protected set; }

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);

        QuantitySlotsByType = VariantSelectionList<int>.LoadFrom(properties["quantitySlots"]);
        SlotBgColorByType = VariantSelectionList<string>.LoadFrom(properties["slotBgColor"]);
        StorageFlagsByType = VariantSelectionList<EnumItemStorageFlags>.LoadFrom(properties["storageFlags"].Token as JObject, token => (EnumItemStorageFlags)token.Value<int>());
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
