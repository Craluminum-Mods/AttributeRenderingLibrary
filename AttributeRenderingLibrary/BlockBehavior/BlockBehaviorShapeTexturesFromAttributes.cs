using Newtonsoft.Json.Linq;
using System;
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

public class BlockBehaviorShapeTexturesFromAttributes(Block block) : StrongBlockBehavior(block), IBlockShapeTexturesFromAttributes, IBlockPropertiesSupplier, IContainedMeshSource, IContainedCustomName, IAttachableToEntity
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
    #region Block properties
    public Dictionary<string, CompositeShape>? shapeInventoryByType { get; protected set; }
    public Dictionary<string, Dictionary<string, CompositeTexture>>? TexturesInventoryByType { get; protected set; }
    public Dictionary<string, Cuboidf[]>? CollisionBoxesByType { get; protected set; }
    public Dictionary<string, Cuboidf[]>? SelectionBoxesByType { get; protected set; }
    public Dictionary<string, BlockDropItemStack[]>? DropsByType { get; protected set; }
    public Dictionary<string, int>? RequiredMiningTierByType { get; protected set; }
    public Dictionary<string, EnumBlockMaterial>? BlockMaterialByType { get; protected set; }
    public Dictionary<string, float[]>? LiquidBarrierOnSidesByType { get; protected set; }
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

#nullable disable
    public ICoreClientAPI clientApi;
    public ICoreAPI coreApi;
