using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace AttributeRenderingLibrary;

public class CollectibleBehaviorFoodTags(CollectibleObject collObj) : CollectibleBehavior(collObj), ICreatureDietFoodTags
{
    public Dictionary<string, string[]>? FoodTagsByType { get; protected set; }

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);

        if (properties is not { Count: > 0 }) return;

        FoodTagsByType = properties["foodTags"].AsObject<Dictionary<string, string[]>>();
    }

    public virtual string[] GetFoodTags(ItemStack itemstack) => !itemstack.FindByVariant(FoodTagsByType!, out string[] foodTags, out Variants variants)
            ? []
            : variants.ReplacePlaceholders(foodTags) ?? [];
}