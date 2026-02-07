using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace AttributeRenderingLibrary;

public class CollectibleBehaviorShapeTexturesFromAttributes(CollectibleObject collObj) : CollectibleBehavior(collObj), IShapeTexturesFromAttributes, IContainedMeshSource, IContainedCustomName, IAttachableToEntity
{
    public VariantSelectionList<CompositeShape> shapeByType { get; protected set; }
    public VariantSelectionList<Dictionary<string, CompositeTexture>> texturesByType { get; protected set; }

    public VariantSelectionList<List<object>> NameByType { get; protected set; }
    public VariantSelectionList<List<object>> DescriptionByType { get; protected set; }
    public VariantSelectionList<List<object>> ContainedDescriptionByType { get; protected set; }
    public VariantSelectionList<EnumItemStorageFlags> StorageFlagsByType { get; protected set; }
    #region Animations
    public VariantSelectionList<string> HeldLeftReadyAnimationByType { get; protected set; }
    public VariantSelectionList<string> HeldRightReadyAnimationByType { get; protected set; }

    public VariantSelectionList<string> HeldLeftTpIdleAnimationByType { get; protected set; }
    public VariantSelectionList<string> HeldRightTpIdleAnimationByType { get; protected set; }

    public VariantSelectionList<string> HeldTpUseAnimationByType { get; protected set; }
    public VariantSelectionList<string> HeldTpHitAnimationByType { get; protected set; }
    #endregion
    #region Extra shape overrides
    public VariantSelectionList<string[]> ShapeIgnoreElementsByType { get; protected set; }
    public VariantSelectionList<string[]> ShapeIgnoreElementsCombineByType { get; protected set; }
    public VariantSelectionList<string[]> ShapeSelectiveElementsByType { get; protected set; }
    public VariantSelectionList<string[]> ShapeSelectiveElementsCombineByType { get; protected set; }
    #endregion
    #region IAttachableToEntity
    public VariantSelectionList<OrderedDictionary<string, CompositeShape>> AttachedShapeBySlotCodeByType { get; protected set; }
    public VariantSelectionList<string> CategoryCodeByType { get; protected set; }
    public VariantSelectionList<string[]> DisableElementsByType { get; protected set; }
    public VariantSelectionList<string[]> KeepElementsByType { get; protected set; }
    private IAttachableToEntity iattr;
    #endregion

    private ICoreClientAPI clientApi;

