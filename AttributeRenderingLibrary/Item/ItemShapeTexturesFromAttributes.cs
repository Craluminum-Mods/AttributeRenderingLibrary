using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace AttributeRenderingLibrary;

public class ItemShapeTexturesFromAttributes : Item, IShapeTexturesFromAttributes, ICollectiblePropertiesSupplier, IContainedMeshSource, IContainedCustomName, IAttachableToEntity
{
    #region Collectible properties
    public Dictionary<string, TagSet>? TagsByType { get; protected set; }
    public Dictionary<string, CompositeShape>? shapeByType { get; protected set; }
    public Dictionary<string, Dictionary<string, CompositeTexture>>? texturesByType { get; protected set; }
    public Dictionary<string, List<object>>? NameByType { get; protected set; }
    public Dictionary<string, List<object>>? DescriptionByType { get; protected set; }
    public Dictionary<string, List<object>>? ContainedNameByType { get; protected set; }
    public Dictionary<string, List<object>>? ContainedDescriptionByType { get; protected set; }
    public Dictionary<string, byte[]>? LightHsvByType { get; protected set; }
    public Dictionary<string, EnumItemStorageFlags>? StorageFlagsByType { get; protected set; }
    public Dictionary<string, int>? DurabilityByType { get; protected set; }
    public Dictionary<string, float>? AttackPowerByType { get; protected set; }
    public Dictionary<string, float>? AttackRangeByType { get; protected set; }
    public Dictionary<string, Dictionary<EnumBlockMaterial, float>>? MiningSpeedByType { get; protected set; }
    public Dictionary<string, EnumItemDamageSource[]>? DamagedByByType { get; protected set; }
    public Dictionary<string, EnumTool?>? ToolByType { get; protected set; }
    public Dictionary<string, int>? ToolTierByType { get; protected set; }
    #endregion
    #region Collectible properties (resolvable)
    public Dictionary<string, CombustibleProperties>? CombustiblePropsByType { get; protected set; }
    public Dictionary<string, FoodNutritionProperties>? NutritionPropsByType { get; protected set; }
    public Dictionary<string, GrindingProperties>? GrindingPropsByType { get; protected set; }
    public Dictionary<string, CrushingProperties>? CrushingPropsByType { get; protected set; }
    public Dictionary<string, TransitionableProperties[]>? TransitionablePropsByType { get; protected set; }
    public Dictionary<string, JuiceableProperties>? JuiceablePropsByType { get; protected set; }
    public Dictionary<string, DistillationProps>? DistillationPropsByType { get; protected set; }
    #endregion
    #region Animations
    public Dictionary<string, string>? HeldLeftReadyAnimationByType { get; protected set; }
    public Dictionary<string, string>? HeldRightReadyAnimationByType { get; protected set; }

    public Dictionary<string, string>? HeldLeftTpIdleAnimationByType { get; protected set; }
    public Dictionary<string, string>? HeldRightTpIdleAnimationByType { get; protected set; }

