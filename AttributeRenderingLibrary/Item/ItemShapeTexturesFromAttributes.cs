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

public class ItemShapeTexturesFromAttributes : Item, IShapeTexturesFromAttributes, IContainedMeshSource, IContainedCustomName, IAttachableToEntity
{
    public Dictionary<string, CompositeShape> shapeByType { get; protected set; }
    public Dictionary<string, Dictionary<string, CompositeTexture>> texturesByType { get; protected set; }

    public Dictionary<string, List<object>> NameByType { get; protected set; }
    public Dictionary<string, List<object>> DescriptionByType { get; protected set; }
    public Dictionary<string, List<object>> ContainedDescriptionByType { get; protected set; }
    public Dictionary<string, EnumItemStorageFlags> StorageFlagsByType { get; protected set; }
    public Dictionary<string, int> DurabilityByType { get; protected set; }
    public Dictionary<string, float> AttackPowerByType { get; protected set; }
    public Dictionary<string, float> AttackRangeByType { get; protected set; }
    #region Animations
    public Dictionary<string, string> HeldLeftReadyAnimationByType { get; protected set; }
    public Dictionary<string, string> HeldRightReadyAnimationByType { get; protected set; }

    public Dictionary<string, string> HeldLeftTpIdleAnimationByType { get; protected set; }
    public Dictionary<string, string> HeldRightTpIdleAnimationByType { get; protected set; }

