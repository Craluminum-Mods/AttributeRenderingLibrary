using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace AttributeRenderingLibrary;

public class CollectibleBehaviorWearableAttachment(CollectibleObject collObj) : CollectibleBehaviorShapeTexturesFromAttributes(collObj)
{
    public Dictionary<string, bool> IsFoldableByType { get; protected set; }
    public Dictionary<string, bool> VisibleDamageEffectByType { get; protected set; }

    public override void OnUnloaded(ICoreAPI api)
    {
        base.OnUnloaded(api);

        Dictionary<string, MultiTextureMeshRef> meshRefs = ObjectCacheUtil.TryGet<Dictionary<string, MultiTextureMeshRef>>(api, "AttributeRenderingLibrary_wearableAttachmentMeshRefs");
        meshRefs?.Foreach(meshRef => meshRef.Value?.Dispose());
        ObjectCacheUtil.Delete(api, "AttributeRenderingLibrary_wearableAttachmentMeshRefs");
    }

    public override void LoadTypes(JsonObject properties)
    {
        base.LoadTypes(properties);

        if (properties == null) return;

        IsFoldableByType = properties["isFoldable"].AsObject<Dictionary<string, bool>>();
        VisibleDamageEffectByType = properties["visibleDamageEffect"].AsObject<Dictionary<string, bool>>();
    }

    public override MeshData GenMesh(ItemSlot slot, ITextureAtlasAPI targetAtlas, BlockPos forBlockPos)
    {
        string slotType = (slot as ItemSlotDisplay)?.DisplayCategory ?? "default";
        return slotType == "shelf" && IsFoldable(slot.Itemstack)
            ? genFoldedMesh(slot, targetAtlas, forBlockPos)
            : genFullBodyMesh(slot, targetAtlas);
    }

    public virtual bool IsFoldable(ItemStack itemstack)
    {
        return itemstack.FindByVariant(IsFoldableByType, out bool isFoldable) && isFoldable;
    }

    public virtual MeshData genFoldedMesh(ItemSlot slot, ITextureAtlasAPI targetAtlas, BlockPos forBlockPos)
    {
        MeshData mesh = RenderExtensions.GenEmptyMesh();

        DisplayableAttributes displayableProps = slot.Itemstack.Collectible.GetCollectibleInterface<IDisplayableProps>()?.GetDisplayableProps(slot, "shelf");
        displayableProps ??= slot.Itemstack.ItemAttributes["displayable"]["shelf"].AsObject<DisplayableAttributes>();

        return displayableProps == null ? mesh : GetOrCreateMesh(slot, targetAtlas, overrideShape: displayableProps.Shape);
    }

    public override string GetMeshCacheKey(ItemSlot slot)
    {
        string slotType = (slot as ItemSlotDisplay)?.DisplayCategory ?? "default";
        return GetMeshCacheKey(slot, slotType);
    }

    public virtual string GetMeshCacheKey(ItemSlot slot, string slotType)
    {
        return slotType + "-ARL_wearableAttachmentModelRef-" + base.GetMeshCacheKey(slot);
    }

    public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
    {
        Dictionary<string, MultiTextureMeshRef> meshrefs = ObjectCacheUtil.GetOrCreate(capi, "AttributeRenderingLibrary_wearableAttachmentMeshRefs", () => new Dictionary<string, MultiTextureMeshRef>());
        string key = GetMeshCacheKey(renderinfo.InSlot, "default");

        if (!meshrefs.TryGetValue(key, out renderinfo.ModelRef))
        {
            MeshData mesh = genFullBodyMesh(renderinfo.InSlot, capi.ItemTextureAtlas);
            renderinfo.ModelRef = meshrefs[key] = mesh == null ? renderinfo.ModelRef : capi.Render.UploadMultiTextureMesh(mesh);
        }

        renderinfo.NormalShaded = true;

        if ((itemstack.FindByVariant(VisibleDamageEffectByType, out bool visibleDamageEffect) && visibleDamageEffect) || (collObj.Attributes is { Count: > 0 } && collObj.Attributes.IsTrue("visibleDamageEffect")))
        {
            renderinfo.DamageEffect = Math.Max(0, 1 - ((float)collObj.GetRemainingDurability(itemstack) / collObj.GetMaxDurability(itemstack) * 1.1f));
        }
    }

    public virtual MeshData genFullBodyMesh(ItemSlot slot, ITextureAtlasAPI targetAtlas)
    {
        MeshData mesh = RenderExtensions.GenEmptyMesh();

        EntityProperties props = clientApi.World.GetEntityType(new AssetLocation("player"));
        Shape entityShape = props.Client.LoadedShape.Clone();
        AssetLocation shapePathForLogging = props.Client.Shape.Base;
        Shape newShape;

        if (AttachedShapeBySlotCodeByType is not { Count: > 0 })
        {
            // No need to step parent anything if its just a texture on the seraph
            newShape = entityShape;
        }
        else
        {
            newShape = new Shape()
            {
                Elements = entityShape.CloneElements(),
                Animations = entityShape.CloneAnimations(),
                AnimationsByCrc32 = entityShape.AnimationsByCrc32,
                JointsById = entityShape.JointsById,
                TextureWidth = entityShape.TextureWidth,
                TextureHeight = entityShape.TextureHeight,
                Textures = null,
            };
        }

        Variants variants = Variants.FromStack(slot.Itemstack);

        Shape? shape = GetShape(slot, variants, overrideShape: null, out CompositeShape? rcshape);

        if (shape == null || rcshape == null) return RenderExtensions.GetUnknownBlockModelData(clientApi);

        newShape.StepParentShape(shape, rcshape.Base.ToShortString(), shapePathForLogging.ToShortString(), clientApi.Logger, (key, code) => { });

        if (rcshape.Overlays is { Length: > 0 })
        {
            foreach (CompositeShape overlay in rcshape.Overlays)
            {
                Shape oshape = Shape.TryGet(clientApi, overlay.Base.CopyWithPathPrefixAndAppendixOnce("shapes/", ".json"));
                if (oshape == null)
                {
                    LoggerUtil.Warn(clientApi, this, $"Wearable shape {rcshape.Base} overlay {overlay.Base} defined in {slot.Itemstack.Class} {slot.Itemstack.Collectible.Code} not found or errored, was supposed to be at {rcshape.Base}. Item will be invisible.");
                    continue;
                }

                newShape.StepParentShape(oshape, overlay.Base.ToShortString(), shapePathForLogging.ToShortString(), clientApi.Logger, (key, Code) => { });
            }
        }

        UniversalShapeTextureSource stexSource = new(clientApi, targetAtlas, shape, rcshape.Base.ToString());
        Dictionary<string, AssetLocation> prefixedTextureCodes = null;
        string overlayPrefix = "";

        if (rcshape.Overlays is { Length: > 0 })
        {
            overlayPrefix = GetMeshCacheKey(slot);
            prefixedTextureCodes = ShapeOverlayHelper.AddOverlays(clientApi, overlayPrefix, variants, stexSource, shape, rcshape);
        }

        foreach ((string textureCode, CompositeTexture texture) in slot.Itemstack.Item.Textures)
        {
            stexSource.textures[textureCode] = texture;
        }

        ShapeOverlayHelper.BakeVariantTextures(clientApi, stexSource, variants, texturesByType, prefixedTextureCodes, overlayPrefix);

        clientApi.Tesselator.TesselateShapeWithJointIds("entity", newShape, out mesh, stexSource, new Vec3f(), quantityElements: rcshape.QuantityElements, selectiveElements: GetShapeSelectiveElements(slot.Itemstack, rcshape));

        return mesh;
    }
}