    public Dictionary<string, string>? HeldTpUseAnimationByType { get; protected set; }
    public Dictionary<string, string>? HeldTpHitAnimationByType { get; protected set; }
    #endregion
    #region Extra shape overrides
    public Dictionary<string, string[]>? ShapeIgnoreElementsByType { get; protected set; }
    public Dictionary<string, string[]>? ShapeIgnoreElementsCombineByType { get; protected set; }
    public Dictionary<string, string[]>? ShapeSelectiveElementsByType { get; protected set; }
    public Dictionary<string, string[]>? ShapeSelectiveElementsCombineByType { get; protected set; }
    #endregion
    #region IAttachableToEntity
    public Dictionary<string, CompositeShape>? AttachedShapeByType { get; protected set; }
    public Dictionary<string, System.Collections.Generic.OrderedDictionary<string, CompositeShape>>? AttachedShapeBySlotCodeByType { get; protected set; }
    public Dictionary<string, string>? CategoryCodeByType { get; protected set; }
    public Dictionary<string, string[]>? DisableElementsByType { get; protected set; }
    public Dictionary<string, string[]>? KeepElementsByType { get; protected set; }
    public IAttachableToEntity? iattr;
    #endregion

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);

        if (Attributes != null && Attributes.IsTrue("wearableAttachment"))
        {
            LoggerUtil.Warn(api, this, $"'AttributeRenderingLibrary.ItemShapeTexturesFromAttributes' class currently doesn't support wearables properly. Please, replace it with 'AttributeRenderingLibrary.Wearable' behavior for {Code} instead");
        }

        LoadTypes();
        iattr = IAttachableToEntity.FromAttributes(this);
    }

    public override void OnUnloaded(ICoreAPI api)
    {
        base.OnUnloaded(api);
        Dictionary<string, MultiTextureMeshRef>? meshRefs = ObjectCacheUtil.TryGet<Dictionary<string, MultiTextureMeshRef>>(api, "AttributeRenderingLibrary_ItemShapeTexturesFromAttributes_MeshRefs");
        meshRefs?.Foreach(meshRef => meshRef.Value?.Dispose());
        ObjectCacheUtil.Delete(api, "AttributeRenderingLibrary_ItemShapeTexturesFromAttributes_MeshRefs");
    }

    public virtual void LoadTypes()
    {
        if (Attributes is not { Count: > 0 }) return;

        LoadTags();

        shapeByType = Attributes["shape"].AsObject<Dictionary<string, CompositeShape>>();
        texturesByType = Attributes["textures"].AsObject<Dictionary<string, Dictionary<string, CompositeTexture>>>();

        ShapeIgnoreElementsByType = Attributes["shapeIgnoreElements"].AsObject<Dictionary<string, string[]>>();
        ShapeIgnoreElementsCombineByType = Attributes["shapeIgnoreElementsCombine"].AsObject<Dictionary<string, string[]>>();
        ShapeSelectiveElementsByType = Attributes["shapeSelectiveElements"].AsObject<Dictionary<string, string[]>>();
        ShapeSelectiveElementsCombineByType = Attributes["shapeSelectiveElementsCombine"].AsObject<Dictionary<string, string[]>>();

        NameByType = Attributes["name"].AsObject<Dictionary<string, List<object>>>();
        DescriptionByType = Attributes["description"].AsObject<Dictionary<string, List<object>>>();
        ContainedNameByType = Attributes["containedName"].AsObject<Dictionary<string, List<object>>>();
        ContainedDescriptionByType = Attributes["containedDescription"].AsObject<Dictionary<string, List<object>>>();
        LightHsvByType = Attributes["lightHsv"].AsObject<Dictionary<string, byte[]>>();
        StorageFlagsByType = Attributes["storageFlags"].AsObject<Dictionary<string, EnumItemStorageFlags>>();
        DurabilityByType = Attributes["durability"].AsObject<Dictionary<string, int>>();
        AttackPowerByType = Attributes["attackPower"].AsObject<Dictionary<string, float>>();
        AttackRangeByType = Attributes["attackRange"].AsObject<Dictionary<string, float>>();
        MiningSpeedByType = Attributes["miningSpeed"].AsObject<Dictionary<string, Dictionary<EnumBlockMaterial, float>>>();
        DamagedByByType = Attributes["damagedBy"].AsObject<Dictionary<string, EnumItemDamageSource[]>>();
        ToolByType = Attributes["tool"].AsObject<Dictionary<string, EnumTool?>>();
        ToolTierByType = Attributes["toolTier"].AsObject<Dictionary<string, int>>();
        CombustiblePropsByType = Attributes["combustibleProps"].AsObject<Dictionary<string, CombustibleProperties>>();
        NutritionPropsByType = Attributes["nutritionProps"].AsObject<Dictionary<string, FoodNutritionProperties>>();
        GrindingPropsByType = Attributes["grindingProps"].AsObject<Dictionary<string, GrindingProperties>>();
        CrushingPropsByType = Attributes["crushingProps"].AsObject<Dictionary<string, CrushingProperties>>();
        TransitionablePropsByType = Attributes["transitionableProps"].AsObject<Dictionary<string, TransitionableProperties[]>>();
        JuiceablePropsByType = Attributes["juiceableProperties"].AsObject<Dictionary<string, JuiceableProperties>>();
        DistillationPropsByType = Attributes["distillationProps"].AsObject<Dictionary<string, DistillationProps>>();

        HeldLeftReadyAnimationByType = Attributes["heldLeftReadyAnimation"].AsObject<Dictionary<string, string>>();
        HeldRightReadyAnimationByType = Attributes["heldRightReadyAnimation"].AsObject<Dictionary<string, string>>();

        HeldLeftTpIdleAnimationByType = Attributes["heldLeftTpIdleAnimation"].AsObject<Dictionary<string, string>>();
        HeldRightTpIdleAnimationByType = Attributes["heldRightTpIdleAnimation"].AsObject<Dictionary<string, string>>();

        HeldTpUseAnimationByType = Attributes["heldTpUseAnimation"].AsObject<Dictionary<string, string>>();
        HeldTpHitAnimationByType = Attributes["heldTpHitAnimation"].AsObject<Dictionary<string, string>>();

        AttachedShapeByType = Attributes["STFA_attachableToEntity"]?["attachedShape"].AsObject<Dictionary<string, CompositeShape>>();
        AttachedShapeBySlotCodeByType = Attributes["STFA_attachableToEntity"]?["attachedShapeBySlotCode"].AsObject<Dictionary<string, System.Collections.Generic.OrderedDictionary<string, CompositeShape>>>();
        CategoryCodeByType = Attributes["STFA_attachableToEntity"]?["categoryCode"].AsObject<Dictionary<string, string>>();
        DisableElementsByType = Attributes["STFA_attachableToEntity"]?["disableElements"].AsObject<Dictionary<string, string[]>>();
        KeepElementsByType = Attributes["STFA_attachableToEntity"]?["keepElements"].AsObject<Dictionary<string, string[]>>();
    }

    public virtual void LoadTags()
    {
        Dictionary<string, List<string>>? unresolvedTags = Attributes["tags"].AsObject<Dictionary<string, List<string>>>();
        if (unresolvedTags is not { Count: > 0 }) return;

        TagsByType = [];

        foreach ((string type, List<string> tags) in unresolvedTags)
        {
            Core.Api.CollectibleTagRegistry.TryCreateTagSetAndLogIssues(out TagSet resolvedTags, tags);
            TagsByType.Add(type, resolvedTags);
        }
    }

    public override TagSet GetTags(ItemStack stack) => !stack.FindByVariant(TagsByType, out TagSet tags)
            ? base.GetTags(stack)
            : tags;

    public virtual MeshData GetOrCreateMesh(ItemSlot slot, ITextureAtlasAPI targetAtlas)
    {
        return GetOrCreateMesh(slot, targetAtlas, overrideShape: null);
    }

    public virtual MeshData GetOrCreateMesh(ItemSlot slot, ITextureAtlasAPI targetAtlas, CompositeShape overrideShape)
    {
        ICoreClientAPI clientApi = api as ICoreClientAPI;
        MeshData mesh = RenderExtensions.GenEmptyMesh();

        Variants variants = Variants.FromStack(slot.Itemstack);

        Shape? shape = GetShape(slot, variants, overrideShape, out CompositeShape? rcshape);

        if (shape == null || rcshape == null) return RenderExtensions.GetUnknownItemModelData(clientApi);

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

        TesselationMetaData meta = new()
        {
            QuantityElements = rcshape.QuantityElements,
            SelectiveElements = GetShapeSelectiveElements(slot.Itemstack, rcshape),
            IgnoreElements = GetShapeIgnoreElements(slot.Itemstack, rcshape),
            TexSource = stexSource,
            TypeForLogging = "ShapeTexturesFromAttributes item"
        };

        clientApi.Tesselator.TesselateShape(meta, shape, out mesh);
        return mesh;
    }

    public virtual Shape? GetShape(ItemSlot? slot, Variants variants, CompositeShape? overrideShape, out CompositeShape? originalShape)
    {
        CompositeShape? ucshape = overrideShape;
        if (ucshape == null)
        {
            variants.FindByVariant(shapeByType!, out ucshape);
            ucshape ??= Shape;
        }
        if (ucshape == null)
        {
            originalShape = ucshape;
            return null;
        }

        CompositeShape rcshape = variants.ReplacePlaceholders(ucshape.Clone());
        rcshape.Base = rcshape.Base.CopyWithPathPrefixAndAppendixOnce("shapes/", ".json");

        originalShape = rcshape;
        return Vintagestory.API.Common.Shape.TryGet(api, rcshape.Base);
    }

    public virtual string[] GetShapeIgnoreElements(ItemStack itemStack, CompositeShape cshape)
    {
        if (ShapeIgnoreElementsByType is { Count: > 0 })
        {
            Variants variants = Variants.FromStack(itemStack);
            if (variants.FindByVariant(ShapeIgnoreElementsByType, out string[] elements) && elements != null)
            {
                return variants.ReplacePlaceholders(elements).Append(cshape.IgnoreElements);
            }
        }

        if (ShapeIgnoreElementsCombineByType is { Count: > 0 })
        {
            Variants variants = Variants.FromStack(itemStack);
            List<string> result = cshape.IgnoreElements is null ? new() : new(cshape.IgnoreElements);
            foreach (string[] elements in variants.FindAllByVariant(ShapeIgnoreElementsCombineByType))
            {
                result.AddRange(variants.ReplacePlaceholders(elements));
            }
            return [.. result];
        }

        return cshape.IgnoreElements;
    }

    public virtual string[] GetShapeSelectiveElements(ItemStack itemStack, CompositeShape cshape)
    {
        if (ShapeSelectiveElementsByType is { Count: > 0 })
        {
            Variants variants = Variants.FromStack(itemStack);
            if (variants.FindByVariant(ShapeSelectiveElementsByType, out string[] elements) && elements != null)
            {
                return variants.ReplacePlaceholders(elements).Append(cshape.SelectiveElements);
            }
        }

        if (ShapeSelectiveElementsCombineByType is { Count: > 0 })
        {
            Variants variants = Variants.FromStack(itemStack);
            List<string> result = cshape.SelectiveElements is null ? new() : new(cshape.SelectiveElements);
            foreach (string[] elements in variants.FindAllByVariant(ShapeSelectiveElementsCombineByType))
            {
                result.AddRange(variants.ReplacePlaceholders(elements));
            }
            return [.. result];
        }

        return cshape.SelectiveElements;
    }

    public override void OnBeforeRender(ICoreClientAPI clientApi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
    {
        Dictionary<string, MultiTextureMeshRef> meshRefs = ObjectCacheUtil.GetOrCreate(clientApi, "AttributeRenderingLibrary_ItemShapeTexturesFromAttributes_MeshRefs", () => new Dictionary<string, MultiTextureMeshRef>());

        string key = GetMeshCacheKey(renderinfo.InSlot);

        if (!meshRefs.TryGetValue(key, out MultiTextureMeshRef? meshref))
        {
            MeshData mesh = GenMesh(renderinfo.InSlot, clientApi.ItemTextureAtlas, null);
            meshref = clientApi.Render.UploadMultiTextureMesh(mesh);
            meshRefs[key] = meshref;
        }

        renderinfo.ModelRef = meshref;
        renderinfo.NormalShaded = true;

        base.OnBeforeRender(clientApi, itemstack, target, ref renderinfo);
    }

    public override string GetHeldItemName(ItemStack itemStack) => !itemStack.FindByVariant(NameByType, out List<object> langKeys, out Variants variants) || langKeys is not { Count: > 0 }
            ? base.GetHeldItemName(itemStack)
            : variants.GetName(langKeys);

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

        if (!inSlot.Itemstack.FindByVariant(DescriptionByType, out List<object> langKeys, out Variants variants) || langKeys is not { Count: > 0 })
        {
            return;
        }
        variants.GetDescription(dsc, langKeys);
        variants.GetDebugDescription(dsc, withDebugInfo);
    }

    public override byte[] GetLightHsv(IBlockAccessor blockAccessor, BlockPos pos, ItemStack stack) => !stack.FindByVariant(LightHsvByType, out var lightHsv)
            ? base.GetLightHsv(blockAccessor, pos, stack)
            : lightHsv;

    public override EnumItemStorageFlags GetStorageFlags(ItemStack itemstack) => !itemstack.FindByVariant(StorageFlagsByType, out var storageFlags)
            ? base.GetStorageFlags(itemstack)
            : storageFlags;

    public override int GetMaxDurability(ItemStack itemstack) => !itemstack.FindByVariant(DurabilityByType, out int durability)
            ? base.GetMaxDurability(itemstack)
            : durability;

    public override float GetAttackPower(ItemStack stack) => !stack.FindByVariant(AttackPowerByType, out float attackPower)
            ? base.GetAttackPower(stack)
            : attackPower;

    public override float GetAttackRange(IItemStack withItemStack) => !(withItemStack as ItemStack).FindByVariant(AttackRangeByType, out float attackRange)
            ? base.GetAttackRange(withItemStack)
            : attackRange;

    public override Dictionary<EnumBlockMaterial, float> GetMiningSpeeds(ItemSlot slot) => !slot.Itemstack.FindByVariant(MiningSpeedByType, out var miningSpeed)
            ? base.GetMiningSpeeds(slot)
            : miningSpeed;

    public override EnumItemDamageSource[] GetDamagedBy(ItemSlot slot) => !slot.Itemstack.FindByVariant(DamagedByByType, out var damagedBy)
            ? base.GetDamagedBy(slot)
            : damagedBy;

    public override EnumTool? GetTool(ItemSlot slot) => !slot.Itemstack.FindByVariant(ToolByType, out var tool)
            ? base.GetTool(slot)
            : tool;

    public override int GetToolTier(ItemSlot slot) => !slot.Itemstack.FindByVariant(ToolTierByType, out int toolTier)
            ? base.GetToolTier(slot)
            : toolTier;

    public override CombustibleProperties GetCombustibleProperties(IWorldAccessor world, ItemStack stack, BlockPos pos)
    {
        if (!stack.FindByVariant(CombustiblePropsByType, out CombustibleProperties props, out Variants variants) || props == null)
        {
            return base.GetCombustibleProperties(world, stack, pos);
        }

        if (props.SmeltedStack == null)
        {
            return props;
        }

        CombustibleProperties clonedProps = props.Clone();
        JsonItemStack resultStack = variants.ReplacePlaceholders(clonedProps.SmeltedStack);
        if (!resultStack.Resolve(world, ""))
        {
            LoggerUtil.Warn(api, this, $"Smelted stack with code '{resultStack.Code}' cannot be resolved for '{stack.Collectible.Code}'. Will skip it.");
            clonedProps.SmeltedStack = null;
        }
        return clonedProps;
    }

    public override FoodNutritionProperties GetNutritionProperties(IWorldAccessor world, ItemStack stack, Entity forEntity)
    {
        if (!stack.FindByVariant(NutritionPropsByType, out FoodNutritionProperties props, out Variants variants) || props == null)
        {
            return base.GetNutritionProperties(world, stack, forEntity);
        }

        if (props.EatenStack == null)
        {
            return props;
        }

        FoodNutritionProperties clonedProps = props.Clone();
        JsonItemStack resultStack = variants.ReplacePlaceholders(clonedProps.EatenStack);
        if (!resultStack.Resolve(world, ""))
        {
            LoggerUtil.Warn(api, this, $"Eaten stack with code '{resultStack.Code}' cannot be resolved for '{stack.Collectible.Code}'. Will skip it.");
            clonedProps.EatenStack = null;
        }
        return clonedProps;
    }

    public override GrindingProperties GetGrindingProperties(IWorldAccessor world, ItemStack stack)
    {
        if (!stack.FindByVariant(GrindingPropsByType, out GrindingProperties props, out Variants variants) || props == null)
        {
            return base.GetGrindingProperties(world, stack);
        }

        if (props.GroundStack == null)
        {
            return props;
        }

        GrindingProperties clonedProps = props.Clone();
        JsonItemStack resultStack = variants.ReplacePlaceholders(clonedProps.GroundStack);
        if (!resultStack.Resolve(world, ""))
        {
            LoggerUtil.Warn(api, this, $"Ground stack with code '{resultStack.Code}' cannot be resolved for '{stack.Collectible.Code}'. Will skip it.");
            clonedProps.GroundStack = null;
        }
        return clonedProps;
    }

    public override CrushingProperties GetCrushingProperties(IWorldAccessor world, ItemStack stack)
    {
        if (!stack.FindByVariant(CrushingPropsByType, out CrushingProperties props, out Variants variants) || props == null)
        {
            return base.GetCrushingProperties(world, stack);
        }

        if (props.CrushedStack == null)
        {
            return props;
        }

        CrushingProperties clonedProps = props.Clone();
        JsonItemStack resultStack = variants.ReplacePlaceholders(clonedProps.CrushedStack);
        if (!resultStack.Resolve(world, ""))
        {
            LoggerUtil.Warn(api, this, $"Crushed stack with code '{resultStack.Code}' cannot be resolved for '{stack.Collectible.Code}'. Will skip it.");
            clonedProps.CrushedStack = null;
        }
        return clonedProps;
    }

    public override TransitionableProperties[] GetTransitionableProperties(IWorldAccessor world, ItemStack stack, Entity forEntity)
    {
        if (!stack.FindByVariant(TransitionablePropsByType, out TransitionableProperties[] allTypedProps, out Variants variants) || allTypedProps == null)
        {
            return base.GetTransitionableProperties(world, stack, forEntity);
        }

        List<TransitionableProperties> allResolvedProps = [];

        for (int i = 0; i < allTypedProps.Length; i++)
        {
            if (allTypedProps[i].TransitionedStack == null)
            {
                allResolvedProps.Add(allTypedProps[i]);
                continue;
            }

            TransitionableProperties clonedProps = allTypedProps[i].Clone();
            JsonItemStack resultStack = variants.ReplacePlaceholders(clonedProps.TransitionedStack);
            if (!resultStack.Resolve(world, ""))
            {
                LoggerUtil.Warn(api, this, $"Transitioned stack with code '{resultStack.Code}' cannot be resolved for '{stack.Collectible.Code}'. Will skip it.");
                continue;
            }

            allResolvedProps.Add(clonedProps);
        }

        return [.. allResolvedProps];
    }

    public override string GetHeldReadyAnimation(ItemSlot activeHotbarSlot, Entity forEntity, EnumHand hand)
    {
        Dictionary<string, string> animCodesByType = (hand == EnumHand.Left) ? HeldLeftReadyAnimationByType : HeldRightReadyAnimationByType;
        return !activeHotbarSlot.Itemstack.FindByVariant(animCodesByType, out string animCode)
            ? base.GetHeldReadyAnimation(activeHotbarSlot, forEntity, hand)
            : animCode;
    }

    public override string GetHeldTpIdleAnimation(ItemSlot activeHotbarSlot, Entity forEntity, EnumHand hand)
    {
        Dictionary<string, string> animCodesByType = (hand == EnumHand.Left) ? HeldLeftTpIdleAnimationByType : HeldRightTpIdleAnimationByType;
        return !activeHotbarSlot.Itemstack.FindByVariant(animCodesByType, out string animCode)
            ? base.GetHeldTpIdleAnimation(activeHotbarSlot, forEntity, hand)
            : animCode;
    }

    public override string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity forEntity) => !activeHotbarSlot.Itemstack.FindByVariant(HeldTpUseAnimationByType, out string animCode)
            ? base.GetHeldTpUseAnimation(activeHotbarSlot, forEntity)
            : animCode;

    public override string GetHeldTpHitAnimation(ItemSlot slot, Entity byEntity) => !slot.Itemstack.FindByVariant(HeldTpHitAnimationByType, out string animCode)
            ? base.GetHeldTpHitAnimation(slot, byEntity)
            : animCode;
    #region IContainedMeshSource
    public virtual MeshData GenMesh(ItemSlot slot, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos)
    {
        return GetOrCreateMesh(slot, targetAtlas);
    }

    public virtual string GetMeshCacheKey(ItemSlot slot)
    {
        return $"{slot.Itemstack.Collectible.Code}-{Variants.FromStack(slot.Itemstack)}";
    }
    #endregion
    #region IContainedCustomName
    public virtual string GetContainedInfo(ItemSlot inSlot)
    {
        if (!inSlot.Itemstack.FindByVariant(ContainedDescriptionByType, out List<object> langKeys, out Variants variants) || langKeys is not { Count: > 0 })
        {
            return inSlot.Itemstack.GetName();
        }

        StringBuilder dsc = new();
        variants.GetDescription(dsc, langKeys);
        return dsc.ToString();
    }

    public virtual string GetContainedName(ItemSlot inSlot, int quantity)
    {
        if (!inSlot.Itemstack.FindByVariant(ContainedNameByType, out List<object> langKeys, out Variants variants) || langKeys is not { Count: > 0 })
        {
            return inSlot.Itemstack.GetName();
        }

        StringBuilder dsc = new();
        variants.GetDescription(dsc, langKeys);
        return dsc.ToString();
    }
    #endregion
    #region IAttachableToEntity
    public virtual void CollectTextures(ItemStack stack, Shape shape, string texturePrefixCode, Dictionary<string, CompositeTexture> intoDict)
    {
        foreach ((string textureCode, CompositeTexture texture) in stack.Item.Textures)
        {
            shape.Textures[textureCode] = texture.Baked.BakedName;
        }

        Dictionary<string, Dictionary<string, CompositeTexture>> texturesByType = [];

        if (stack.Collectible.GetCollectibleInterface<IShapeTexturesFromAttributes>() is { } STFA)
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

    public virtual CompositeShape GetAttachedShape(ItemStack stack, string slotCode)
    {
        Variants variants = Variants.FromStack(stack);

        if (stack.FindByVariant(AttachedShapeByType, out CompositeShape attachedShape) && attachedShape != null)
        {
            CompositeShape rcshape = variants.ReplacePlaceholders(attachedShape.Clone());
            return rcshape.RemoveNonExistingOverlays(api)!;
        }

        if (stack.FindByVariant(AttachedShapeBySlotCodeByType, out System.Collections.Generic.OrderedDictionary<string, CompositeShape> attachedShapeBySlotCode) && attachedShapeBySlotCode is { Count: > 0 })
        {
            foreach ((string _slotCode, CompositeShape ucshape) in attachedShapeBySlotCode)
            {
                if (WildcardUtil.Match(_slotCode, slotCode))
                {
                    CompositeShape rcshape = variants.ReplacePlaceholders(ucshape.Clone());
                    return rcshape.RemoveNonExistingOverlays(api)!;
                }
            }
        }

        if (iattr is AttributeAttachableToEntity attachableToEntity)
        {
            if (attachableToEntity.AttachedShape != null)
            {
                return attachableToEntity.AttachedShape;
            }
            if (attachableToEntity.AttachedShapeBySlotCode != null)
            {
                foreach (var val in attachableToEntity.AttachedShapeBySlotCode)
                {
                    if (WildcardUtil.Match(val.Key, slotCode))
                    {
                        return val.Value;
                    }
                }
            }
        }

        _ = GetShape(new DummySlot(stack), variants, null, out CompositeShape? compositeShape);
        return compositeShape.RemoveNonExistingOverlays(api)!;
    }

    public virtual string GetCategoryCode(ItemStack stack) => !stack.FindByVariant(CategoryCodeByType, out string categoryCode)
            ? (iattr?.GetCategoryCode(stack))
            : categoryCode;

    public virtual string[] GetDisableElements(ItemStack stack) => !stack.FindByVariant(DisableElementsByType, out string[] elems)
            ? (iattr?.GetDisableElements(stack))
            : elems;

    public virtual string[] GetKeepElements(ItemStack stack) => !stack.FindByVariant(KeepElementsByType, out string[] elems)
            ? (iattr?.GetKeepElements(stack))
            : elems;

    public virtual string GetTexturePrefixCode(ItemStack stack) => GetMeshCacheKey(new DummySlot(stack));

    public virtual bool IsAttachable(Entity toEntity, ItemStack itemStack) => true;

    public virtual int RequiresBehindSlots { get; set; }
    #endregion
    #region ICollectiblePropertiesSupplier
    public virtual JuiceableProperties GetJuiceableProperties(ItemStack stack, ref EnumHandling handling)
    {
        if (!stack.FindByVariant(JuiceablePropsByType, out JuiceableProperties result, out Variants variants) || result == null)
        {
            return result;
        }

        JuiceableProperties clonedProps = new()
        {
            LitresPerItem = result.LitresPerItem,
            PressedDryRatio = result.PressedDryRatio,
            LiquidStack = result.LiquidStack?.Clone(),
            PressedStack = result.PressedStack?.Clone(),
            ReturnStack = result.ReturnStack?.Clone()
        };

        if (clonedProps.LiquidStack != null)
        {
            clonedProps.LiquidStack = variants.ReplacePlaceholders(clonedProps.LiquidStack);
            if (!clonedProps.LiquidStack.Resolve(api.World, ""))
            {
                LoggerUtil.Warn(api, this, $"Liquid stack with code '{clonedProps.LiquidStack.Code}' cannot be resolved for '{stack.Collectible.Code}'. Will skip it.");
                clonedProps.LiquidStack = null;
            }
        }
        if (clonedProps.PressedStack != null)
        {
            clonedProps.PressedStack = variants.ReplacePlaceholders(clonedProps.PressedStack);
            if (!clonedProps.PressedStack.Resolve(api.World, ""))
            {
                LoggerUtil.Warn(api, this, $"Pressed stack with code '{clonedProps.PressedStack.Code}' cannot be resolved for '{stack.Collectible.Code}'. Will skip it.");
                clonedProps.PressedStack = null;
            }
        }
        if (clonedProps.ReturnStack != null)
        {
            clonedProps.ReturnStack = variants.ReplacePlaceholders(clonedProps.ReturnStack);
            if (!clonedProps.ReturnStack.Resolve(api.World, ""))
            {
                LoggerUtil.Warn(api, this, $"Return stack with code '{clonedProps.ReturnStack.Code}' cannot be resolved for '{stack.Collectible.Code}'. Will skip it.");
                clonedProps.ReturnStack = null;
            }
        }

        handling = EnumHandling.PreventSubsequent;
        return clonedProps;
    }

    public virtual DistillationProps GetDistillationProperties(ItemStack stack, ref EnumHandling handling)
    {
        if (!stack.FindByVariant(DistillationPropsByType, out DistillationProps result, out Variants variants) || result == null)
        {
            return result;
        }

        if (result.DistilledStack == null)
        {
            handling = EnumHandling.PreventSubsequent;
            return result;
        }

        DistillationProps clonedProps = new()
        {
            DistilledStack = result.DistilledStack?.Clone(),
            Ratio = result.Ratio
        };

        JsonItemStack resultStack = variants.ReplacePlaceholders(clonedProps.DistilledStack);
        if (!resultStack.Resolve(api.World, ""))
        {
            LoggerUtil.Warn(api, this, $"Distilled stack with code '{resultStack.Code}' cannot be resolved for '{stack.Collectible.Code}'. Will skip it.");
            clonedProps.DistilledStack = null;
        }

        handling = EnumHandling.PreventSubsequent;
        return clonedProps;
    }
    #endregion
}