    public Dictionary<string, string> HeldTpUseAnimationByType { get; protected set; }
    public Dictionary<string, string> HeldTpHitAnimationByType { get; protected set; }
    #endregion
    #region Extra shape overrides
    public Dictionary<string, string[]> ShapeIgnoreElementsByType { get; protected set; }
    public Dictionary<string, string[]> ShapeIgnoreElementsCombineByType { get; protected set; }
    public Dictionary<string, string[]> ShapeSelectiveElementsByType { get; protected set; }
    public Dictionary<string, string[]> ShapeSelectiveElementsCombineByType { get; protected set; }
    #endregion
    #region IAttachableToEntity
    public Dictionary<string, OrderedDictionary<string, CompositeShape>> AttachedShapeBySlotCodeByType { get; protected set; }
    public Dictionary<string, string> CategoryCodeByType { get; protected set; }
    public Dictionary<string, string[]> DisableElementsByType { get; protected set; }
    public Dictionary<string, string[]> KeepElementsByType { get; protected set; }
    private IAttachableToEntity iattr;
    #endregion

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);
        LoadTypes();
        iattr = IAttachableToEntity.FromAttributes(this);
    }

    public override void OnUnloaded(ICoreAPI api)
    {
        base.OnUnloaded(api);
        Dictionary<string, MultiTextureMeshRef> meshRefs = ObjectCacheUtil.TryGet<Dictionary<string, MultiTextureMeshRef>>(api, "AttributeRenderingLibrary_ItemShapeTexturesFromAttributes_MeshRefs");
        meshRefs?.Foreach(meshRef => meshRef.Value?.Dispose());
        ObjectCacheUtil.Delete(api, "AttributeRenderingLibrary_ItemShapeTexturesFromAttributes_MeshRefs");
    }

    public virtual void LoadTypes()
    {
        if (Attributes != null)
        {
            shapeByType = Attributes["shape"].AsObject<Dictionary<string, CompositeShape>>();
            texturesByType = Attributes["textures"].AsObject<Dictionary<string, Dictionary<string, CompositeTexture>>>();

            ShapeIgnoreElementsByType = Attributes["shapeIgnoreElements"].AsObject<Dictionary<string, string[]>>();
            ShapeIgnoreElementsCombineByType = Attributes["shapeIgnoreElementsCombine"].AsObject<Dictionary<string, string[]>>();
            ShapeSelectiveElementsByType = Attributes["shapeSelectiveElements"].AsObject<Dictionary<string, string[]>>();
            ShapeSelectiveElementsCombineByType = Attributes["shapeSelectiveElementsCombine"].AsObject<Dictionary<string, string[]>>();

            NameByType = Attributes["name"].AsObject<Dictionary<string, List<object>>>();
            DescriptionByType = Attributes["description"].AsObject<Dictionary<string, List<object>>>();
            ContainedDescriptionByType = Attributes["containedDescription"].AsObject<Dictionary<string, List<object>>>();
            StorageFlagsByType = Attributes["storageFlags"].AsObject<Dictionary<string, EnumItemStorageFlags>>();
            DurabilityByType = Attributes["durability"].AsObject<Dictionary<string, int>>();
            AttackPowerByType = Attributes["attackPower"].AsObject<Dictionary<string, float>>();
            AttackRangeByType = Attributes["attackRange"].AsObject<Dictionary<string, float>>();

            HeldLeftReadyAnimationByType = Attributes["heldLeftReadyAnimation"].AsObject<Dictionary<string, string>>();
            HeldRightReadyAnimationByType = Attributes["heldRightReadyAnimation"].AsObject<Dictionary<string, string>>();

            HeldLeftTpIdleAnimationByType = Attributes["heldLeftTpIdleAnimation"].AsObject<Dictionary<string, string>>();
            HeldRightTpIdleAnimationByType = Attributes["heldRightTpIdleAnimation"].AsObject<Dictionary<string, string>>();

            HeldTpUseAnimationByType = Attributes["heldTpUseAnimation"].AsObject<Dictionary<string, string>>();
            HeldTpHitAnimationByType = Attributes["heldTpHitAnimation"].AsObject<Dictionary<string, string>>();

            AttachedShapeBySlotCodeByType = Attributes["STFA_attachableToEntity"]?["attachedShapeBySlotCode"].AsObject<Dictionary<string, OrderedDictionary<string, CompositeShape>>>();
            CategoryCodeByType = Attributes["STFA_attachableToEntity"]?["categoryCode"].AsObject<Dictionary<string, string>>();
            DisableElementsByType = Attributes["STFA_attachableToEntity"]?["disableElements"].AsObject<Dictionary<string, string[]>>();
            KeepElementsByType = Attributes["STFA_attachableToEntity"]?["keepElements"].AsObject<Dictionary<string, string[]>>();
        }
    }

    public virtual MeshData GetOrCreateMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas)
    {
        ICoreClientAPI clientApi = api as ICoreClientAPI;
        MeshData mesh = RenderExtensions.GenEmptyMesh();

        Variants variants = Variants.FromStack(itemstack);
        variants.FindByVariant(shapeByType, out CompositeShape ucshape);
        ucshape ??= Shape;

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
            TypeForLogging = "ShapeTexturesFromAttributes item"
        };

        clientApi.Tesselator.TesselateShape(meta, shape, out mesh);
        return mesh;
    }

    public virtual string[] GetShapeIgnoreElements(ItemStack itemStack, CompositeShape cshape)
    {
        if (ShapeIgnoreElementsByType is { Count: > 0 })
        {
            Variants variants = Variants.FromStack(itemStack);
            if (variants.FindByVariant(ShapeIgnoreElementsByType, out string[] ignoreElements) && ignoreElements != null)
            {
                return cshape.IgnoreElements.Append(variants.ReplacePlaceholders(ignoreElements));
            }
        }
        
        if (ShapeIgnoreElementsCombineByType is { Count: > 0 })
        {
            Variants variants = Variants.FromStack(itemStack);
            List<string> result = cshape.IgnoreElements is null ? new() : new(cshape.IgnoreElements);
            foreach (string[] subset in variants.FindAllByVariant(ShapeIgnoreElementsCombineByType))
            {
                result.AddRange(variants.ReplacePlaceholders(subset));
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
                return cshape.SelectiveElements.Append(variants.ReplacePlaceholders(selectiveElements));
            }
        }
        
        if (ShapeSelectiveElementsCombineByType is { Count: > 0 })
        {
            Variants variants = Variants.FromStack(itemStack);
            List<string> result = cshape.SelectiveElements is null ? new() : new(cshape.SelectiveElements);
            foreach (string[] subset in variants.FindAllByVariant(ShapeSelectiveElementsCombineByType))
            {
                result.AddRange(variants.ReplacePlaceholders(subset));
            }
            return result.ToArray();
        }
        
        return cshape.SelectiveElements;
    }

    public override void OnBeforeRender(ICoreClientAPI clientApi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
    {
        Dictionary<string, MultiTextureMeshRef> meshRefs = ObjectCacheUtil.GetOrCreate(clientApi, "AttributeRenderingLibrary_ItemShapeTexturesFromAttributes_MeshRefs", () => new Dictionary<string, MultiTextureMeshRef>());

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

    public override string GetHeldItemName(ItemStack itemStack)
    {
        if (NameByType == null || NameByType.Count == 0)
        {
            return base.GetHeldItemName(itemStack);
        }

        Variants variants = Variants.FromStack(itemStack);
        variants.FindByVariant(NameByType, out List<object> _langKeys);

        string name = variants.GetName(_langKeys);
        if (string.IsNullOrEmpty(name))
        {
            name = base.GetHeldItemName(itemStack);
        }
        return name;
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

        if (DescriptionByType == null || DescriptionByType.Count == 0)
        {
            return;
        }

        Variants variants = Variants.FromStack(inSlot.Itemstack);
        variants.FindByVariant(DescriptionByType, out List<object> _langKeys);
        variants.GetDescription(dsc, _langKeys);
        variants.GetDebugDescription(dsc, withDebugInfo);
    }

    public override EnumItemStorageFlags GetStorageFlags(ItemStack itemstack)
    {
        if (StorageFlagsByType == null || StorageFlagsByType.Count == 0)
        {
            return base.GetStorageFlags(itemstack);
        }

        Variants variants = Variants.FromStack(itemstack);
        if (!variants.FindByVariant(StorageFlagsByType, out EnumItemStorageFlags storageFlags))
        {
            return base.GetStorageFlags(itemstack);
        }
        return storageFlags;
    }

    public override int GetMaxDurability(ItemStack itemstack)
    {
        if (DurabilityByType == null || DurabilityByType.Count == 0)
        {
            return base.GetMaxDurability(itemstack);
        }

        Variants variants = Variants.FromStack(itemstack);
        if (!variants.FindByVariant(DurabilityByType, out int durability))
        {
            return base.GetMaxDurability(itemstack);
        }
        return durability;
    }

    public override float GetAttackPower(IItemStack withItemStack)
    {
        if (AttackPowerByType == null || AttackPowerByType.Count == 0)
        {
            return base.GetAttackPower(withItemStack);
        }

        Variants variants = Variants.FromStack(withItemStack as ItemStack);
        if (!variants.FindByVariant(AttackPowerByType, out float attackPower))
        {
            return base.GetAttackPower(withItemStack);
        }
        return attackPower;
    }

    public override float GetAttackRange(IItemStack withItemStack)
    {
        if (AttackRangeByType == null || AttackRangeByType.Count == 0)
        {
            return base.GetAttackRange(withItemStack);
        }

        Variants variants = Variants.FromStack(withItemStack as ItemStack);
        if (!variants.FindByVariant(AttackRangeByType, out float attackRange))
        {
            return base.GetAttackRange(withItemStack);
        }
        return attackRange;
    }

    public override string GetHeldReadyAnimation(ItemSlot activeHotbarSlot, Entity forEntity, EnumHand hand)
    {
        Dictionary<string, string> animCodesByType = (hand == EnumHand.Left) ? HeldLeftReadyAnimationByType : HeldRightReadyAnimationByType;

        if (animCodesByType == null || animCodesByType.Count == 0)
        {
            return base.GetHeldReadyAnimation(activeHotbarSlot, forEntity, hand);
        }

        Variants variants = Variants.FromStack(activeHotbarSlot.Itemstack);
        if (!variants.FindByVariant(animCodesByType, out string animCode))
        {
            return base.GetHeldReadyAnimation(activeHotbarSlot, forEntity, hand);
        }
        return animCode;
    }

    public override string GetHeldTpIdleAnimation(ItemSlot activeHotbarSlot, Entity forEntity, EnumHand hand)
    {
        Dictionary<string, string> animCodesByType = (hand == EnumHand.Left) ? HeldLeftTpIdleAnimationByType : HeldRightTpIdleAnimationByType;

        if (animCodesByType == null || animCodesByType.Count == 0)
        {
            return base.GetHeldTpIdleAnimation(activeHotbarSlot, forEntity, hand);
        }

        Variants variants = Variants.FromStack(activeHotbarSlot.Itemstack);
        if (!variants.FindByVariant(animCodesByType, out string animCode))
        {
            return base.GetHeldTpIdleAnimation(activeHotbarSlot, forEntity, hand);
        }
        return animCode;
    }

    public override string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity forEntity)
    {
        if (HeldTpUseAnimationByType == null || HeldTpUseAnimationByType.Count == 0)
        {
            return base.GetHeldTpUseAnimation(activeHotbarSlot, forEntity);
        }

        Variants variants = Variants.FromStack(activeHotbarSlot.Itemstack);
        if (!variants.FindByVariant(HeldTpUseAnimationByType, out string animCode))
        {
            return base.GetHeldTpUseAnimation(activeHotbarSlot, forEntity);
        }
        return animCode;
    }

    public override string GetHeldTpHitAnimation(ItemSlot slot, Entity byEntity)
    {
        if (HeldTpHitAnimationByType == null || HeldTpHitAnimationByType.Count == 0)
        {
            return base.GetHeldTpHitAnimation(slot, byEntity);
        }

        Variants variants = Variants.FromStack(slot.Itemstack);
        if (!variants.FindByVariant(HeldTpHitAnimationByType, out string animCode))
        {
            return base.GetHeldTpHitAnimation(slot, byEntity);
        }
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
            return GetHeldItemName(inSlot.Itemstack);
        }

        StringBuilder dsc = new();
        Variants variants = Variants.FromStack(inSlot.Itemstack);
        variants.FindByVariant(ContainedDescriptionByType, out List<object> _langKeys);

        if (_langKeys == null || _langKeys.Count == 0)
        {
            return GetHeldItemName(inSlot.Itemstack);
        }

        variants.GetDescription(dsc, _langKeys);
        return dsc.ToString();
    }

    public virtual string GetContainedName(ItemSlot inSlot, int quantity)
    {
        return GetHeldItemName(inSlot.Itemstack);
    }

    void IAttachableToEntity.CollectTextures(ItemStack stack, Shape shape, string texturePrefixCode, Dictionary<string, CompositeTexture> intoDict)
    {
        foreach ((string textureCode, CompositeTexture texture) in stack.Item.Textures)
        {
            shape.Textures[textureCode] = texture.Baked.BakedName;
        }

        Dictionary<string, Dictionary<string, CompositeTexture>> texturesByType = [];

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
                if (!api.Assets.Exists(ctex.Base.CopyWithPathPrefixAndAppendixOnce("textures/", ".png")))
                {
                    ctex.Base.Path = "unknown";
                    ctex.Base.Domain = "game";
                }
                ctex.Bake(api.Assets);
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
                        if (api.Assets.Exists(overlay.Base.Clone().CopyWithPathPrefixAndAppendixOnce("shapes/", ".json")))
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