#nullable enable

    public override void OnLoaded(ICoreAPI api)
    {
        // blocks with this behavior cannot be chiseled
        block.Attributes ??= new JsonObject(new JObject());
        block.Attributes.Token!["canChisel"] = JToken.FromObject(false);

        clientApi = api as ICoreClientAPI;
        coreApi = api;
        iattr = IAttachableToEntity.FromAttributes(collObj);
    }

    public override void OnUnloaded(ICoreAPI api)
    {
        Dictionary<string, MultiTextureMeshRef>? meshRefs = ObjectCacheUtil.TryGet<Dictionary<string, MultiTextureMeshRef>>(api, "AttributeRenderingLibrary_BehaviorShapeTexturesFromAttributes_MeshRefs");
        meshRefs?.Foreach(meshRef => meshRef.Value?.Dispose());
        ObjectCacheUtil.Delete(api, "AttributeRenderingLibrary_BehaviorShapeTexturesFromAttributes_MeshRefs");

        Dictionary<string, MeshData>? meshes = ObjectCacheUtil.TryGet<Dictionary<string, MeshData>>(api, "AttributeRenderingLibrary_BehaviorShapeTexturesFromAttributes_Meshes");
        meshes?.Foreach(mesh => mesh.Value?.Dispose());
        ObjectCacheUtil.Delete(api, "AttributeRenderingLibrary_BehaviorShapeTexturesFromAttributes_Meshes");
    }

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);
        LoadTypes(properties);
    }

    public virtual void LoadTypes(JsonObject properties)
    {
        if (properties is not { Count: > 0 }) return;

        LoadTags(properties);

        shapeByType = properties["shape"].AsObject<Dictionary<string, CompositeShape>>();
        texturesByType = properties["textures"].AsObject<Dictionary<string, Dictionary<string, CompositeTexture>>>();

        ShapeIgnoreElementsByType = properties["shapeIgnoreElements"].AsObject<Dictionary<string, string[]>>();
        ShapeIgnoreElementsCombineByType = properties["shapeIgnoreElementsCombine"].AsObject<Dictionary<string, string[]>>();
        ShapeSelectiveElementsByType = properties["shapeSelectiveElements"].AsObject<Dictionary<string, string[]>>();
        ShapeSelectiveElementsCombineByType = properties["shapeSelectiveElementsCombine"].AsObject<Dictionary<string, string[]>>();

        NameByType = properties["name"].AsObject<Dictionary<string, List<object>>>();
        DescriptionByType = properties["description"].AsObject<Dictionary<string, List<object>>>();
        ContainedNameByType = properties["containedName"].AsObject<Dictionary<string, List<object>>>();
        ContainedDescriptionByType = properties["containedDescription"].AsObject<Dictionary<string, List<object>>>();
        LightHsvByType = properties["lightHsv"].AsObject<Dictionary<string, byte[]>>();
        StorageFlagsByType = properties["storageFlags"].AsObject<Dictionary<string, EnumItemStorageFlags>>();
        DurabilityByType = properties["durability"].AsObject<Dictionary<string, int>>();
        AttackPowerByType = properties["attackPower"].AsObject<Dictionary<string, float>>();
        AttackRangeByType = properties["attackRange"].AsObject<Dictionary<string, float>>();
        MiningSpeedByType = properties["miningSpeed"].AsObject<Dictionary<string, Dictionary<EnumBlockMaterial, float>>>();
        DamagedByByType = properties["damagedBy"].AsObject<Dictionary<string, EnumItemDamageSource[]>>();
        ToolByType = properties["tool"].AsObject<Dictionary<string, EnumTool?>>();
        ToolTierByType = properties["toolTier"].AsObject<Dictionary<string, int>>();
        CombustiblePropsByType = properties["combustibleProps"].AsObject<Dictionary<string, CombustibleProperties>>();
        NutritionPropsByType = properties["nutritionProps"].AsObject<Dictionary<string, FoodNutritionProperties>>();
        GrindingPropsByType = properties["grindingProps"].AsObject<Dictionary<string, GrindingProperties>>();
        CrushingPropsByType = properties["crushingProps"].AsObject<Dictionary<string, CrushingProperties>>();
        TransitionablePropsByType = properties["transitionableProps"].AsObject<Dictionary<string, TransitionableProperties[]>>();
        JuiceablePropsByType = properties["juiceableProperties"].AsObject<Dictionary<string, JuiceableProperties>>();
        DistillationPropsByType = properties["distillationProps"].AsObject<Dictionary<string, DistillationProps>>();

        HeldLeftReadyAnimationByType = properties["heldLeftReadyAnimation"].AsObject<Dictionary<string, string>>();
        HeldRightReadyAnimationByType = properties["heldRightReadyAnimation"].AsObject<Dictionary<string, string>>();

        HeldLeftTpIdleAnimationByType = properties["heldLeftTpIdleAnimation"].AsObject<Dictionary<string, string>>();
        HeldRightTpIdleAnimationByType = properties["heldRightTpIdleAnimation"].AsObject<Dictionary<string, string>>();

        HeldTpUseAnimationByType = properties["heldTpUseAnimation"].AsObject<Dictionary<string, string>>();
        HeldTpHitAnimationByType = properties["heldTpHitAnimation"].AsObject<Dictionary<string, string>>();

        AttachedShapeByType = properties["STFA_attachableToEntity"]?["attachedShape"].AsObject<Dictionary<string, CompositeShape>>();
        AttachedShapeBySlotCodeByType = properties["STFA_attachableToEntity"]?["attachedShapeBySlotCode"].AsObject<Dictionary<string, System.Collections.Generic.OrderedDictionary<string, CompositeShape>>>();
        CategoryCodeByType = properties["STFA_attachableToEntity"]?["categoryCode"].AsObject<Dictionary<string, string>>();
        DisableElementsByType = properties["STFA_attachableToEntity"]?["disableElements"].AsObject<Dictionary<string, string[]>>();
        KeepElementsByType = properties["STFA_attachableToEntity"]?["keepElements"].AsObject<Dictionary<string, string[]>>();

        shapeInventoryByType = properties["shapeInventory"].AsObject<Dictionary<string, CompositeShape>>();
        TexturesInventoryByType = properties["texturesInventory"].AsObject<Dictionary<string, Dictionary<string, CompositeTexture>>>();
        DropsByType = properties["drops"].AsObject<Dictionary<string, BlockDropItemStack[]>>();
        RequiredMiningTierByType = properties["requiredMiningTier"].AsObject<Dictionary<string, int>>();
        BlockMaterialByType = properties["blockMaterial"].AsObject<Dictionary<string, EnumBlockMaterial>>();
        LiquidBarrierOnSidesByType = properties["liquidBarrierOnSides"].AsObject<Dictionary<string, float[]>>();
        LoadAndResolveCollisionAndSelectionBoxes(properties);

        // it is necessary to add empty attributes for juiceableProperties and distillationProps, otherwise they may not properly show up in handbook
        if (JuiceablePropsByType is { Count: > 0 } && (collObj.Attributes == null || !collObj.Attributes.KeyExists("juiceableProperties")))
        {
            collObj.EnsureAttributesNotNull();
            collObj.Attributes!.Token!["juiceableProperties"] = JToken.FromObject(new JuiceableProperties());
        }
        if (DistillationPropsByType is { Count: > 0 } && (collObj.Attributes == null || !collObj.Attributes.KeyExists("distillationProps")))
        {
            collObj.EnsureAttributesNotNull();
            collObj.Attributes!.Token!["distillationProps"] = JToken.FromObject(new DistillationProps());
        }
    }

    public virtual void LoadTags(JsonObject properties)
    {
        Dictionary<string, List<string>>? unresolvedTags = properties["tags"].AsObject<Dictionary<string, List<string>>>();
        if (unresolvedTags is not { Count: > 0 }) return;

        TagsByType = [];

        foreach ((string type, List<string> tags) in unresolvedTags)
        {
            TagRegistryError tagRegistryError = Core.Api.CollectibleTagRegistry.TryCreateTagSetAndLogIssues(out TagSet resolvedTags, tags);
            TagsByType.Add(type, resolvedTags);
        }
    }

    public virtual void LoadAndResolveCollisionAndSelectionBoxes(JsonObject properties)
    {
        Dictionary<string, RotatableCube[]>? rawCollisionsAndSelections = properties["collisionSelectionBoxes"]?.AsObject<Dictionary<string, RotatableCube[]>>();
        Dictionary<string, RotatableCube[]>? rawCollisions = properties["collisionBoxes"]?.AsObject<Dictionary<string, RotatableCube[]>>();
        Dictionary<string, RotatableCube[]>? rawSelections = properties["selectionBoxes"]?.AsObject<Dictionary<string, RotatableCube[]>>();

        if (rawCollisionsAndSelections is { Count: > 0 })
        {
            CollisionBoxesByType = rawCollisionsAndSelections.ToDictionary(x => x.Key, x => x.Value.ToCuboidf());
            SelectionBoxesByType = rawCollisionsAndSelections.ToDictionary(x => x.Key, x => x.Value.ToCuboidf());
        }
        else
        {
            if (rawCollisions is { Count: > 0 })
                CollisionBoxesByType = rawCollisions.ToDictionary(x => x.Key, x => x.Value.ToCuboidf());

            if (rawSelections is { Count: > 0 })
                SelectionBoxesByType = rawSelections.ToDictionary(x => x.Key, x => x.Value.ToCuboidf());
        }
    }

    public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack, ref EnumHandling handling)
    {
        if (world.BlockAccessor.GetBlockEntity(blockSel.Position)?.GetBehavior<BlockEntityBehaviorShapeTexturesFromAttributes>() is { } beBehavior)
        {
            beBehavior.OnBlockPlaced(byItemStack);
            handling = EnumHandling.Handled;
        }
        return true;
    }

    /// <summary>
    /// Alias of <see cref="GenGuiMesh(ItemSlot, CompositeShape)"/>
    /// </summary>
    /// <remarks>
    /// It is recommended to cache the returned mesh in the ObjectCacheUtil for efficiency
    /// </remarks>
    /// <param name="slot">Slot that holds the block</param>
    /// <returns>Mesh for block (when held, dropped or in gui slot)</returns>
    [Obsolete("Use GenGuiMesh(ItemSlot, CompositeShape) instead")]
    public virtual MeshData GenGuiMesh(ItemSlot slot)
    {
        return GenGuiMesh(slot, overrideShape: null);
    }

    /// <summary>
    /// Used to generate mesh for when block is held, dropped or in gui slot.
    /// </summary>
    /// <remarks>
    /// It is recommended to cache the returned mesh in the ObjectCacheUtil for efficiency
    /// </remarks>
    /// <param name="slot">Slot that holds the block</param>
    /// <param name="overrideShape">Optional custom shape to use instead of the default shape.</param>
    /// <returns>Mesh for block (when held, dropped or in gui slot)</returns>
    public virtual MeshData GenGuiMesh(ItemSlot slot, CompositeShape? overrideShape = null)
    {
        MeshData mesh = RenderExtensions.GenEmptyMesh();

        Variants variants = Variants.FromStack(slot.Itemstack!);

        Shape? shape = GetInventoryShape(slot, variants, overrideShape, out CompositeShape? rcshape);

        if (shape == null || rcshape == null) return RenderExtensions.GetUnknownBlockModelData(clientApi);

        UniversalShapeTextureSource stexSource = new(clientApi, clientApi.BlockTextureAtlas, shape, rcshape.Base.ToString());
        Dictionary<string, AssetLocation>? prefixedTextureCodes = null;
        string overlayPrefix = "";

        if (rcshape.Overlays is { Length: > 0 })
        {
            overlayPrefix = GetMeshCacheKey(slot);
            prefixedTextureCodes = ShapeOverlayHelper.AddOverlays(clientApi, overlayPrefix, variants, stexSource, shape, rcshape);
        }

        foreach ((string textureCode, CompositeTexture texture) in slot.Itemstack!.Block.Textures)
        {
            stexSource.textures[textureCode] = texture;
        }

        bool foundInventoryTextures = slot.Itemstack.FindByVariant(TexturesInventoryByType!, out _);
        ShapeOverlayHelper.BakeVariantTextures(clientApi, stexSource, variants, foundInventoryTextures ? TexturesInventoryByType : texturesByType, prefixedTextureCodes, overlayPrefix);

        TesselationMetaData meta = new()
        {
            QuantityElements = rcshape.QuantityElements,
            SelectiveElements = GetShapeSelectiveElements(variants, rcshape),
            IgnoreElements = GetShapeIgnoreElements(variants, rcshape),
            TexSource = stexSource,
            TypeForLogging = "ShapeTexturesFromAttributes block behavior"
        };

        clientApi.Tesselator.TesselateShape(meta, shape, out mesh);
        return mesh;
    }

    /// <summary>
    /// Alias of <see cref="GetOrCreateMesh(Variants, CompositeShape, BlockPos, string, ITexPositionSource)"/>
    /// </summary>
    /// <param name="variants">Types/variants of the block that are usually stored in BlockEntity or BlockEntityBehavior</param>
    /// <param name="overrideTexturesource">Optional custom texture source, e.g. for decals. When provided, the result is not cached.</param>
    /// <returns>Mesh for placed block</returns>
    [Obsolete("Use GetOrCreateMesh(Variants, CompositeShape, BlockPos, string, ITexPositionSource) instead")]
    public virtual MeshData GetOrCreateMesh(Variants variants, ITexPositionSource overrideTexturesource = null)
    {
        return GetOrCreateMesh(variants: variants, overrideShape: null, atBlockPos: null, extraCacheKey: "", overrideTexturesource: overrideTexturesource);
    }

    /// <summary>
    /// Used to generate mesh for placed block, optionally with decal texture.
    /// </summary>
    /// <remarks>
    /// Note: this method already caches returned mesh.<br/>
    /// Make sure to use unique extra cache key when providing custom shape
    /// </remarks>
    /// <param name="variants">Types/variants of the block that are usually stored in BlockEntity or BlockEntityBehavior</param>
    /// <param name="overrideShape">Optional custom shape to use instead of the default shape.</param>
    /// <param name="extraCacheKey">Additional suffix for the mesh cache key, to differentiate meshes that share the same code and variants.</param>
    /// <param name="atBlockPos">Block position. Use it to get block and/or block entity</param>
    /// <param name="overrideTexturesource">Optional custom texture source, e.g. for decals. When provided, the result is not cached.</param>
    /// <returns>Mesh for placed block</returns>
    public virtual MeshData GetOrCreateMesh(Variants variants, CompositeShape? overrideShape, BlockPos atBlockPos, string extraCacheKey, ITexPositionSource overrideTexturesource = null)
    {
        Dictionary<string, MeshData> cMeshes = ObjectCacheUtil.GetOrCreate(clientApi, "AttributeRenderingLibrary_BehaviorShapeTexturesFromAttributes_Meshes", () => new Dictionary<string, MeshData>());

        string key = string.IsNullOrEmpty(extraCacheKey) ? $"{block.Code}-{variants}" : $"{block.Code}-{variants}-{extraCacheKey}";
        if (overrideTexturesource != null || !cMeshes.TryGetValue(key, out MeshData? mesh))
        {
            mesh = RenderExtensions.GenEmptyMesh();

            Shape? shape = GetShape(slot: null, atBlockPos, variants, overrideShape, out CompositeShape? rcshape);

            if (shape == null || rcshape == null) return RenderExtensions.GetUnknownBlockModelData(clientApi);

            UniversalShapeTextureSource stexSource = new(clientApi, clientApi.BlockTextureAtlas, shape, rcshape.Base.ToString());
            Dictionary<string, AssetLocation>? prefixedTextureCodes = null;
            string overlayPrefix = "";

            if (rcshape.Overlays is { Length: > 0 })
            {
                overlayPrefix = key;
                prefixedTextureCodes = ShapeOverlayHelper.AddOverlays(clientApi, overlayPrefix, variants, stexSource, shape, rcshape);
            }

            foreach ((string textureCode, CompositeTexture texture) in block.Textures)
            {
                stexSource.textures[textureCode] = texture;
            }

            ShapeOverlayHelper.BakeVariantTextures(clientApi, stexSource, variants, texturesByType, prefixedTextureCodes, overlayPrefix);

            TesselationMetaData meta = new()
            {
                QuantityElements = rcshape.QuantityElements,
                SelectiveElements = GetShapeSelectiveElements(variants, rcshape),
                IgnoreElements = GetShapeIgnoreElements(variants, rcshape),
                TexSource = stexSource,
                TypeForLogging = "ShapeTexturesFromAttributes block behavior"
            };

            clientApi.Tesselator.TesselateShape(meta, shape, out mesh);

            if (overrideTexturesource == null)
            {
                cMeshes[key] = mesh;
            }
        }
        return mesh;
    }

    /// <summary>
    /// Used to generate mesh for block stored in a visual container that uses <see cref="IContainedMeshSource"/> to generate mesh for each contained item.
    /// </summary>
    /// <remarks>
    /// It is recommended to cache the returned mesh in the ObjectCacheUtil for efficiency<br/>
    /// When <paramref name="overrideShape"/> is provided, <see cref="GetMeshCacheKey"/> by default will cache only the first generated mesh for the same variants.
    /// </remarks>
    /// <param name="slot">Container slot that holds current block</param>
    /// <param name="targetAtlas">Texture atlas</param>
    /// <param name="atBlockPos">Container position. Use it to get container block</param>
    /// <param name="overrideShape">Optional custom shape to use instead of the default shape.</param>
    /// <returns>Mesh for block stored in a visual container</returns>
    public virtual MeshData GenContainedMesh(ItemSlot slot, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos, CompositeShape? overrideShape = null)
    {
        MeshData mesh = RenderExtensions.GenEmptyMesh();

        Variants variants = Variants.FromStack(slot.Itemstack!);

        Shape? shape = GetShape(slot, null, variants, overrideShape, out CompositeShape? rcshape);

        if (shape == null || rcshape == null) return RenderExtensions.GetUnknownBlockModelData(clientApi);

        UniversalShapeTextureSource stexSource = new(clientApi, targetAtlas, shape, rcshape.Base.ToString());
        Dictionary<string, AssetLocation>? prefixedTextureCodes = null;
        string overlayPrefix = "";

        if (rcshape.Overlays is { Length: > 0 })
        {
            overlayPrefix = GetMeshCacheKey(slot);
            prefixedTextureCodes = ShapeOverlayHelper.AddOverlays(clientApi, overlayPrefix, variants, stexSource, shape, rcshape);
        }

        foreach ((string textureCode, CompositeTexture texture) in slot.Itemstack!.Block.Textures)
        {
            stexSource.textures[textureCode] = texture;
        }

        ShapeOverlayHelper.BakeVariantTextures(clientApi, stexSource, variants, texturesByType, prefixedTextureCodes, overlayPrefix);

        TesselationMetaData meta = new()
        {
            QuantityElements = rcshape.QuantityElements,
            SelectiveElements = GetShapeSelectiveElements(variants, rcshape),
            IgnoreElements = GetShapeIgnoreElements(variants, rcshape),
            TexSource = stexSource,
            TypeForLogging = "ShapeTexturesFromAttributes block behavior"
        };

        clientApi.Tesselator.TesselateShape(meta, shape, out mesh);
        return mesh;
    }

    public virtual Shape? GetShape(ItemSlot? slot, BlockPos? pos, Variants variants, CompositeShape? overrideShape, out CompositeShape? originalShape)
    {
        CompositeShape? ucshape = overrideShape;

        if (ucshape == null)
        {
            variants.FindByVariant(shapeByType, out ucshape);
            ucshape ??= block.Shape;
        }
        if (ucshape == null)
        {
            originalShape = ucshape;
            return null;
        }

        CompositeShape rcshape = variants.ReplacePlaceholders(ucshape.Clone());
        rcshape.Base = rcshape.Base.CopyWithPathPrefixAndAppendixOnce("shapes/", ".json");

        originalShape = rcshape;
        return Vintagestory.API.Common.Shape.TryGet(coreApi, rcshape.Base);
    }

    public virtual Shape? GetInventoryShape(ItemSlot slot, Variants variants, CompositeShape? overrideShape, out CompositeShape? originalShape)
    {
        CompositeShape? ucshape = overrideShape;

        if (ucshape == null)
            variants.FindByVariant(shapeInventoryByType, out ucshape);

        if (ucshape == null)
            variants.FindByVariant(shapeByType, out ucshape);

        ucshape ??= block.ShapeInventory;
        ucshape ??= block.Shape;

        if (ucshape == null)
        {
            originalShape = ucshape;
            return null;
        }

        CompositeShape rcshape = variants.ReplacePlaceholders(ucshape.Clone());
        rcshape.Base = rcshape.Base.CopyWithPathPrefixAndAppendixOnce("shapes/", ".json");

        originalShape = rcshape;
        return Vintagestory.API.Common.Shape.TryGet(coreApi, rcshape.Base);
    }

    public virtual string[] GetShapeIgnoreElements(Variants variants, CompositeShape cshape)
    {
        if (ShapeIgnoreElementsByType is { Count: > 0 })
        {
            if (variants.FindByVariant(ShapeIgnoreElementsByType, out string[] elements) && elements != null)
            {
                return variants.ReplacePlaceholders(elements).Append(cshape.IgnoreElements);
            }
        }

        if (ShapeIgnoreElementsCombineByType is { Count: > 0 })
        {
            List<string> result = cshape.IgnoreElements is null ? new() : new(cshape.IgnoreElements);
            foreach (string[] subset in variants.FindAllByVariant(ShapeIgnoreElementsCombineByType))
            {
                result.AddRange(variants.ReplacePlaceholders(subset));
            }
            return [.. result];
        }

        return cshape.IgnoreElements;
    }

    public virtual string[] GetShapeSelectiveElements(Variants variants, CompositeShape cshape)
    {
        if (ShapeSelectiveElementsByType is { Count: > 0 })
        {
            if (variants.FindByVariant(ShapeSelectiveElementsByType, out string[] elements) && elements != null)
            {
                return variants.ReplacePlaceholders(elements).Append(cshape.SelectiveElements);
            }
        }

        if (ShapeSelectiveElementsCombineByType is { Count: > 0 })
        {
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
        Dictionary<string, MultiTextureMeshRef> meshRefs = ObjectCacheUtil.GetOrCreate(clientApi, "AttributeRenderingLibrary_BehaviorShapeTexturesFromAttributes_MeshRefs", () => new Dictionary<string, MultiTextureMeshRef>());

        string key = GetMeshCacheKey(renderinfo.InSlot);

        if (!meshRefs.TryGetValue(key, out MultiTextureMeshRef meshref))
        {
            MeshData mesh = GenGuiMesh(renderinfo.InSlot);
            meshref = clientApi.Render.UploadMultiTextureMesh(mesh);
            meshRefs[key] = meshref;
        }

        renderinfo.ModelRef = meshref;
        renderinfo.NormalShaded = true;

        base.OnBeforeRender(clientApi, itemstack, target, ref renderinfo);
    }

    public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref float dropQuantityMultiplier, ref EnumHandling handling)
    {
        if (world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BlockEntityBehaviorShapeTexturesFromAttributes>() is { } beBehavior)
        {
            if (beBehavior.Variants.FindByVariant(DropsByType, out BlockDropItemStack[] unresolvedDrops)
                && unresolvedDrops != null
                && unresolvedDrops.Length > 0)
            {
                List<ItemStack> resolvedDrops = new(unresolvedDrops.Length);
                for (int i = 0; i < unresolvedDrops.Length; i++)
                {
                    BlockDropItemStack dstack = beBehavior.Variants.ReplacePlaceholders(unresolvedDrops[i].Clone());
                    if (!dstack.Resolve(world, "AttributeRenderingLibrary.BlockShapeTexturesFromAttributes", dstack.Code))
                    {
                        break;
                    }
                    ItemStack stack = dstack.ToRandomItemstackForPlayer(byPlayer, world, dropQuantityMultiplier);
                    if (stack != null)
                    {
                        resolvedDrops.Add(stack);
                        if (dstack.LastDrop)
                        {
                            break;
                        }
                    }
                }
                handling = EnumHandling.PreventSubsequent;
                return [.. resolvedDrops];
            }

            handling = EnumHandling.Handled;
            return [OnPickBlock(world, pos, ref handling)];
        }

        handling = EnumHandling.PassThrough;
        return null;
    }

    public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
    {
        if (world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BlockEntityBehaviorShapeTexturesFromAttributes>() is { } beBehavior)
        {
            handling = EnumHandling.PreventSubsequent;
            ItemStack stack = new(block);
            beBehavior.Variants.ToStack(stack);
            return stack;
        }

        handling = EnumHandling.PassThrough;
        return null;
    }

    public override void GetDecal(IWorldAccessor world, BlockPos pos, ITexPositionSource decalTexSource, ref MeshData decalModelData, ref MeshData blockModelData, ref EnumHandling handled)
    {
        if (world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BlockEntityBehaviorShapeTexturesFromAttributes>() is not { } beBehavior)
        {
            handled = EnumHandling.PassThrough;
            return;
        }

        Vec3f rotationRad = GetRotation(world, pos);
        float[] mat = Matrixf.Create().Translate(0.5f, 0.5f, 0.5f).Rotate(rotationRad).Translate(-0.5f, -0.5f, -0.5f).Values;
        MeshData decalMesh = GetOrCreateMesh(beBehavior.Variants, overrideTexturesource: decalTexSource).Clone().MatrixTransform(mat);
        MeshData blockMesh = GetOrCreateMesh(beBehavior.Variants).Clone().MatrixTransform(mat);
        decalModelData = decalMesh;
        blockModelData = blockMesh;
        return;
    }

    public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos, ref EnumHandling handled)
    {
        if (CollisionBoxesByType is not { Count: > 0 })
        {
            return base.GetCollisionBoxes(blockAccessor, pos, ref handled);
        }

        if (blockAccessor.GetBlockEntity(pos)?.GetBehavior<BlockEntityBehaviorShapeTexturesFromAttributes>() is { } beBehavior
            && beBehavior.Variants != null
            && beBehavior.Variants.FindByVariant(CollisionBoxesByType, out Cuboidf[] cuboids)
            && cuboids != null
            && cuboids.Length > 0)
        {
            handled = EnumHandling.PreventSubsequent;
            return cuboids;
        }
        return base.GetCollisionBoxes(blockAccessor, pos, ref handled);
    }

    public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos, ref EnumHandling handled)
    {
        if (SelectionBoxesByType is not { Count: > 0 })
        {
            return base.GetSelectionBoxes(blockAccessor, pos, ref handled);
        }

        if (blockAccessor.GetBlockEntity(pos)?.GetBehavior<BlockEntityBehaviorShapeTexturesFromAttributes>() is { } beBehavior
            && beBehavior.Variants != null
            && beBehavior.Variants.FindByVariant(SelectionBoxesByType, out Cuboidf[] cuboids)
            && cuboids != null
            && cuboids.Length > 0)
        {
            handled = EnumHandling.PreventSubsequent;
            return cuboids;
        }
        return base.GetSelectionBoxes(blockAccessor, pos, ref handled);
    }

    public override float GetLiquidBarrierHeightOnSide(BlockFacing face, BlockPos pos, ref EnumHandling handled)
    {
        float[] lbos = GetLiquidBarrierOnSides(pos);
        if (lbos != null)
        {
            float[] liquidBarrierHeightonSide = new float[6];

            for (int i = 0; i < 6; i++) liquidBarrierHeightonSide[i] = block.SideIsSolid(pos, BlockFacing.ALLFACES[i].Index) ? 1f : 0f;

            for (int i = 0; lbos != null && i < lbos.Length; i++) liquidBarrierHeightonSide[i] = lbos[i];

            handled = EnumHandling.PreventSubsequent;
            return liquidBarrierHeightonSide[face.Index];
        }
        return base.GetLiquidBarrierHeightOnSide(face, pos, ref handled);
    }

    public virtual float[] GetLiquidBarrierOnSides(BlockPos pos)
    {
        if (LiquidBarrierOnSidesByType is not { Count: > 0 })
        {
            return null;
        }

        if (coreApi.World.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BlockEntityBehaviorShapeTexturesFromAttributes>() is { } beBehavior
            && beBehavior.Variants != null
            && beBehavior.Variants.FindByVariant(LiquidBarrierOnSidesByType, out float[] liquidBarrierOnSides))
        {
            return liquidBarrierOnSides;
        }
        return null;
    }

    public override void GetHeldItemName(StringBuilder sb, ItemStack itemStack)
    {
        if (!itemStack.FindByVariant(NameByType, out List<object> langKeys, out Variants variants) || langKeys is not { Count: > 0 })
        {
            return;
        }

        sb.Clear().Append(variants.GetName(langKeys));
    }

    public override void GetPlacedBlockName(StringBuilder sb, IWorldAccessor world, BlockPos pos)
    {
        if (NameByType is not { Count: > 0 })
        {
            return;
        }

        if (world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BlockEntityBehaviorShapeTexturesFromAttributes>() is not { } beBehavior)
        {
            return;
        }

        if (!beBehavior.Variants.FindByVariant(NameByType, out List<object> langKeys) || langKeys is not { Count: > 0 })
        {
            return;
        }

        sb.Clear().Append(beBehavior.Variants.GetName(langKeys));
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        if (!inSlot.Itemstack.FindByVariant(DescriptionByType, out List<object> langKeys, out Variants variants) || langKeys is not { Count: > 0 })
        {
            return;
        }
        variants.GetDescription(dsc, langKeys);
        variants.GetDebugDescription(dsc, withDebugInfo);
    }

    public override EnumItemStorageFlags GetStorageFlags(ItemStack itemstack, ref EnumHandling handling)
    {
        if (!itemstack.FindByVariant(StorageFlagsByType, out EnumItemStorageFlags storageFlags))
        {
            return base.GetStorageFlags(itemstack, ref handling);
        }
        handling = EnumHandling.PreventSubsequent;
        return storageFlags;
    }

    public override int GetMaxDurability(ItemStack itemstack, int durability, ref EnumHandling handling)
    {
        if (!itemstack.FindByVariant(DurabilityByType, out int maxDurability))
        {
            return base.GetMaxDurability(itemstack, durability, ref handling);
        }
        handling = EnumHandling.PreventSubsequent;
        return maxDurability;
    }

    public override float GetAttackPower(ItemStack itemstack, float attackPower, ref EnumHandling handling)
    {
        if (!itemstack.FindByVariant(AttackPowerByType, out float newAttackPower))
        {
            return base.GetAttackPower(itemstack, attackPower, ref handling);
        }
        handling = EnumHandling.PreventSubsequent;
        return newAttackPower;
    }

    public override float GetAttackRange(ItemStack itemstack, float attackRange, ref EnumHandling handling)
    {
        if (!itemstack.FindByVariant(AttackRangeByType, out float newAttackRange))
        {
            return base.GetAttackRange(itemstack, attackRange, ref handling);
        }
        handling = EnumHandling.PreventSubsequent;
        return newAttackRange;
    }

    public override Dictionary<EnumBlockMaterial, float> GetMiningSpeeds(ItemSlot slot, ref EnumHandling handling)
    {
        if (!slot.Itemstack.FindByVariant(MiningSpeedByType, out Dictionary<EnumBlockMaterial, float> miningSpeed))
        {
            return base.GetMiningSpeeds(slot, ref handling);
        }
        handling = EnumHandling.PreventSubsequent;
        return miningSpeed;
    }

    public override string GetHeldReadyAnimation(ItemSlot slot, Entity forEntity, EnumHand hand, ref EnumHandling handling)
    {
        Dictionary<string, string> animCodesByType = (hand == EnumHand.Left) ? HeldLeftReadyAnimationByType : HeldRightReadyAnimationByType;
        if (!slot.Itemstack.FindByVariant(animCodesByType, out string animCode))
        {
            return base.GetHeldReadyAnimation(slot, forEntity, hand, ref handling);
        }
        handling = EnumHandling.PreventSubsequent;
        return animCode;
    }

    public override string GetHeldTpIdleAnimation(ItemSlot slot, Entity forEntity, EnumHand hand, ref EnumHandling handling)
    {
        Dictionary<string, string> animCodesByType = (hand == EnumHand.Left) ? HeldLeftTpIdleAnimationByType : HeldRightTpIdleAnimationByType;
        if (!slot.Itemstack.FindByVariant(animCodesByType, out string animCode))
        {
            return base.GetHeldTpIdleAnimation(slot, forEntity, hand, ref handling);
        }
        handling = EnumHandling.PreventSubsequent;
        return animCode;
    }

    public override string GetHeldTpUseAnimation(ItemSlot slot, Entity forEntity, ref EnumHandling handling)
    {
        if (!slot.Itemstack.FindByVariant(HeldTpUseAnimationByType, out string animCode))
        {
            return base.GetHeldTpUseAnimation(slot, forEntity, ref handling);
        }
        handling = EnumHandling.PreventSubsequent;
        return animCode;
    }

    public override string GetHeldTpHitAnimation(ItemSlot slot, Entity byEntity, ref EnumHandling handling)
    {
        if (!slot.Itemstack.FindByVariant(HeldTpHitAnimationByType, out string animCode))
        {
            return base.GetHeldTpHitAnimation(slot, byEntity, ref handling);
        }
        handling = EnumHandling.PreventSubsequent;
        return animCode;
    }

    /// <summary>
    /// Rotation (in radians) for the block at the given position
    /// </summary>
    /// <param name="world"></param>
    /// <param name="pos"></param>
    /// <returns></returns>
    public virtual Vec3f GetRotation(IWorldAccessor world, BlockPos pos)
    {
        BlockEntity blockEntity = world.BlockAccessor.GetBlockEntity(pos);

        if (blockEntity == null)
        {
            return Vec3f.Zero;
        }

        if (blockEntity?.GetBehavior<BEBehaviorRotatablePlaceable>() is { } rotatablePlaceable)
        {
            return new Vec3f(0, rotatablePlaceable.MeshAngleRad, 0);
        }

        if (blockEntity?.GetBehavior<BlockEntityBehaviorShapeTexturesFromAttributes>() is { } beBehavior)
        {
            beBehavior.Variants.FindByVariant(shapeByType, out CompositeShape shapeForRotation);
            shapeForRotation ??= block.Shape;

            return new Vec3f
            {
                X = (shapeForRotation?.rotateX ?? 0) * GameMath.DEG2RAD,
                Y = (shapeForRotation?.rotateY ?? 0) * GameMath.DEG2RAD,
                Z = (shapeForRotation?.rotateZ ?? 0) * GameMath.DEG2RAD
            };
        }

        return Vec3f.Zero;
    }
    #region IContainedMeshSource
    public virtual MeshData GenMesh(ItemSlot slot, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos) => GenContainedMesh(slot, targetAtlas, atBlockPos);

    public virtual string GetMeshCacheKey(ItemSlot slot) => $"{slot.Itemstack.Collectible.Code}-{Variants.FromStack(slot.Itemstack)}";
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
        foreach ((string textureCode, CompositeTexture texture) in stack.Block.Textures)
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
                if (!coreApi.Assets.Exists(ctex.Base.CopyWithPathPrefixAndAppendixOnce("textures/", ".png")))
                {
                    ctex.Base.Path = "unknown";
                    ctex.Base.Domain = "game";
                }
                ctex.Bake(coreApi.Assets);
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
            return rcshape.RemoveNonExistingOverlays(coreApi)!;
        }

        if (stack.FindByVariant(AttachedShapeBySlotCodeByType, out System.Collections.Generic.OrderedDictionary<string, CompositeShape> attachedShapeBySlotCode) && attachedShapeBySlotCode is { Count: > 0 })
        {
            foreach ((string _slotCode, CompositeShape ucshape) in attachedShapeBySlotCode)
            {
                if (WildcardUtil.Match(_slotCode, slotCode))
                {
                    CompositeShape rcshape = variants.ReplacePlaceholders(ucshape.Clone());
                    return rcshape.RemoveNonExistingOverlays(coreApi)!;
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

        _ = GetShape(new DummySlot(stack), null, variants, null, out CompositeShape? compositeShape);
        return compositeShape.RemoveNonExistingOverlays(coreApi)!;
    }

    public virtual string GetCategoryCode(ItemStack stack)
    {
        return !stack.FindByVariant(CategoryCodeByType, out string categoryCode) ? (iattr?.GetCategoryCode(stack)) : categoryCode;
    }

    public virtual string[] GetDisableElements(ItemStack stack)
    {
        return !stack.FindByVariant(DisableElementsByType, out string[] elems) ? (iattr?.GetDisableElements(stack)) : elems;
    }

    public virtual string[] GetKeepElements(ItemStack stack)
    {
        return !stack.FindByVariant(KeepElementsByType, out string[] elems) ? (iattr?.GetKeepElements(stack)) : elems;
    }

    public virtual string GetTexturePrefixCode(ItemStack stack) => GetMeshCacheKey(new DummySlot(stack));

    public virtual bool IsAttachable(Entity toEntity, ItemStack itemStack) => true;

    public virtual int RequiresBehindSlots { get; set; }
    #endregion
    #region IBlockPropertiesSupplier
    public virtual TagSet GetTags(ItemStack stack, ref EnumHandling handling)
    {
        if (stack.FindByVariant(TagsByType, out TagSet result))
        {
            handling = EnumHandling.PreventSubsequent;
        }
        return result;
    }

    public virtual byte[] GetLightHsv(IBlockAccessor blockAccessor, BlockPos pos, ItemStack stack, ref EnumHandling handling)
    {
        byte[] result;
        if (pos != null && blockAccessor.GetBlockEntity(pos)?.GetBehavior<BlockEntityBehaviorShapeTexturesFromAttributes>() is { } beBehavior)
        {
            if (beBehavior.Variants.FindByVariant(LightHsvByType, out result))
            {
                handling = EnumHandling.PreventSubsequent;
                return result;
            }
        }
        else
        {
            if (stack.FindByVariant(LightHsvByType, out result))
            {
                handling = EnumHandling.PreventSubsequent;
                return result;
            }
        }
        return result;
    }

    public virtual int GetRequiredMiningTier(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
    {
        int result = 0;

        if (RequiredMiningTierByType is not { Count: > 0 })
        {
            return result;
        }
        if (world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BlockEntityBehaviorShapeTexturesFromAttributes>() is not { } beBehavior)
        {
            return result;
        }
        if (beBehavior.Variants.FindByVariant(RequiredMiningTierByType, out result))
        {
            handling = EnumHandling.PreventSubsequent;
        }
        return result;
    }

    public virtual EnumBlockMaterial GetBlockMaterial(IBlockAccessor blockAccessor, BlockPos pos, ItemStack stack, ref EnumHandling handling)
    {
        EnumBlockMaterial result;
        if (pos != null && blockAccessor.GetBlockEntity(pos)?.GetBehavior<BlockEntityBehaviorShapeTexturesFromAttributes>() is { } beBehavior)
        {
            if (beBehavior.Variants.FindByVariant(BlockMaterialByType, out result))
            {
                handling = EnumHandling.PreventSubsequent;
                return result;
            }
        }
        else
        {
            if (stack.FindByVariant(BlockMaterialByType, out result))
            {
                handling = EnumHandling.PreventSubsequent;
                return result;
            }
        }
        return result;
    }

    public virtual EnumItemDamageSource[] GetDamagedBy(ItemSlot slot, ref EnumHandling handling)
    {
        if (slot.Itemstack.FindByVariant(DamagedByByType, out EnumItemDamageSource[] result))
        {
            handling = EnumHandling.PreventSubsequent;
        }
        return result;
    }

    public virtual EnumTool? GetTool(ItemSlot slot, ref EnumHandling handling)
    {
        if (slot.Itemstack.FindByVariant(ToolByType, out EnumTool? result))
        {
            handling = EnumHandling.PreventSubsequent;
        }
        return result;
    }

    public virtual int GetToolTier(ItemSlot slot, ref EnumHandling handling)
    {
        if (slot.Itemstack.FindByVariant(ToolTierByType, out int result))
        {
            handling = EnumHandling.PreventSubsequent;
        }
        return result;
    }

    public virtual CombustibleProperties GetCombustibleProperties(IWorldAccessor world, ItemStack stack, BlockPos pos, ref EnumHandling handling)
    {
        Variants variants;

        CombustibleProperties result;
        if (pos != null && world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BlockEntityBehaviorShapeTexturesFromAttributes>() is { } beBehavior)
        {
            if (!beBehavior.Variants.FindByVariant(CombustiblePropsByType, out result) || result == null)
            {
                return result;
            }
            variants = beBehavior.Variants;
        }
        else
        {
            if (!stack.FindByVariant(CombustiblePropsByType, out result, out variants) || result == null)
            {
                return result;
            }
        }

        if (result.SmeltedStack == null)
        {
            handling = EnumHandling.PreventSubsequent;
            return result;
        }

        CombustibleProperties clonedProps = result.Clone();
        JsonItemStack resultStack = variants.ReplacePlaceholders(clonedProps.SmeltedStack);
        if (!resultStack.Resolve(world, ""))
        {
            LoggerUtil.Warn(world.Api, this, $"Smelted stack with code '{resultStack.Code}' cannot be resolved for '{stack.Collectible.Code}'. Will skip it.");
            clonedProps.SmeltedStack = null;
        }
        handling = EnumHandling.PreventSubsequent;
        return clonedProps;
    }

    public virtual FoodNutritionProperties GetNutritionProperties(IWorldAccessor world, ItemStack stack, Entity forEntity, ref EnumHandling handling)
    {
        if (!stack.FindByVariant(NutritionPropsByType, out FoodNutritionProperties result, out Variants variants) || result == null)
        {
            return result;
        }

        if (result.EatenStack == null)
        {
            handling = EnumHandling.PreventSubsequent;
            return result;
        }

        FoodNutritionProperties clonedProps = result.Clone();
        JsonItemStack resultStack = variants.ReplacePlaceholders(clonedProps.EatenStack);
        if (!resultStack.Resolve(world, ""))
        {
            LoggerUtil.Warn(world.Api, this, $"Eaten stack with code '{resultStack.Code}' cannot be resolved for '{stack.Collectible.Code}'. Will skip it.");
            clonedProps.EatenStack = null;
        }
        handling = EnumHandling.PreventSubsequent;
        return clonedProps;
    }

    public virtual GrindingProperties GetGrindingProperties(IWorldAccessor world, ItemStack stack, ref EnumHandling handling)
    {
        if (!stack.FindByVariant(GrindingPropsByType, out GrindingProperties result, out Variants variants) || result == null)
        {
            return result;
        }

        if (result.GroundStack == null)
        {
            handling = EnumHandling.PreventSubsequent;
            return result;
        }

        GrindingProperties clonedProps = result.Clone();
        JsonItemStack resultStack = variants.ReplacePlaceholders(clonedProps.GroundStack);
        if (!resultStack.Resolve(world, ""))
        {
            LoggerUtil.Warn(world.Api, this, $"Ground stack with code '{resultStack.Code}' cannot be resolved for '{stack.Collectible.Code}'. Will skip it.");
            clonedProps.GroundStack = null;
        }
        handling = EnumHandling.PreventSubsequent;
        return clonedProps;
    }

    public virtual CrushingProperties GetCrushingProperties(IWorldAccessor world, ItemStack stack, ref EnumHandling handling)
    {
        if (!stack.FindByVariant(CrushingPropsByType, out CrushingProperties result, out Variants variants) || result == null)
        {
            return result;
        }

        if (result.CrushedStack == null)
        {
            handling = EnumHandling.PreventSubsequent;
            return result;
        }

        CrushingProperties clonedProps = result.Clone();
        JsonItemStack resultStack = variants.ReplacePlaceholders(clonedProps.CrushedStack);
        if (!resultStack.Resolve(world, ""))
        {
            LoggerUtil.Warn(world.Api, this, $"Crushed stack with code '{resultStack.Code}' cannot be resolved for '{stack.Collectible.Code}'. Will skip it.");
            clonedProps.CrushedStack = null;
        }
        handling = EnumHandling.PreventSubsequent;
        return clonedProps;
    }

    public virtual TransitionableProperties[] GetTransitionableProperties(IWorldAccessor world, ItemStack stack, Entity forEntity, ref EnumHandling handling)
    {
        if (!stack.FindByVariant(TransitionablePropsByType, out TransitionableProperties[] result, out Variants variants) || result == null)
        {
            return result;
        }

        List<TransitionableProperties> allResolvedProps = [];

        for (int i = 0; i < result.Length; i++)
        {
            if (result[i].TransitionedStack == null)
            {
                allResolvedProps.Add(result[i]);
                continue;
            }

            TransitionableProperties clonedProps = result[i].Clone();
            JsonItemStack resultStack = variants.ReplacePlaceholders(clonedProps.TransitionedStack);
            if (!resultStack.Resolve(world, ""))
            {
                LoggerUtil.Warn(world.Api, this, $"Transitioned stack with code '{resultStack.Code}' cannot be resolved for '{stack.Collectible.Code}'. Will skip it.");
                continue;
            }

            allResolvedProps.Add(clonedProps);
        }

        handling = EnumHandling.PreventSubsequent;
        return [.. allResolvedProps];
    }

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
            if (!clonedProps.LiquidStack.Resolve(coreApi.World, ""))
            {
                LoggerUtil.Warn(coreApi, this, $"Liquid stack with code '{clonedProps.LiquidStack.Code}' cannot be resolved for '{stack.Collectible.Code}'. Will skip it.");
                clonedProps.LiquidStack = null;
            }
        }
        if (clonedProps.PressedStack != null)
        {
            clonedProps.PressedStack = variants.ReplacePlaceholders(clonedProps.PressedStack);
            if (!clonedProps.PressedStack.Resolve(coreApi.World, ""))
            {
                LoggerUtil.Warn(coreApi, this, $"Pressed stack with code '{clonedProps.PressedStack.Code}' cannot be resolved for '{stack.Collectible.Code}'. Will skip it.");
                clonedProps.PressedStack = null;
            }
        }
        if (clonedProps.ReturnStack != null)
        {
            clonedProps.ReturnStack = variants.ReplacePlaceholders(clonedProps.ReturnStack);
            if (!clonedProps.ReturnStack.Resolve(coreApi.World, ""))
            {
                LoggerUtil.Warn(coreApi, this, $"Return stack with code '{clonedProps.ReturnStack.Code}' cannot be resolved for '{stack.Collectible.Code}'. Will skip it.");
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
        if (!resultStack.Resolve(coreApi.World, ""))
        {
            LoggerUtil.Warn(coreApi, this, $"Distilled stack with code '{resultStack.Code}' cannot be resolved for '{stack.Collectible.Code}'. Will skip it.");
            clonedProps.DistilledStack = null;
        }

        handling = EnumHandling.PreventSubsequent;
        return clonedProps;
    }
    #endregion
}