    public override void OnLoaded(ICoreAPI api)
    {
        clientApi = api as ICoreClientAPI;
        iattr = IAttachableToEntity.FromAttributes(collObj);
    }

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);
        
        if (properties is not { Count: > 0 })
            return;

        shapeByType = VariantSelectionList<CompositeShape>.LoadFrom(properties["shape"]);
        texturesByType = VariantSelectionList<Dictionary<string, CompositeTexture>>.LoadFrom(properties["textures"]);

        ShapeIgnoreElementsByType = VariantSelectionList<string[]>.LoadFrom(properties["shapeIgnoreElements"]);
        ShapeIgnoreElementsCombineByType = VariantSelectionList<string[]>.LoadFrom(properties["shapeIgnoreElementsCombine"]);
        ShapeSelectiveElementsByType = VariantSelectionList<string[]>.LoadFrom(properties["shapeSelectiveElements"]);
        ShapeSelectiveElementsCombineByType = VariantSelectionList<string[]>.LoadFrom(properties["shapeSelectiveElementsCombine"]);

        NameByType = VariantSelectionList<List<object>>.LoadFrom(properties["name"]);
        DescriptionByType = VariantSelectionList<List<object>>.LoadFrom(properties["description"]);
        ContainedDescriptionByType = VariantSelectionList<List<object>>.LoadFrom(properties["containedDescription"]);
        StorageFlagsByType = VariantSelectionList<EnumItemStorageFlags>.LoadFrom(properties["storageFlags"]);

        HeldLeftReadyAnimationByType = VariantSelectionList<string>.LoadFrom(properties["heldLeftReadyAnimation"]);
        HeldRightReadyAnimationByType = VariantSelectionList<string>.LoadFrom(properties["heldRightReadyAnimation"]);

        HeldLeftTpIdleAnimationByType = VariantSelectionList<string>.LoadFrom(properties["heldLeftTpIdleAnimation"]);
        HeldRightTpIdleAnimationByType = VariantSelectionList<string>.LoadFrom(properties["heldRightTpIdleAnimation"]);

        HeldTpUseAnimationByType = VariantSelectionList<string>.LoadFrom(properties["heldTpUseAnimation"]);
        HeldTpHitAnimationByType = VariantSelectionList<string>.LoadFrom(properties["heldTpHitAnimation"]);

        AttachedShapeBySlotCodeByType = VariantSelectionList<OrderedDictionary<string, CompositeShape>>.LoadFrom(properties["STFA_attachableToEntity"]?["attachedShapeBySlotCode"]);
        CategoryCodeByType = VariantSelectionList<string>.LoadFrom(properties["STFA_attachableToEntity"]?["categoryCode"]);
        DisableElementsByType = VariantSelectionList<string[]>.LoadFrom(properties["STFA_attachableToEntity"]?["disableElements"]);
        KeepElementsByType = VariantSelectionList<string[]>.LoadFrom(properties["STFA_attachableToEntity"]?["keepElements"]);
    }

    public override void OnUnloaded(ICoreAPI api)
    {
        Dictionary<string, MultiTextureMeshRef> meshRefs = ObjectCacheUtil.TryGet<Dictionary<string, MultiTextureMeshRef>>(api, "AttributeRenderingLibrary_BehaviorShapeTexturesFromAttributes_MeshRefs");
        meshRefs?.Foreach(meshRef => meshRef.Value?.Dispose());
        ObjectCacheUtil.Delete(api, "AttributeRenderingLibrary_BehaviorShapeTexturesFromAttributes_MeshRefs");
    }

    public virtual MeshData GetOrCreateMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas)
    {
        MeshData mesh = RenderExtensions.GenEmptyMesh();

        Variants variants = Variants.FromStack(itemstack);
        variants.FindByVariant(shapeByType, out CompositeShape ucshape);
        ucshape ??= itemstack.Item.Shape;

        if (ucshape == null) return mesh;

        CompositeShape rcshape = variants.ReplacePlaceholders(ucshape.Clone());
        rcshape.Base = rcshape.Base.CopyWithPathPrefixAndAppendixOnce("shapes/", ".json");

        Shape shape = clientApi.Assets.TryGet(rcshape.Base)?.ToObject<Shape>();
        if (shape == null) return mesh;

        UniversalShapeTextureSource stexSource = new UniversalShapeTextureSource(clientApi, targetAtlas, shape, rcshape.Base.ToString());
        Dictionary<string, AssetLocation> prefixedTextureCodes = null;
        string overlayPrefix = "";

        if (rcshape.Overlays != null && rcshape.Overlays.Length > 0)
        {
            overlayPrefix = GetMeshCacheKey(itemstack);
            prefixedTextureCodes = ShapeOverlayHelper.AddOverlays(clientApi, overlayPrefix, variants, stexSource, shape, rcshape);
        }

        foreach ((string textureCode, CompositeTexture texture) in itemstack.Item.Textures)
        {
            stexSource.textures[textureCode] = texture;
        }

        ShapeOverlayHelper.BakeVariantTextures(clientApi, stexSource, variants, texturesByType, prefixedTextureCodes, overlayPrefix);

        TesselationMetaData meta = new TesselationMetaData
        {
            QuantityElements = rcshape.QuantityElements,
            SelectiveElements = GetShapeSelectiveElements(itemstack, rcshape),
            IgnoreElements = GetShapeIgnoreElements(itemstack, rcshape),
            TexSource = stexSource,
            TypeForLogging = "ShapeTexturesFromAttributes item behavior"
        };

        clientApi.Tesselator.TesselateShape(meta, shape, out mesh);
        return mesh;
    }

    /// <summary>
    /// Temporary solution for wearable attachments until 1.22 is out with proper wearable support
    /// </summary>
    public virtual MeshData GenWearableMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas)
    {
        MeshData mesh = RenderExtensions.GenEmptyMesh();

        EntityProperties props = clientApi.World.GetEntityType(new AssetLocation("player"));
        Shape entityShape = props.Client.LoadedShape.Clone();
        AssetLocation shapePathForLogging = props.Client.Shape.Base;
        Shape newShape;

        if (AttachedShapeBySlotCodeByType == null || AttachedShapeBySlotCodeByType.Count <= 0)
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

        Variants variants = Variants.FromStack(itemstack);
        variants.FindByVariant(shapeByType, out CompositeShape ucshape);
        ucshape ??= itemstack.Item.Shape;


        if (ucshape == null) return mesh;

        CompositeShape rcshape = variants.ReplacePlaceholders(ucshape.Clone());
        rcshape.Base = rcshape.Base.CopyWithPathPrefixAndAppendixOnce("shapes/", ".json");

        Shape shape = clientApi.Assets.TryGet(rcshape.Base)?.ToObject<Shape>();
        if (shape == null) return mesh;

        newShape.StepParentShape(shape, rcshape.Base.ToShortString(), shapePathForLogging.ToShortString(), clientApi.Logger, (key, code) => { });

        if (rcshape.Overlays != null)
        {
            foreach (var overlay in rcshape.Overlays)
            {
                Shape oshape = Shape.TryGet(clientApi, overlay.Base.CopyWithPathPrefixAndAppendixOnce("shapes/", ".json"));
                if (oshape == null)
                {
                    clientApi.World.Logger.Warning("[Attribute Rendering Library] Wearable shape {0} overlay {4} defined in {1} {2} not found or errored, was supposed to be at {3}. Item will be invisible.", rcshape.Base, itemstack.Class, itemstack.Collectible.Code, rcshape.Base, overlay.Base);
                    continue;
                }

                newShape.StepParentShape(oshape, overlay.Base.ToShortString(), shapePathForLogging.ToShortString(), clientApi.Logger, (key, Code) => { });
            }
        }

        UniversalShapeTextureSource stexSource = new UniversalShapeTextureSource(clientApi, targetAtlas, shape, rcshape.Base.ToString());
        Dictionary<string, AssetLocation> prefixedTextureCodes = null;
        string overlayPrefix = "";

        if (rcshape.Overlays != null && rcshape.Overlays.Length > 0)
        {
            overlayPrefix = GetMeshCacheKey(itemstack);
            prefixedTextureCodes = ShapeOverlayHelper.AddOverlays(clientApi, overlayPrefix, variants, stexSource, shape, rcshape);
        }

        foreach ((string textureCode, CompositeTexture texture) in itemstack.Item.Textures)
        {
            stexSource.textures[textureCode] = texture;
        }

        ShapeOverlayHelper.BakeVariantTextures(clientApi, stexSource, variants, texturesByType, prefixedTextureCodes, overlayPrefix);

        clientApi.Tesselator.TesselateShapeWithJointIds("entity", newShape, out mesh, stexSource, new Vec3f(), quantityElements: rcshape.QuantityElements, selectiveElements: GetShapeSelectiveElements(itemstack, rcshape));

        return mesh;
    }

    public virtual string[] GetShapeIgnoreElements(ItemStack itemStack, CompositeShape cshape)
    {
        if (ShapeIgnoreElementsByType is { Count: > 0 })
        {
            Variants variants = Variants.FromStack(itemStack);
            if (variants.FindByVariant(ShapeIgnoreElementsByType, out string[] selectiveElements) && selectiveElements != null)
            {
                return cshape.IgnoreElements.Append(selectiveElements);
            }
        }
        
        if (ShapeIgnoreElementsCombineByType is { Count: > 0 })
        {
            Variants variants = Variants.FromStack(itemStack);
            List<string> result = cshape.IgnoreElements is null ? new() : new(cshape.IgnoreElements);
            foreach (var subset in variants.FindAllByVariant(ShapeIgnoreElementsCombineByType))
            {
                result.AddRange(subset);
            }
            return result.ToArray();
        }
        
        return cshape.IgnoreElements;
    }

    public virtual string[] GetShapeSelectiveElements(ItemStack itemStack, CompositeShape cshape)
    {
        if (ShapeSelectiveElementsByType is { Count: > 0 })
        {
            Variants variants = Variants.FromStack(itemStack);
            if (variants.FindByVariant(ShapeSelectiveElementsByType, out string[] selectiveElements) && selectiveElements != null)
            {
                return cshape.SelectiveElements.Append(selectiveElements);
            }
        }
        
        if (ShapeSelectiveElementsCombineByType is { Count: > 0 })
        {
            Variants variants = Variants.FromStack(itemStack);
            List<string> result = cshape.SelectiveElements is null ? new() : new(cshape.SelectiveElements);
            foreach (var subset in variants.FindAllByVariant(ShapeSelectiveElementsCombineByType))
            {
                result.AddRange(subset);
            }
            return result.ToArray();
        }
        
        return cshape.SelectiveElements;
    }

    public override void OnBeforeRender(ICoreClientAPI clientApi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
    {
        Dictionary<string, MultiTextureMeshRef> meshRefs = ObjectCacheUtil.GetOrCreate(clientApi, "AttributeRenderingLibrary_BehaviorShapeTexturesFromAttributes_MeshRefs", () => new Dictionary<string, MultiTextureMeshRef>());

        string key = GetMeshCacheKey(itemstack);

        if (!meshRefs.TryGetValue(key, out MultiTextureMeshRef meshref))
        {
            MeshData mesh = GenMesh(itemstack, clientApi.ItemTextureAtlas, null);
            meshref = clientApi.Render.UploadMultiTextureMesh(mesh);
            meshRefs[key] = meshref;
        }

        renderinfo.ModelRef = meshref;
        renderinfo.NormalShaded = true;

        base.OnBeforeRender(clientApi, itemstack, target, ref renderinfo);
    }

    public override void GetHeldItemName(StringBuilder sb, ItemStack itemStack)
    {
        if (NameByType == null || NameByType.Count == 0)
        {
            return;
        }

        Variants variants = Variants.FromStack(itemStack);
        variants.FindByVariant(NameByType, out List<object> _langKeys);

        string name = variants.GetName(_langKeys);
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        sb.Clear();
        sb.Append(name);
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        if (DescriptionByType == null || DescriptionByType.Count == 0)
        {
            return;
        }

        Variants variants = Variants.FromStack(inSlot.Itemstack);
        variants.FindByVariant(DescriptionByType, out List<object> _langKeys);
        variants.GetDescription(dsc, _langKeys);
        variants.GetDebugDescription(dsc, withDebugInfo);
    }

    public override EnumItemStorageFlags GetStorageFlags(ItemStack itemstack, ref EnumHandling handling)
    {
        if (StorageFlagsByType == null || StorageFlagsByType.Count == 0)
        {
            return base.GetStorageFlags(itemstack, ref handling);
        }

        Variants variants = Variants.FromStack(itemstack);
        if (!variants.FindByVariant(StorageFlagsByType, out EnumItemStorageFlags storageFlags))
        {
            return base.GetStorageFlags(itemstack, ref handling);
        }
        handling = EnumHandling.PreventSubsequent;
        return storageFlags;
    }

    public override string GetHeldReadyAnimation(ItemSlot activeHotbarSlot, Entity forEntity, EnumHand hand, ref EnumHandling bhHandling)
    {
        VariantSelectionList<string> animCodesByType = (hand == EnumHand.Left) ? HeldLeftReadyAnimationByType : HeldRightReadyAnimationByType;

        if (animCodesByType == null || animCodesByType.Count == 0)
        {
            return base.GetHeldReadyAnimation(activeHotbarSlot, forEntity, hand, ref bhHandling);
        }

        Variants variants = Variants.FromStack(activeHotbarSlot.Itemstack);
        if (!variants.FindByVariant(animCodesByType, out string animCode))
        {
            return base.GetHeldReadyAnimation(activeHotbarSlot, forEntity, hand, ref bhHandling);
        }
        bhHandling = EnumHandling.PreventSubsequent;
        return animCode;
    }

    public override string GetHeldTpIdleAnimation(ItemSlot activeHotbarSlot, Entity forEntity, EnumHand hand, ref EnumHandling bhHandling)
    {
        VariantSelectionList<string> animCodesByType = (hand == EnumHand.Left) ? HeldLeftTpIdleAnimationByType : HeldRightTpIdleAnimationByType;

        if (animCodesByType == null || animCodesByType.Count == 0)
        {
            return base.GetHeldTpIdleAnimation(activeHotbarSlot, forEntity, hand, ref bhHandling);
        }

        Variants variants = Variants.FromStack(activeHotbarSlot.Itemstack);
        if (!variants.FindByVariant(animCodesByType, out string animCode))
        {
            return base.GetHeldTpIdleAnimation(activeHotbarSlot, forEntity, hand, ref bhHandling);
        }
        bhHandling = EnumHandling.PreventSubsequent;
        return animCode;
    }

    public override string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity forEntity, ref EnumHandling bhHandling)
    {
        if (HeldTpUseAnimationByType == null || HeldTpUseAnimationByType.Count == 0)
        {
            return base.GetHeldTpUseAnimation(activeHotbarSlot, forEntity, ref bhHandling);
        }

        Variants variants = Variants.FromStack(activeHotbarSlot.Itemstack);
        if (!variants.FindByVariant(HeldTpUseAnimationByType, out string animCode))
        {
            return base.GetHeldTpUseAnimation(activeHotbarSlot, forEntity, ref bhHandling);
        }
        bhHandling = EnumHandling.PreventSubsequent;
        return animCode;
    }

    public override string GetHeldTpHitAnimation(ItemSlot slot, Entity byEntity, ref EnumHandling bhHandling)
    {
        if (HeldTpHitAnimationByType == null || HeldTpHitAnimationByType.Count == 0)
        {
            return base.GetHeldTpHitAnimation(slot, byEntity, ref bhHandling);
        }

        Variants variants = Variants.FromStack(slot.Itemstack);
        if (!variants.FindByVariant(HeldTpHitAnimationByType, out string animCode))
        {
            return base.GetHeldTpHitAnimation(slot, byEntity, ref bhHandling);
        }
        bhHandling = EnumHandling.PreventSubsequent;
        return animCode;
    }

    public virtual MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos)
    {
        /// Temporary solution for wearable attachments until 1.22 is out with proper wearable support
        if (itemstack.ItemAttributes != null && itemstack.ItemAttributes.IsTrue("wearableAttachment"))
        {
            return GenWearableMesh(itemstack, targetAtlas);
        }
        return GetOrCreateMesh(itemstack, targetAtlas);
    }

    public virtual string GetMeshCacheKey(ItemStack itemstack)
    {
        string key = $"{itemstack.Collectible.Code}-{Variants.FromStack(itemstack)}";

        /// Temporary solution for wearable attachments until 1.22 is out with proper wearable support
        if (itemstack.ItemAttributes != null && itemstack.ItemAttributes.IsTrue("wearableAttachment"))
        {
            return "ARL-wearableAttachmentModelRef-" + key;
        }
        return key;
    }

    public virtual string GetContainedInfo(ItemSlot inSlot)
    {
        if (ContainedDescriptionByType == null || ContainedDescriptionByType.Count == 0)
        {
            return collObj.GetHeldItemName(inSlot.Itemstack);
        }

        StringBuilder dsc = new();
        Variants variants = Variants.FromStack(inSlot.Itemstack);
        variants.FindByVariant(ContainedDescriptionByType, out List<object> _langKeys);

        if (_langKeys == null || _langKeys.Count == 0)
        {
            return collObj.GetHeldItemName(inSlot.Itemstack);
        }

        variants.GetDescription(dsc, _langKeys);
        return dsc.ToString();
    }

    public virtual string GetContainedName(ItemSlot inSlot, int quantity)
    {
        return collObj.GetHeldItemName(inSlot.Itemstack);
    }

    void IAttachableToEntity.CollectTextures(ItemStack stack, Shape shape, string texturePrefixCode, Dictionary<string, CompositeTexture> intoDict)
    {
        foreach ((string textureCode, CompositeTexture texture) in stack.Item.Textures)
        {
            shape.Textures[textureCode] = texture.Baked.BakedName;
        }

        VariantSelectionList<Dictionary<string, CompositeTexture>> texturesByType = null;

        if (stack.Collectible.GetCollectibleInterface<IShapeTexturesFromAttributes>() is IShapeTexturesFromAttributes STFA)
        {
            texturesByType = STFA.texturesByType;
        }

        Variants variants = Variants.FromStack(stack);
        if (variants.FindByVariant(texturesByType, out Dictionary<string, CompositeTexture> _textures))
        {
            foreach ((string textureCode, CompositeTexture texture) in _textures)
            {
                CompositeTexture ctex = texture.Clone();
                ctex = variants.ReplacePlaceholders(ctex);
                if (!clientApi.Assets.Exists(ctex.Base.CopyWithPathPrefixAndAppendixOnce("textures/", ".png")))
                {
                    ctex.Base.Path = "unknown";
                    ctex.Base.Domain = "game";
                }
                ctex.Bake(clientApi.Assets);
                intoDict[textureCode] = ctex;
                shape.Textures[textureCode] = ctex.Baked.BakedName;
            }
        }
    }

    CompositeShape IAttachableToEntity.GetAttachedShape(ItemStack stack, string slotCode)
    {
        if (AttachedShapeBySlotCodeByType == null || AttachedShapeBySlotCodeByType.Count == 0)
        {
            return iattr?.GetAttachedShape(stack, slotCode);
        }

        Variants variants = Variants.FromStack(stack);
        if (!variants.FindByVariant(AttachedShapeBySlotCodeByType, out OrderedDictionary<string, CompositeShape> attachedShapeBySlotCode))
        {
            return iattr?.GetAttachedShape(stack, slotCode);
        }

        if (attachedShapeBySlotCode != null && attachedShapeBySlotCode.Count != 0)
        {
            foreach ((string _slotCode, CompositeShape ucshape) in attachedShapeBySlotCode)
            {
                if (WildcardUtil.Match(_slotCode, slotCode))
                {
                    CompositeShape rcshape = variants.ReplacePlaceholders(ucshape.Clone());
                    if (rcshape.Overlays == null || rcshape.Overlays.Length == 0)
                    {
                        return rcshape;
                    }

                    List<CompositeShape> overlays = [];
                    foreach (CompositeShape overlay in rcshape.Overlays)
                    {
                        if (clientApi.Assets.Exists(overlay.Base.Clone().CopyWithPathPrefixAndAppendixOnce("shapes/", ".json")))
                        {
                            overlays.Add(overlay);
                        }
                    }
                    rcshape.Overlays = overlays.ToArray();
                    return rcshape;
                }
            }
        }

        return iattr?.GetAttachedShape(stack, slotCode);
    }

    string IAttachableToEntity.GetCategoryCode(ItemStack stack)
    {
        if (CategoryCodeByType == null || CategoryCodeByType.Count == 0)
        {
            return iattr?.GetCategoryCode(stack);
        }

        Variants variants = Variants.FromStack(stack);
        variants.FindByVariant(CategoryCodeByType, out string categoryCode);
        return categoryCode;
    }

    string[] IAttachableToEntity.GetDisableElements(ItemStack stack)
    {
        if (DisableElementsByType == null || DisableElementsByType.Count == 0)
        {
            return iattr?.GetDisableElements(stack);
        }

        Variants variants = Variants.FromStack(stack);
        variants.FindByVariant(DisableElementsByType, out string[] disableElements);
        return disableElements;
    }

    string[] IAttachableToEntity.GetKeepElements(ItemStack stack)
    {
        if (KeepElementsByType == null || KeepElementsByType.Count == 0)
        {
            return iattr?.GetKeepElements(stack);
        }

        Variants variants = Variants.FromStack(stack);
        variants.FindByVariant(KeepElementsByType, out string[] keepElements);
        return keepElements;
    }

    string IAttachableToEntity.GetTexturePrefixCode(ItemStack stack) => GetMeshCacheKey(stack);

    bool IAttachableToEntity.IsAttachable(Entity toEntity, ItemStack itemStack) => true;

    int IAttachableToEntity.RequiresBehindSlots { get; set; }
}
