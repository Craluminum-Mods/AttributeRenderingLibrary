using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace AttributeRenderingLibrary;

public class ItemShapeTexturesFromAttributes : Item, IShapeTexturesFromAttributes, ICollectiblePropertiesSupplier, IContainedMeshSource, IContainedCustomName, IAttachableToEntity
{
    #region Collectible properties
    public Dictionary<string, CompositeShape> shapeByType { get; protected set; }
    public Dictionary<string, Dictionary<string, CompositeTexture>> texturesByType { get; protected set; }
    public Dictionary<string, List<object>> NameByType { get; protected set; }
    public Dictionary<string, List<object>> DescriptionByType { get; protected set; }
    public Dictionary<string, List<object>> ContainedNameByType { get; protected set; }
    public Dictionary<string, List<object>> ContainedDescriptionByType { get; protected set; }
    public Dictionary<string, EnumItemStorageFlags> StorageFlagsByType { get; protected set; }
    public Dictionary<string, int> DurabilityByType { get; protected set; }
    public Dictionary<string, float> AttackPowerByType { get; protected set; }
    public Dictionary<string, float> AttackRangeByType { get; protected set; }
    public Dictionary<string, Dictionary<EnumBlockMaterial, float>> MiningSpeedByType { get; protected set; }
    public Dictionary<string, EnumItemDamageSource[]> DamagedByByType { get; protected set; }
    public Dictionary<string, EnumTool?> ToolByType { get; protected set; }
    public Dictionary<string, int> ToolTierByType { get; protected set; }
    #endregion
    #region Collectible properties (resolvable)
    public Dictionary<string, CombustibleProperties> CombustiblePropsType { get; protected set; }
    public Dictionary<string, FoodNutritionProperties> NutritionPropsType { get; protected set; }
    public Dictionary<string, GrindingProperties> GrindingPropsType { get; protected set; }
    public Dictionary<string, CrushingProperties> CrushingPropsType { get; protected set; }
    public Dictionary<string, TransitionableProperties[]> TransitionablePropsType { get; protected set; }
    #endregion
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
    public Dictionary<string, System.Collections.Generic.OrderedDictionary<string, CompositeShape>> AttachedShapeBySlotCodeByType { get; protected set; }
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
        if (Attributes == null) return;

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
        StorageFlagsByType = Attributes["storageFlags"].AsObject<Dictionary<string, EnumItemStorageFlags>>();
        DurabilityByType = Attributes["durability"].AsObject<Dictionary<string, int>>();
        AttackPowerByType = Attributes["attackPower"].AsObject<Dictionary<string, float>>();
        AttackRangeByType = Attributes["attackRange"].AsObject<Dictionary<string, float>>();
        MiningSpeedByType = Attributes["miningSpeed"].AsObject<Dictionary<string, Dictionary<EnumBlockMaterial, float>>>();
        DamagedByByType = Attributes["damagedBy"].AsObject<Dictionary<string, EnumItemDamageSource[]>>();
        ToolByType = Attributes["tool"].AsObject<Dictionary<string, EnumTool?>>();
        ToolTierByType = Attributes["toolTier"].AsObject<Dictionary<string, int>>();
        CombustiblePropsType = Attributes["combustibleProps"].AsObject<Dictionary<string, CombustibleProperties>>();
        NutritionPropsType = Attributes["nutritionProps"].AsObject<Dictionary<string, FoodNutritionProperties>>();
        GrindingPropsType = Attributes["grindingProps"].AsObject<Dictionary<string, GrindingProperties>>();
        CrushingPropsType = Attributes["crushingProps"].AsObject<Dictionary<string, CrushingProperties>>();
        TransitionablePropsType = Attributes["TransitionableProps"].AsObject<Dictionary<string, TransitionableProperties[]>>();

        HeldLeftReadyAnimationByType = Attributes["heldLeftReadyAnimation"].AsObject<Dictionary<string, string>>();
        HeldRightReadyAnimationByType = Attributes["heldRightReadyAnimation"].AsObject<Dictionary<string, string>>();

