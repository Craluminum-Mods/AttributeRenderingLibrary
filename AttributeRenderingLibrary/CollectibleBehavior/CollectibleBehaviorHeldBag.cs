using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace AttributeRenderingLibrary;

public class CollectibleBehaviorHeldBag(CollectibleObject collObj) : Vintagestory.GameContent.CollectibleBehaviorHeldBag(collObj)
{
    public Dictionary<string, int> QuantitySlotsByType { get; protected set; }
    public Dictionary<string, string> SlotBgColorByType { get; protected set; }
    public Dictionary<string, EnumItemStorageFlags> StorageFlagsByType { get; protected set; }
    public Dictionary<string, TagSet> StorageTagsByType { get; protected set; }

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);

        if (properties is not { Count: > 0 }) return;

        LoadTags(properties);

        QuantitySlotsByType = properties["quantitySlots"].AsObject<Dictionary<string, int>>();
        SlotBgColorByType = properties["slotBgColor"].AsObject<Dictionary<string, string>>();
        StorageFlagsByType = properties["storageFlags"].AsObject<Dictionary<string, int>>()?.ToDictionary(x => x.Key, x => (EnumItemStorageFlags)x.Value);
    }

    public virtual void LoadTags(JsonObject properties)
    {
        Dictionary<string, List<string>> unresolvedTags = properties["tags"].AsObject<Dictionary<string, List<string>>>();
        if (unresolvedTags == null) return;

        StorageTagsByType = [];

        foreach ((string type, List<string> tags) in unresolvedTags)
        {
            Core.Api.CollectibleTagRegistry.TryCreateTagSetAndLogIssues(out TagSet resolvedTags, tags);
            StorageTagsByType.Add(type, resolvedTags);
        }
    }

    public override TagSet GetStorageTags(ItemStack bagstack) => !bagstack.FindByVariant(StorageTagsByType, out TagSet storageTags)
            ? base.GetStorageTags(bagstack)
            : storageTags;

    public override int GetQuantitySlots(ItemStack bagstack) => !bagstack.FindByVariant(QuantitySlotsByType, out int quantitySlots)
            ? base.GetQuantitySlots(bagstack)
            : quantitySlots;

    public override string GetSlotBgColor(ItemStack bagstack) => !bagstack.FindByVariant(SlotBgColorByType, out string slotBgColor, out Variants variants)
            ? base.GetSlotBgColor(bagstack)
            : variants.ReplacePlaceholders(slotBgColor);

    public override EnumItemStorageFlags GetStorageFlags(ItemStack bagstack) => !bagstack.FindByVariant(StorageFlagsByType, out EnumItemStorageFlags storageFlags)
            ? base.GetStorageFlags(bagstack)
            : storageFlags;
}
