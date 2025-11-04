using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace AttributeRenderingLibrary;

public class CollectibleBehaviorShapeTexturesFromAttributes : CollectibleBehavior, IShapeTexturesFromAttributes, IContainedMeshSource, IContainedCustomName, IAttachableToEntity
{

    public Dictionary<string, List<object>> NameByType { get; protected set; } = new();
    public Dictionary<string, List<object>> DescriptionByType { get; protected set; } = new();
    public Dictionary<string, List<object>> ContainedDescriptionByType { get; protected set; } = new();
    public Dictionary<string, EnumItemStorageFlags> StorageFlagsByType { get; protected set; }

    public Dictionary<string, CompositeShape> shapeByType { get; protected set; }
    public Dictionary<string, Dictionary<string, CompositeTexture>> texturesByType { get; protected set; }
    #region Animations
    public Dictionary<string, string> HeldLeftReadyAnimationByType { get; protected set; } = new();
    public Dictionary<string, string> HeldRightReadyAnimationByType { get; protected set; } = new();

    public Dictionary<string, string> HeldLeftTpIdleAnimationByType { get; protected set; } = new();
    public Dictionary<string, string> HeldRightTpIdleAnimationByType { get; protected set; } = new();

    public Dictionary<string, string> HeldTpUseAnimationByType { get; protected set; } = new();
    public Dictionary<string, string> HeldTpHitAnimationByType { get; protected set; } = new();
    #endregion
    #region IAttachableToEntity
    public Dictionary<string, OrderedDictionary<string, CompositeShape>> attachedShapeBySlotCodeByType = new();
    public Dictionary<string, string> categoryCodeByType = new();
    public Dictionary<string, string[]> disableElementsByType = new();
    public Dictionary<string, string[]> keepElementsByType = new();
    private IAttachableToEntity iattr;
    #endregion

    private ICoreClientAPI clientApi;

    public CollectibleBehaviorShapeTexturesFromAttributes(CollectibleObject collObj) : base(collObj) { }

    public override void OnLoaded(ICoreAPI api)
    {
        clientApi = api as ICoreClientAPI;
        iattr = IAttachableToEntity.FromAttributes(collObj);
    }

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);

        if (properties != null)
        {
            shapeByType = properties["shape"].AsObject<Dictionary<string, CompositeShape>>();
            texturesByType = properties["textures"].AsObject<Dictionary<string, Dictionary<string, CompositeTexture>>>();

            NameByType = properties["name"].AsObject<Dictionary<string, List<object>>>();
            DescriptionByType = properties["description"].AsObject<Dictionary<string, List<object>>>();
            ContainedDescriptionByType = properties["containedDescription"].AsObject<Dictionary<string, List<object>>>();
            StorageFlagsByType = properties["storageFlags"].AsObject<Dictionary<string, EnumItemStorageFlags>>();

            HeldLeftReadyAnimationByType = properties["heldLeftReadyAnimation"].AsObject<Dictionary<string, string>>();
            HeldRightReadyAnimationByType = properties["heldRightReadyAnimation"].AsObject<Dictionary<string, string>>();

            HeldLeftTpIdleAnimationByType = properties["heldLeftTpIdleAnimation"].AsObject<Dictionary<string, string>>();
            HeldRightTpIdleAnimationByType = properties["heldRightTpIdleAnimation"].AsObject<Dictionary<string, string>>();

            HeldTpUseAnimationByType = properties["heldTpUseAnimation"].AsObject<Dictionary<string, string>>();
            HeldTpHitAnimationByType = properties["heldTpHitAnimation"].AsObject<Dictionary<string, string>>();

            attachedShapeBySlotCodeByType = properties["STFA_attachableToEntity"]?["attachedShapeBySlotCode"].AsObject<Dictionary<string, OrderedDictionary<string, CompositeShape>>>();
            categoryCodeByType = properties["STFA_attachableToEntity"]?["categoryCode"].AsObject<Dictionary<string, string>>();
            disableElementsByType = properties["STFA_attachableToEntity"]?["disableElements"].AsObject<Dictionary<string, string[]>>();
            keepElementsByType = properties["STFA_attachableToEntity"]?["keepElements"].AsObject<Dictionary<string, string[]>>();
        }
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

        clientApi.Tesselator.TesselateShape("ShapeTexturesFromAttributes behavior", shape, out mesh, stexSource, quantityElements: rcshape.QuantityElements, selectiveElements: rcshape.SelectiveElements);
        return mesh;
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
        Dictionary<string, string> animCodesByType = (hand == EnumHand.Left) ? HeldLeftReadyAnimationByType : HeldRightReadyAnimationByType;

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
        Dictionary<string, string> animCodesByType = (hand == EnumHand.Left) ? HeldLeftTpIdleAnimationByType : HeldRightTpIdleAnimationByType;

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
        return GetOrCreateMesh(itemstack, targetAtlas);
    }

    public virtual string GetMeshCacheKey(ItemStack itemstack)
    {
        return $"{itemstack.Collectible.Code}-{Variants.FromStack(itemstack)}";
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

        Dictionary<string, Dictionary<string, CompositeTexture>> texturesByType = new();

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
        if (attachedShapeBySlotCodeByType == null || attachedShapeBySlotCodeByType.Count == 0)
        {
            return iattr?.GetAttachedShape(stack, slotCode);
        }

        Variants variants = Variants.FromStack(stack);
        if (!variants.FindByVariant(attachedShapeBySlotCodeByType, out OrderedDictionary<string, CompositeShape> attachedShapeBySlotCode))
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

                    List<CompositeShape> overlays = new();
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
        if (categoryCodeByType == null || categoryCodeByType.Count == 0)
        {
            return iattr?.GetCategoryCode(stack);
        }

        Variants variants = Variants.FromStack(stack);
        variants.FindByVariant(categoryCodeByType, out string categoryCode);
        return categoryCode;
    }

    string[] IAttachableToEntity.GetDisableElements(ItemStack stack)
    {
        if (disableElementsByType == null || disableElementsByType.Count == 0)
        {
            return iattr?.GetDisableElements(stack);
        }

        Variants variants = Variants.FromStack(stack);
        variants.FindByVariant(disableElementsByType, out string[] disableElements);
        return disableElements;
    }

    string[] IAttachableToEntity.GetKeepElements(ItemStack stack)
    {
        if (keepElementsByType == null || keepElementsByType.Count == 0)
        {
            return iattr?.GetKeepElements(stack);
        }

        Variants variants = Variants.FromStack(stack);
        variants.FindByVariant(keepElementsByType, out string[] keepElements);
        return keepElements;
    }

    string IAttachableToEntity.GetTexturePrefixCode(ItemStack stack) => GetMeshCacheKey(stack);

    bool IAttachableToEntity.IsAttachable(Entity toEntity, ItemStack itemStack) => true;

    int IAttachableToEntity.RequiresBehindSlots { get; set; }
}
