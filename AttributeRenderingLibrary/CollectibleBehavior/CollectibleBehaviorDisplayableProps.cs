using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace AttributeRenderingLibrary;

public class CollectibleBehaviorDisplayableProps(CollectibleObject collObj) : CollectibleBehavior(collObj), IDisplayableProps
{
    public Dictionary<string, Dictionary<string, DisplayableAttributes>>? DisplayablePropsByCodeByType { get; protected set; }

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);

        if (properties is not { Count: > 0 }) return;

        DisplayablePropsByCodeByType = properties["displayable"].AsObject<Dictionary<string, Dictionary<string, DisplayableAttributes>>>(null, collObj.Code.Domain);
    }

    public virtual DisplayableAttributes? GetDisplayableProps(ItemSlot inSlot, string displayType)
    {
        if (!inSlot.Itemstack!.FindByVariant(DisplayablePropsByCodeByType!, out Dictionary<string, DisplayableAttributes> propsByCode, out Variants variants) || propsByCode is not { Count: > 0})
        {
            return null;
        }
        if (propsByCode.TryGetValue(displayType, out DisplayableAttributes? displayProps))
        {
            return new DisplayableAttributes
            {
                Behavior = displayProps.Behavior,
                Size = displayProps.Size,
                Shape = variants.ReplacePlaceholders(displayProps.Shape?.Clone()),
                Transform = displayProps.Transform,
                RandYRotAngle = displayProps.RandYRotAngle,
                Category = variants.ReplacePlaceholders(displayProps.Category),
                PileableSelectiveElements = displayProps.PileableSelectiveElements is not null
                    ? variants.ReplacePlaceholders(displayProps.PileableSelectiveElements)
                    : null
            };
        }
        return null;
    }
}