        HeldLeftTpIdleAnimationByType = Attributes["heldLeftTpIdleAnimation"].AsObject<Dictionary<string, string>>();
        HeldRightTpIdleAnimationByType = Attributes["heldRightTpIdleAnimation"].AsObject<Dictionary<string, string>>();

        HeldTpUseAnimationByType = Attributes["heldTpUseAnimation"].AsObject<Dictionary<string, string>>();
        HeldTpHitAnimationByType = Attributes["heldTpHitAnimation"].AsObject<Dictionary<string, string>>();

        AttachedShapeBySlotCodeByType = Attributes["STFA_attachableToEntity"]?["attachedShapeBySlotCode"].AsObject<Dictionary<string, System.Collections.Generic.OrderedDictionary<string, CompositeShape>>>();
        CategoryCodeByType = Attributes["STFA_attachableToEntity"]?["categoryCode"].AsObject<Dictionary<string, string>>();
        DisableElementsByType = Attributes["STFA_attachableToEntity"]?["disableElements"].AsObject<Dictionary<string, string[]>>();
        KeepElementsByType = Attributes["STFA_attachableToEntity"]?["keepElements"].AsObject<Dictionary<string, string[]>>();
    }

    public virtual MeshData GetOrCreateMesh(ItemSlot slot, ITextureAtlasAPI targetAtlas)
    {
        ICoreClientAPI clientApi = api as ICoreClientAPI;
        MeshData mesh = RenderExtensions.GenEmptyMesh();

        Variants variants = Variants.FromStack(slot.Itemstack);
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
            overlayPrefix = GetMeshCacheKey(slot);
            prefixedTextureCodes = ShapeOverlayHelper.AddOverlays(clientApi, overlayPrefix, variants, stexSource, shape, rcshape);
        }

        foreach ((string textureCode, CompositeTexture texture) in slot.Itemstack.Item.Textures)
        {
            stexSource.textures[textureCode] = texture;
        }

        ShapeOverlayHelper.BakeVariantTextures(clientApi, stexSource, variants, texturesByType, prefixedTextureCodes, overlayPrefix);

        TesselationMetaData meta = new TesselationMetaData
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

    /// <summary>
    /// Temporary solution for wearable attachments until 1.22 is out with proper wearable support
    /// </summary>
    public virtual MeshData GenWearableMesh(ItemSlot slot, ITextureAtlasAPI targetAtlas)
    {
        ICoreClientAPI clientApi = api as ICoreClientAPI;
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

        Variants variants = Variants.FromStack(slot.Itemstack);
        variants.FindByVariant(shapeByType, out CompositeShape ucshape);
        ucshape ??= slot.Itemstack.Item.Shape;


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
                Shape oshape = Vintagestory.API.Common.Shape.TryGet(clientApi, overlay.Base.CopyWithPathPrefixAndAppendixOnce("shapes/", ".json"));
                if (oshape == null)
                {
                    LoggerUtil.Warn(clientApi, this, $"Wearable shape {rcshape.Base} overlay {overlay.Base} defined in {slot.Itemstack.Class} {slot.Itemstack.Collectible.Code} not found or errored, was supposed to be at {rcshape.Base}. Item will be invisible.");
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

        string key = GetMeshCacheKey(renderinfo.InSlot);

        if (!meshRefs.TryGetValue(key, out MultiTextureMeshRef meshref))
        {
            MeshData mesh = GenMesh(renderinfo.InSlot, clientApi.ItemTextureAtlas, null);
            meshref = clientApi.Render.UploadMultiTextureMesh(mesh);
            meshRefs[key] = meshref;
        }

        renderinfo.ModelRef = meshref;
        renderinfo.NormalShaded = true;

        base.OnBeforeRender(clientApi, itemstack, target, ref renderinfo);
    }

    public override string GetHeldItemName(ItemStack itemStack)
    {
        if (!itemStack.FindByVariant(NameByType, out List<object> langKeys, out Variants variants) || langKeys == null || langKeys.Count == 0)
        {
            return base.GetHeldItemName(itemStack);
        }
        return variants.GetName(langKeys);
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

        if (!inSlot.Itemstack.FindByVariant(DescriptionByType, out List<object> langKeys, out Variants variants) || langKeys == null || langKeys.Count == 0)
        {
            return;
        }
        variants.GetDescription(dsc, langKeys);
        variants.GetDebugDescription(dsc, withDebugInfo);
    }

    public override EnumItemStorageFlags GetStorageFlags(ItemStack itemstack)
    {
        if (!itemstack.FindByVariant(StorageFlagsByType, out EnumItemStorageFlags storageFlags))
        {
            return base.GetStorageFlags(itemstack);
        }
        return storageFlags;
    }

    public override int GetMaxDurability(ItemStack itemstack)
    {
        if (!itemstack.FindByVariant(DurabilityByType, out int durability))
        {
            return base.GetMaxDurability(itemstack);
        }
        return durability;
    }

    public override float GetAttackPower(ItemStack stack)
    {
        if (!stack.FindByVariant(AttackPowerByType, out float attackPower))
        {
            return base.GetAttackPower(stack);
        }
        return attackPower;
    }

    public override float GetAttackRange(IItemStack withItemStack)
    {
        if (!(withItemStack as ItemStack).FindByVariant(AttackRangeByType, out float attackRange))
        {
            return base.GetAttackRange(withItemStack);
        }
        return attackRange;
    }

    public override Dictionary<EnumBlockMaterial, float> GetMiningSpeeds(ItemSlot slot)
    {
        if (!slot.Itemstack.FindByVariant(MiningSpeedByType, out Dictionary<EnumBlockMaterial, float> miningSpeed))
        {
            return base.GetMiningSpeeds(slot);
        }
        return miningSpeed;
    }

    public override EnumItemDamageSource[] GetDamagedBy(ItemSlot slot)
    {
        if (!slot.Itemstack.FindByVariant(DamagedByByType, out EnumItemDamageSource[] damagedBy))
        {
            return base.GetDamagedBy(slot);
        }
        return damagedBy;
    }

    public override EnumTool? GetTool(ItemSlot slot)
    {
        if (!slot.Itemstack.FindByVariant(ToolByType, out EnumTool? tool))
        {
            return base.GetTool(slot);
        }
        return tool;
    }

    public override int GetToolTier(ItemSlot slot)
    {
        if (!slot.Itemstack.FindByVariant(ToolTierByType, out int toolTier))
        {
            return base.GetToolTier(slot);
        }
        return toolTier;
    }

    public override CombustibleProperties GetCombustibleProperties(IWorldAccessor world, ItemStack itemstack, BlockPos pos)
    {
        if (!itemstack.FindByVariant(CombustiblePropsType, out CombustibleProperties props, out Variants variants))
        {
            return base.GetCombustibleProperties(world, itemstack, pos);
        }

        CombustibleProperties clonedProps = props.Clone();
        if (clonedProps.SmeltedStack != null)
        {
            JsonItemStack resultStack = variants.ReplacePlaceholders(clonedProps.SmeltedStack);
            if (!resultStack.Resolve(world, ""))
            {
                LoggerUtil.Warn(api, this, $"Smelted stack with code '{resultStack.Code}' cannot be resolved for '{itemstack.Collectible.Code}' in '{this}' class for {nameof(CombustibleProperties)} by attributes. Will use default properties instead.");
                return base.GetCombustibleProperties(world, itemstack, pos);
            }
        }
        return clonedProps;
    }

    public override FoodNutritionProperties GetNutritionProperties(IWorldAccessor world, ItemStack itemstack, Entity forEntity)
    {
        if (!itemstack.FindByVariant(NutritionPropsType, out FoodNutritionProperties props, out Variants variants))
        {
            return base.GetNutritionProperties(world, itemstack, forEntity);
        }

        FoodNutritionProperties clonedProps = props.Clone();
        if (clonedProps.EatenStack != null)
        {
            JsonItemStack resultStack = variants.ReplacePlaceholders(clonedProps.EatenStack);
            if (!resultStack.Resolve(world, ""))
            {
                LoggerUtil.Warn(api, this, $"Eaten stack with code '{resultStack.Code}' cannot be resolved for '{itemstack.Collectible.Code}' in '{this}' class for {nameof(FoodNutritionProperties)} by attributes. Will use default properties instead.");
                return base.GetNutritionProperties(world, itemstack, forEntity);
            }
        }
        return clonedProps;
    }

    public override GrindingProperties GetGrindingProperties(IWorldAccessor world, ItemStack itemstack)
    {
        if (!itemstack.FindByVariant(GrindingPropsType, out GrindingProperties props, out Variants variants))
        {
            return base.GetGrindingProperties(world, itemstack);
        }

        GrindingProperties clonedProps = props.Clone();
        if (clonedProps.GroundStack != null)
        {
            JsonItemStack resultStack = variants.ReplacePlaceholders(clonedProps.GroundStack);
            if (!resultStack.Resolve(world, ""))
            {
                LoggerUtil.Warn(api, this, $"Ground stack with code '{resultStack.Code}' cannot be resolved for '{itemstack.Collectible.Code}' in '{this}' class for {nameof(GrindingProperties)} by attributes. Will use default properties instead.");
                return base.GetGrindingProperties(world, itemstack);
            }
        }
        return clonedProps;
    }

    public override CrushingProperties GetCrushingProperties(IWorldAccessor world, ItemStack itemstack)
    {
        if (!itemstack.FindByVariant(CrushingPropsType, out CrushingProperties props, out Variants variants))
        {
            return base.GetCrushingProperties(world, itemstack);
        }

        CrushingProperties clonedProps = props.Clone();
        if (clonedProps.CrushedStack != null)
        {
            JsonItemStack resultStack = variants.ReplacePlaceholders(clonedProps.CrushedStack);
            if (!resultStack.Resolve(world, ""))
            {
                LoggerUtil.Warn(api, this, $"Crushed stack with code '{resultStack.Code}' cannot be resolved for '{itemstack.Collectible.Code}' in '{this}' class for {nameof(CrushingProperties)} by attributes. Will use default properties instead.");
                return base.GetCrushingProperties(world, itemstack);
            }
        }
        return clonedProps;
    }

    public override TransitionableProperties[] GetTransitionableProperties(IWorldAccessor world, ItemStack itemstack, Entity forEntity)
    {
        if (!itemstack.FindByVariant(TransitionablePropsType, out TransitionableProperties[] allTypedProps, out Variants variants) || allTypedProps == null)
        {
            return base.GetTransitionableProperties(world, itemstack, forEntity);
        }

        List<TransitionableProperties> allResolvedProps = [];

        for (int i = 0; i < allTypedProps.Length; i++)
        {
            TransitionableProperties clonedProps = allTypedProps[i].Clone();
            if (clonedProps.TransitionedStack != null)
            {
                JsonItemStack resultStack = variants.ReplacePlaceholders(clonedProps.TransitionedStack);
                if (!resultStack.Resolve(world, ""))
                {
                    LoggerUtil.Warn(api, this, $"Transitioned stack with code '{resultStack.Code}' cannot be resolved for '{itemstack.Collectible.Code}' in '{this}' class for {nameof(TransitionableProperties)} by attributes. Will skip it.");
                    continue;
                }
            }

            allResolvedProps.Add(clonedProps);
        }

        return allResolvedProps.ToArray();
    }

    public override string GetHeldReadyAnimation(ItemSlot activeHotbarSlot, Entity forEntity, EnumHand hand)
    {
        Dictionary<string, string> animCodesByType = (hand == EnumHand.Left) ? HeldLeftReadyAnimationByType : HeldRightReadyAnimationByType;
        if (!activeHotbarSlot.Itemstack.FindByVariant(animCodesByType, out string animCode))
        {
            return base.GetHeldReadyAnimation(activeHotbarSlot, forEntity, hand);
        }
        return animCode;
    }

    public override string GetHeldTpIdleAnimation(ItemSlot activeHotbarSlot, Entity forEntity, EnumHand hand)
    {
        Dictionary<string, string> animCodesByType = (hand == EnumHand.Left) ? HeldLeftTpIdleAnimationByType : HeldRightTpIdleAnimationByType;
        if (!activeHotbarSlot.Itemstack.FindByVariant(animCodesByType, out string animCode))
        {
            return base.GetHeldTpIdleAnimation(activeHotbarSlot, forEntity, hand);
        }
        return animCode;
    }

    public override string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity forEntity)
    {
        if (!activeHotbarSlot.Itemstack.FindByVariant(HeldTpUseAnimationByType, out string animCode))
        {
            return base.GetHeldTpUseAnimation(activeHotbarSlot, forEntity);
        }
        return animCode;
    }

    public override string GetHeldTpHitAnimation(ItemSlot slot, Entity byEntity)
    {
        if (!slot.Itemstack.FindByVariant(HeldTpHitAnimationByType, out string animCode))
        {
            return base.GetHeldTpHitAnimation(slot, byEntity);
        }
        return animCode;
    }
    #region IContainedMeshSource
    public virtual MeshData GenMesh(ItemSlot slot, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos)
    {
        // Temporary solution for wearable attachments until 1.22 is out with proper wearable support
        if (slot.Itemstack.ItemAttributes != null && slot.Itemstack.ItemAttributes.IsTrue("wearableAttachment"))
        {
            return GenWearableMesh(slot, targetAtlas);
        }
        return GetOrCreateMesh(slot, targetAtlas);
    }

    public virtual string GetMeshCacheKey(ItemSlot slot)
    {
        string key = $"{slot.Itemstack.Collectible.Code}-{Variants.FromStack(slot.Itemstack)}";

        // Temporary solution for wearable attachments until 1.22 is out with proper wearable support
        if (slot.Itemstack.ItemAttributes != null && slot.Itemstack.ItemAttributes.IsTrue("wearableAttachment"))
        {
            return "ARL-wearableAttachmentModelRef-" + key;
        }
        return key;
    }
    #endregion
    #region IContainedCustomName
    public virtual string GetContainedInfo(ItemSlot inSlot)
    {
        if (!inSlot.Itemstack.FindByVariant(ContainedDescriptionByType, out List<object> langKeys, out Variants variants) || langKeys == null || langKeys.Count == 0)
        {
            return inSlot.Itemstack.GetName();
        }

        StringBuilder dsc = new();
        variants.GetDescription(dsc, langKeys);
        return dsc.ToString();
    }

    public virtual string GetContainedName(ItemSlot inSlot, int quantity)
    {
        if (!inSlot.Itemstack.FindByVariant(ContainedNameByType, out List<object> langKeys, out Variants variants) || langKeys == null || langKeys.Count == 0)
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

    public virtual CompositeShape GetAttachedShape(ItemStack stack, string slotCode)
    {
        if (!stack.FindByVariant(AttachedShapeBySlotCodeByType, out System.Collections.Generic.OrderedDictionary<string, CompositeShape> attachedShapeBySlotCode, out Variants variants) || attachedShapeBySlotCode == null || attachedShapeBySlotCode.Count == 0)
        {
            return iattr?.GetAttachedShape(stack, slotCode);
        }

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
        return iattr?.GetAttachedShape(stack, slotCode);
    }

    public virtual string GetCategoryCode(ItemStack stack)
    {
        if (!stack.FindByVariant(CategoryCodeByType, out string categoryCode))
        {
            return iattr?.GetCategoryCode(stack);
        }
        return categoryCode;
    }

    public virtual string[] GetDisableElements(ItemStack stack)
    {
        if (!stack.FindByVariant(DisableElementsByType, out string[] elems))
        {
            return iattr?.GetDisableElements(stack);
        }
        return elems;
    }

    public virtual string[] GetKeepElements(ItemStack stack)
    {
        if (!stack.FindByVariant(KeepElementsByType, out string[] elems))
        {
            return iattr?.GetKeepElements(stack);
        }
        return elems;
    }

    public virtual string GetTexturePrefixCode(ItemStack stack) => GetMeshCacheKey(new DummySlot(stack));

    public virtual bool IsAttachable(Entity toEntity, ItemStack itemStack) => true;

    public virtual int RequiresBehindSlots { get; set; }
    #endregion
}
