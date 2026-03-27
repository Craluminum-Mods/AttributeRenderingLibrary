using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace AttributeRenderingLibrary;

/// <summary>
/// Provides custom transformation for OnBeforeRender and collectibles stored inside a BlockEntityDisplay, 
/// specifically for collectibles with Variants.
/// </summary>
public class CollectibleBehaviorContainedTransform(CollectibleObject collObj) : CollectibleBehavior(collObj), IContainedTransform
{
    public Transforms? BasicTransforms { get; protected set; }
    public Dictionary<string, Dictionary<string, ModelTransform>>? ExtraTransforms { get; protected set; }

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);
        BasicTransforms = properties["transforms"].AsObject<Transforms>();
        ExtraTransforms = properties["extraTransforms"].AsObject<Dictionary<string, Dictionary<string, ModelTransform>>>()?.ToDictionary(x => x.Key.ToLowerInvariant(), x => x.Value);
    }

    public override void OnBeforeRender(ICoreClientAPI capi, ItemStack stack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
    {
        ApplyOnBeforeRenderTransform(target, stack, ref renderinfo.Transform);
    }

    public void ApplyOnBeforeRenderTransform(EnumItemRenderTarget target, ItemStack stack, ref ModelTransform transform)
    {
        Dictionary<string, ModelTransform>? transformsByType = target switch
        {
            EnumItemRenderTarget.Gui => BasicTransforms?.GuiTransform,
            EnumItemRenderTarget.HandTp => BasicTransforms?.TpHandTransform,
            EnumItemRenderTarget.HandTpOff => BasicTransforms?.TpOffHandTransform,
            EnumItemRenderTarget.Ground => BasicTransforms?.GroundTransform,
            _ => null,
        };

        ModelTransform? newTransform = stack.GetByVariant(transformsByType);
        if (newTransform != null)
        {
            transform = newTransform.EnsureDefaultValues();
        }
    }

    ModelTransform? IContainedTransform.GetTransform(BlockEntityDisplay be, string attributeTransformCode, ItemStack stack)
    {
        attributeTransformCode = attributeTransformCode.ToLowerInvariant();

        if (ExtraTransforms?.TryGetValue(attributeTransformCode, out Dictionary<string, ModelTransform>? transformsByType) == true
            && stack.FindByVariant(transformsByType, out ModelTransform transform) && transform != null)
        {
            return transform.EnsureDefaultValues();
        }
        return null;
    }
}