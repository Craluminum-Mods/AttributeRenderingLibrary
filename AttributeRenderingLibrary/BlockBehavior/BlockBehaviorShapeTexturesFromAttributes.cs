using Newtonsoft.Json.Linq;
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
    public Dictionary<string, TagSet> TagsByType { get; protected set; }
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
    #region Block properties
    public Dictionary<string, CompositeShape> shapeInventoryByType { get; protected set; }
    public Dictionary<string, Cuboidf[]> CollisionBoxesByType { get; protected set; }
    public Dictionary<string, Cuboidf[]> SelectionBoxesByType { get; protected set; }
    public Dictionary<string, BlockDropItemStack[]> DropsByType { get; protected set; }
    public Dictionary<string, int> RequiredMiningTierByType { get; protected set; }
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
    public IAttachableToEntity iattr;
    #endregion

    public ICoreClientAPI clientApi;
    public ICoreAPI coreApi;

    public override void OnLoaded(ICoreAPI api)
    {
        // blocks with this behavior cannot be chiseled
        block.Attributes ??= new JsonObject(new JObject());
        block.Attributes.Token["canChisel"] = JToken.FromObject(false);

        clientApi = api as ICoreClientAPI;
        coreApi = api;
        iattr = IAttachableToEntity.FromAttributes(collObj);
    }

    public override void OnUnloaded(ICoreAPI api)
    {
        Dictionary<string, MultiTextureMeshRef> meshRefs = ObjectCacheUtil.TryGet<Dictionary<string, MultiTextureMeshRef>>(api, "AttributeRenderingLibrary_BehaviorShapeTexturesFromAttributes_MeshRefs");
        meshRefs?.Foreach(meshRef => meshRef.Value?.Dispose());
        ObjectCacheUtil.Delete(api, "AttributeRenderingLibrary_BehaviorShapeTexturesFromAttributes_MeshRefs");

        Dictionary<string, MeshData> meshes = ObjectCacheUtil.TryGet<Dictionary<string, MeshData>>(api, "AttributeRenderingLibrary_BehaviorShapeTexturesFromAttributes_Meshes");
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
        if (properties == null) return;

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
        StorageFlagsByType = properties["storageFlags"].AsObject<Dictionary<string, EnumItemStorageFlags>>();
        DurabilityByType = properties["durability"].AsObject<Dictionary<string, int>>();
        AttackPowerByType = properties["attackPower"].AsObject<Dictionary<string, float>>();
        AttackRangeByType = properties["attackRange"].AsObject<Dictionary<string, float>>();
        MiningSpeedByType = properties["miningSpeed"].AsObject<Dictionary<string, Dictionary<EnumBlockMaterial, float>>>();
        DamagedByByType = properties["damagedBy"].AsObject<Dictionary<string, EnumItemDamageSource[]>>();
        ToolByType = properties["tool"].AsObject<Dictionary<string, EnumTool?>>();
        ToolTierByType = properties["toolTier"].AsObject<Dictionary<string, int>>();
        CombustiblePropsType = properties["combustibleProps"].AsObject<Dictionary<string, CombustibleProperties>>();
        NutritionPropsType = properties["nutritionProps"].AsObject<Dictionary<string, FoodNutritionProperties>>();
        GrindingPropsType = properties["grindingProps"].AsObject<Dictionary<string, GrindingProperties>>();
        CrushingPropsType = properties["crushingProps"].AsObject<Dictionary<string, CrushingProperties>>();
        TransitionablePropsType = properties["TransitionableProps"].AsObject<Dictionary<string, TransitionableProperties[]>>();

        HeldLeftReadyAnimationByType = properties["heldLeftReadyAnimation"].AsObject<Dictionary<string, string>>();
        HeldRightReadyAnimationByType = properties["heldRightReadyAnimation"].AsObject<Dictionary<string, string>>();

        HeldLeftTpIdleAnimationByType = properties["heldLeftTpIdleAnimation"].AsObject<Dictionary<string, string>>();
        HeldRightTpIdleAnimationByType = properties["heldRightTpIdleAnimation"].AsObject<Dictionary<string, string>>();

        HeldTpUseAnimationByType = properties["heldTpUseAnimation"].AsObject<Dictionary<string, string>>();
        HeldTpHitAnimationByType = properties["heldTpHitAnimation"].AsObject<Dictionary<string, string>>();

        AttachedShapeBySlotCodeByType = properties["STFA_attachableToEntity"]?["attachedShapeBySlotCode"].AsObject<Dictionary<string, System.Collections.Generic.OrderedDictionary<string, CompositeShape>>>();
        CategoryCodeByType = properties["STFA_attachableToEntity"]?["categoryCode"].AsObject<Dictionary<string, string>>();
        DisableElementsByType = properties["STFA_attachableToEntity"]?["disableElements"].AsObject<Dictionary<string, string[]>>();
        KeepElementsByType = properties["STFA_attachableToEntity"]?["keepElements"].AsObject<Dictionary<string, string[]>>();

        shapeInventoryByType = properties["shapeInventory"].AsObject<Dictionary<string, CompositeShape>>();
        DropsByType = properties["drops"].AsObject<Dictionary<string, BlockDropItemStack[]>>();
        RequiredMiningTierByType = properties["requiredMiningTier"].AsObject<Dictionary<string, int>>();
        LoadAndResolveCollisionAndSelectionBoxes(properties);
    }

    public virtual void LoadTags(JsonObject properties)
    {
        var unresolvedTags = properties["tags"].AsObject<Dictionary<string, List<string>>>();
        if (unresolvedTags == null) return;

        TagsByType = [];

        foreach ((string type, List<string> tags) in unresolvedTags)
        {
            TagRegistryError tagRegistryError = Core.Api.CollectibleTagRegistry.TryCreateTagSetAndLogIssues(out TagSet resolvedTags, tags);
            TagsByType.Add(type, resolvedTags);
        }
    }

    public virtual void LoadAndResolveCollisionAndSelectionBoxes(JsonObject properties)
    {
        Dictionary<string, RotatableCube[]> rawCollisionsAndSelections = properties["collisionSelectionBoxes"]?.AsObject<Dictionary<string, RotatableCube[]>>();
        Dictionary<string, RotatableCube[]> rawCollisions = properties["collisionBoxes"]?.AsObject<Dictionary<string, RotatableCube[]>>();
        Dictionary<string, RotatableCube[]> rawSelections = properties["selectionBoxes"]?.AsObject<Dictionary<string, RotatableCube[]>>();

        if (rawCollisionsAndSelections?.Count > 0)
        {
            CollisionBoxesByType = rawCollisionsAndSelections?.ToDictionary(x => x.Key, x => x.Value.ToCuboidf());
            SelectionBoxesByType = rawCollisionsAndSelections?.ToDictionary(x => x.Key, x => x.Value.ToCuboidf());
        }
        else
        {
            CollisionBoxesByType = rawCollisions?.ToDictionary(x => x.Key, x => x.Value.ToCuboidf());
            SelectionBoxesByType = rawSelections?.ToDictionary(x => x.Key, x => x.Value.ToCuboidf());
        }
    }

    public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack, ref EnumHandling handling)
    {
        if (world.BlockAccessor.GetBlockEntity(blockSel.Position)?.GetBehavior<BlockEntityBehaviorShapeTexturesFromAttributes>() is BlockEntityBehaviorShapeTexturesFromAttributes beBehavior)
        {
            beBehavior.OnBlockPlaced(byItemStack);
            handling = EnumHandling.Handled;
        }
        return true;
    }

    public virtual MeshData GenGuiMesh(ItemSlot slot)
    {
        return GenGuiMesh(slot, overrideShape: null);
    }

    public virtual MeshData GenGuiMesh(ItemSlot slot, CompositeShape overrideShape)
    {
        MeshData mesh = RenderExtensions.GenEmptyMesh();

        Variants variants = Variants.FromStack(slot.Itemstack);

        CompositeShape ucshape = overrideShape;
        if (ucshape == null)
            variants.FindByVariant(shapeInventoryByType, out ucshape);

        if (ucshape == null)
            variants.FindByVariant(shapeByType, out ucshape);

        ucshape ??= block.ShapeInventory;
        ucshape ??= block.Shape;

        if (ucshape == null) return mesh;

        CompositeShape rcshape = variants.ReplacePlaceholders(ucshape.Clone());
        rcshape.Base = rcshape.Base.CopyWithPathPrefixAndAppendixOnce("shapes/", ".json");

        Shape shape = clientApi.Assets.TryGet(rcshape.Base)?.ToObject<Shape>();
        if (shape == null) return mesh;

        UniversalShapeTextureSource stexSource = new UniversalShapeTextureSource(clientApi, clientApi.BlockTextureAtlas, shape, rcshape.Base.ToString());
        Dictionary<string, AssetLocation> prefixedTextureCodes = null;
        string overlayPrefix = "";

        if (rcshape.Overlays != null && rcshape.Overlays.Length > 0)
        {
            overlayPrefix = GetMeshCacheKey(slot);
            prefixedTextureCodes = ShapeOverlayHelper.AddOverlays(clientApi, overlayPrefix, variants, stexSource, shape, rcshape);
        }

        foreach ((string textureCode, CompositeTexture texture) in slot.Itemstack.Block.Textures)
        {
            stexSource.textures[textureCode] = texture;
        }

        ShapeOverlayHelper.BakeVariantTextures(clientApi, stexSource, variants, texturesByType, prefixedTextureCodes, overlayPrefix);

        TesselationMetaData meta = new TesselationMetaData
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

    public virtual MeshData GetOrCreateMesh(Variants variants, ITexPositionSource overrideTexturesource = null)
    {
        return GetOrCreateMesh(variants: variants, overrideShape: null, atBlockPos: null, extraCacheKey: "", overrideTexturesource: overrideTexturesource);
    }

    public virtual MeshData GetOrCreateMesh(Variants variants, CompositeShape overrideShape, BlockPos atBlockPos, string extraCacheKey, ITexPositionSource overrideTexturesource = null)
    {
        Dictionary<string, MeshData> cMeshes = ObjectCacheUtil.GetOrCreate(clientApi, "AttributeRenderingLibrary_BehaviorShapeTexturesFromAttributes_Meshes", () => new Dictionary<string, MeshData>());

        string key = $"{block.Code}-{variants}-{extraCacheKey}";
        if (overrideTexturesource != null || !cMeshes.TryGetValue(key, out MeshData mesh))
        {
            mesh = RenderExtensions.GenEmptyMesh();

            CompositeShape ucshape = overrideShape;
            if (ucshape == null)
            {
                variants.FindByVariant(shapeByType, out ucshape);
                ucshape ??= block.Shape;
            }
            if (ucshape == null) return mesh;

            CompositeShape rcshape = variants.ReplacePlaceholders(ucshape.Clone());
            rcshape.Base = rcshape.Base.CopyWithPathPrefixAndAppendixOnce("shapes/", ".json");

            Shape shape = clientApi.Assets.TryGet(rcshape.Base)?.ToObject<Shape>();
            if (shape == null) return mesh;

            UniversalShapeTextureSource stexSource = new UniversalShapeTextureSource(clientApi, clientApi.BlockTextureAtlas, shape, rcshape.Base.ToString());
            Dictionary<string, AssetLocation> prefixedTextureCodes = null;
            string overlayPrefix = "";

            if (rcshape.Overlays != null && rcshape.Overlays.Length > 0)
            {
                overlayPrefix = key;
                prefixedTextureCodes = ShapeOverlayHelper.AddOverlays(clientApi, overlayPrefix, variants, stexSource, shape, rcshape);
            }

            foreach ((string textureCode, CompositeTexture texture) in block.Textures)
            {
                stexSource.textures[textureCode] = texture;
            }

            ShapeOverlayHelper.BakeVariantTextures(clientApi, stexSource, variants, texturesByType, prefixedTextureCodes, overlayPrefix);

            TesselationMetaData meta = new TesselationMetaData
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

    public virtual string[] GetShapeIgnoreElements(Variants variants, CompositeShape cshape)
    {
        if (ShapeIgnoreElementsByType is { Count: > 0 })
        {
            if (variants.FindByVariant(ShapeIgnoreElementsByType, out string[] selectiveElements) && selectiveElements != null)
            {
                return cshape.IgnoreElements.Append(selectiveElements);
            }
        }
        
        if (ShapeIgnoreElementsCombineByType is { Count: > 0 })
        {
            List<string> result = cshape.IgnoreElements is null ? new() : new(cshape.IgnoreElements);
            foreach (var subset in variants.FindAllByVariant(ShapeIgnoreElementsCombineByType))
            {
                result.AddRange(subset);
            }
            return result.ToArray();
        }
        
        return cshape.IgnoreElements;
    }

    public virtual string[] GetShapeSelectiveElements(Variants variants, CompositeShape cshape)
    {
        if (ShapeSelectiveElementsByType is { Count: > 0 })
        {
            if (variants.FindByVariant(ShapeSelectiveElementsByType, out string[] selectiveElements) && selectiveElements != null)
            {
                return cshape.SelectiveElements.Append(selectiveElements);
            }
        }
        
        if (ShapeSelectiveElementsCombineByType is { Count: > 0 })
        {
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
        if (world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BlockEntityBehaviorShapeTexturesFromAttributes>() is BlockEntityBehaviorShapeTexturesFromAttributes beBehavior)
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
                return resolvedDrops.ToArray();
            }

            handling = EnumHandling.Handled;
            return [OnPickBlock(world, pos, ref handling)];
        }

        handling = EnumHandling.PassThrough;
        return null;
    }

    public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
    {
        if (world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BlockEntityBehaviorShapeTexturesFromAttributes>() is BlockEntityBehaviorShapeTexturesFromAttributes beBehavior)
        {
            handling = EnumHandling.PreventSubsequent;
            ItemStack stack = new ItemStack(block);
            beBehavior.Variants.ToStack(stack);
            return stack;
        }

        handling = EnumHandling.PassThrough;
        return null;
    }

    public override void GetDecal(IWorldAccessor world, BlockPos pos, ITexPositionSource decalTexSource, ref MeshData decalModelData, ref MeshData blockModelData, ref EnumHandling handled)
    {
        if (world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BlockEntityBehaviorShapeTexturesFromAttributes>() is not BlockEntityBehaviorShapeTexturesFromAttributes beBehavior)
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
        if (CollisionBoxesByType == null || CollisionBoxesByType.Count == 0)
        {
            return base.GetCollisionBoxes(blockAccessor, pos, ref handled);
        }

        if (blockAccessor.GetBlockEntity(pos)?.GetBehavior<BlockEntityBehaviorShapeTexturesFromAttributes>() is BlockEntityBehaviorShapeTexturesFromAttributes beBehavior
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
        if (SelectionBoxesByType == null || SelectionBoxesByType.Count == 0)
        {
            return base.GetSelectionBoxes(blockAccessor, pos, ref handled);
        }

        if (blockAccessor.GetBlockEntity(pos)?.GetBehavior<BlockEntityBehaviorShapeTexturesFromAttributes>() is BlockEntityBehaviorShapeTexturesFromAttributes beBehavior
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

    public override void GetHeldItemName(StringBuilder sb, ItemStack itemStack)
    {
        if (!itemStack.FindByVariant(NameByType, out List<object> langKeys, out Variants variants) || langKeys == null || langKeys.Count == 0)
        {
            return;
        }

        sb.Clear().Append(variants.GetName(langKeys));
    }

    public override void GetPlacedBlockName(StringBuilder sb, IWorldAccessor world, BlockPos pos)
    {
        if (NameByType == null || NameByType.Count == 0)
        {
            return;
        }

        if (world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BlockEntityBehaviorShapeTexturesFromAttributes>() is not BlockEntityBehaviorShapeTexturesFromAttributes beBehavior)
        {
            return;
        }

        if (!beBehavior.Variants.FindByVariant(NameByType, out List<object> langKeys) || langKeys == null || langKeys.Count == 0)
        {
            return;
        }

        sb.Clear().Append(beBehavior.Variants.GetName(langKeys));
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        if (!inSlot.Itemstack.FindByVariant(DescriptionByType, out List<object> langKeys, out Variants variants) || langKeys == null || langKeys.Count == 0)
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

    public override string GetHeldReadyAnimation(ItemSlot slot, Entity forEntity, EnumHand hand, ref EnumHandling bhHandling)
    {
        Dictionary<string, string> animCodesByType = (hand == EnumHand.Left) ? HeldLeftReadyAnimationByType : HeldRightReadyAnimationByType;
        if (!slot.Itemstack.FindByVariant(animCodesByType, out string animCode))
        {
            return base.GetHeldReadyAnimation(slot, forEntity, hand, ref bhHandling);
        }
        bhHandling = EnumHandling.PreventSubsequent;
        return animCode;
    }

    public override string GetHeldTpIdleAnimation(ItemSlot slot, Entity forEntity, EnumHand hand, ref EnumHandling bhHandling)
    {
        Dictionary<string, string> animCodesByType = (hand == EnumHand.Left) ? HeldLeftTpIdleAnimationByType : HeldRightTpIdleAnimationByType;
        if (!slot.Itemstack.FindByVariant(animCodesByType, out string animCode))
        {
            return base.GetHeldTpIdleAnimation(slot, forEntity, hand, ref bhHandling);
        }
        bhHandling = EnumHandling.PreventSubsequent;
        return animCode;
    }

    public override string GetHeldTpUseAnimation(ItemSlot slot, Entity forEntity, ref EnumHandling bhHandling)
    {
        if (!slot.Itemstack.FindByVariant(HeldTpUseAnimationByType, out string animCode))
        {
            return base.GetHeldTpUseAnimation(slot, forEntity, ref bhHandling);
        }
        bhHandling = EnumHandling.PreventSubsequent;
        return animCode;
    }

    public override string GetHeldTpHitAnimation(ItemSlot slot, Entity byEntity, ref EnumHandling bhHandling)
    {
        if (!slot.Itemstack.FindByVariant(HeldTpHitAnimationByType, out string animCode))
        {
            return base.GetHeldTpHitAnimation(slot, byEntity, ref bhHandling);
        }
        bhHandling = EnumHandling.PreventSubsequent;
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

        if (blockEntity?.GetBehavior<BEBehaviorRotatablePlaceable>() is BEBehaviorRotatablePlaceable rotatablePlaceable)
        {
            return new Vec3f(0, rotatablePlaceable.MeshAngleRad, 0);
        }

        if (blockEntity?.GetBehavior<BlockEntityBehaviorShapeTexturesFromAttributes>() is BlockEntityBehaviorShapeTexturesFromAttributes beBehavior)
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
    public virtual MeshData GenMesh(ItemSlot slot, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos)
    {
        return GenGuiMesh(slot);
    }

    public virtual string GetMeshCacheKey(ItemSlot slot)
    {
        return $"{slot.Itemstack.Collectible.Code}-{Variants.FromStack(slot.Itemstack)}";
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
        foreach ((string textureCode, CompositeTexture texture) in stack.Block.Textures)
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
                    if (coreApi.Assets.Exists(overlay.Base.Clone().CopyWithPathPrefixAndAppendixOnce("shapes/", ".json")))
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
    #region IBlockPropertiesSupplier
    public virtual TagSet GetTags(ItemStack stack, ref EnumHandling handling)
    {
        TagSet result = new();
        handling = EnumHandling.PassThrough;

        if (stack.FindByVariant(TagsByType, out result))
        {
            handling = EnumHandling.PreventSubsequent;
        }
        return result;
    }

    public virtual int GetRequiredMiningTier(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
    {
        int result = 0;
        handling = EnumHandling.PassThrough;

        if (RequiredMiningTierByType == null || RequiredMiningTierByType.Count == 0)
        {
            return result;
        }
        if (world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BlockEntityBehaviorShapeTexturesFromAttributes>() is not BlockEntityBehaviorShapeTexturesFromAttributes beBehavior)
        {
            return result;
        }
        if (beBehavior.Variants.FindByVariant(RequiredMiningTierByType, out result))
        {
            handling = EnumHandling.PreventSubsequent;
        }
        return result;
    }

    public virtual EnumItemDamageSource[] GetDamagedBy(ItemSlot slot, ref EnumHandling handling)
    {
        EnumItemDamageSource[] result = [];
        handling = EnumHandling.PassThrough;

        if (slot.Itemstack.FindByVariant(DamagedByByType, out result))
        {
            handling = EnumHandling.PreventSubsequent;
        }
        return result;
    }

    public virtual EnumTool? GetTool(ItemSlot slot, ref EnumHandling handling)
    {
        EnumTool? result = null;
        handling = EnumHandling.PassThrough;

        if (slot.Itemstack.FindByVariant(ToolByType, out result))
        {
            handling = EnumHandling.PreventSubsequent;
        }
        return result;
    }

    public virtual int GetToolTier(ItemSlot slot, ref EnumHandling handling)
    {
        int result = 0;
        handling = EnumHandling.PassThrough;

        if (slot.Itemstack.FindByVariant(ToolTierByType, out result))
        {
            handling = EnumHandling.PreventSubsequent;
        }
        return result;
    }

    public virtual CombustibleProperties GetCombustibleProperties(IWorldAccessor world, ItemStack stack, BlockPos pos, ref EnumHandling handling)
    {
        CombustibleProperties result = null;
        handling = EnumHandling.PassThrough;

        if (!stack.FindByVariant(CombustiblePropsType, out result, out Variants variants) || result == null)
        {
            return result;
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
        FoodNutritionProperties result = null;
        handling = EnumHandling.PassThrough;

        if (!stack.FindByVariant(NutritionPropsType, out result, out Variants variants) || result == null)
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
        GrindingProperties result = null;
        handling = EnumHandling.PassThrough;

        if (!stack.FindByVariant(GrindingPropsType, out result, out Variants variants) || result == null)
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
        CrushingProperties result = null;
        handling = EnumHandling.PassThrough;

        if (!stack.FindByVariant(CrushingPropsType, out result, out Variants variants) || result == null)
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

    public TransitionableProperties[] GetTransitionableProperties(IWorldAccessor world, ItemStack stack, Entity forEntity, ref EnumHandling handling)
    {
        TransitionableProperties[] result = null;
        handling = EnumHandling.PassThrough;

        if (!stack.FindByVariant(TransitionablePropsType, out result, out Variants variants) || result == null)
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
        return allResolvedProps.ToArray();

    }
    #endregion
}