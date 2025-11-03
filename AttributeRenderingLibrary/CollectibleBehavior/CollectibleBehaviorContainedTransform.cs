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
    protected Transforms transforms;
    protected Dictionary<string, Dictionary<string, ModelTransform>> extraTransforms;

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);
        transforms = properties["transforms"].AsObject<Transforms>();
        extraTransforms = properties["extraTransforms"].AsObject<Dictionary<string, Dictionary<string, ModelTransform>>>()?.ToDictionary(x => x.Key.ToLowerInvariant(), x => x.Value);
    }

    public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
    {
        ApplyOnBeforeRenderTransform(target, Variants.FromStack(itemstack), ref renderinfo.Transform);
    }

    public void ApplyOnBeforeRenderTransform(EnumItemRenderTarget target, Variants variants, ref ModelTransform transform)
    {
        Dictionary<string, ModelTransform> transformsByType = target switch
        {
            EnumItemRenderTarget.Gui => transforms?.GuiTransform,
            EnumItemRenderTarget.HandTp => transforms?.TpHandTransform,
            EnumItemRenderTarget.HandTpOff => transforms?.TpOffHandTransform,
            EnumItemRenderTarget.Ground => transforms?.GroundTransform,
            _ => null,
        };

        if (transformsByType != null
            && transformsByType.Count > 0
            && variants.FindByVariant(transformsByType, out ModelTransform newTransform) && newTransform != null)
        {
            newTransform = newTransform.EnsureDefaultValues();
            transform = newTransform;
        }
    }

    ModelTransform IContainedTransform.GetTransform(BlockEntityDisplay be, string attributeTransformCode, ItemStack stack)
    {
        attributeTransformCode = attributeTransformCode.ToLowerInvariant();

        if (extraTransforms != null
            && extraTransforms.Count > 0
            && extraTransforms.TryGetValue(attributeTransformCode, out Dictionary<string, ModelTransform> transformsByType)
            && Variants.FromStack(stack).FindByVariant(transformsByType, out ModelTransform transform))
        {
            transform = transform.EnsureDefaultValues();
            return transform;
        }
        return null;
    }
}