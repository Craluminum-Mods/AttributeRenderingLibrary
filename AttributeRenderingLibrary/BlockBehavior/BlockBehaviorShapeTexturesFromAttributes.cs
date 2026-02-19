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

public class BlockBehaviorShapeTexturesFromAttributes(Block block) : StrongBlockBehavior(block), IBlockShapeTexturesFromAttributes, IContainedMeshSource, IAttachableToEntity
{
    public Dictionary<string, CompositeShape> shapeByType { get; protected set; }
    public Dictionary<string, CompositeShape> shapeInventoryByType { get; protected set; }
    public Dictionary<string, Dictionary<string, CompositeTexture>> texturesByType { get; protected set; }

    public Dictionary<string, List<object>> NameByType { get; protected set; }
    public Dictionary<string, List<object>> DescriptionByType { get; protected set; }
    public Dictionary<string, Cuboidf[]> CollisionBoxesByType { get; protected set; }
    public Dictionary<string, Cuboidf[]> SelectionBoxesByType { get; protected set; }
    public Dictionary<string, BlockDropItemStack[]> DropsByType { get; protected set; }
    public Dictionary<string, Dictionary<EnumBlockMaterial, float>> MiningSpeedByType { get; protected set; }
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

    private ICoreClientAPI clientApi;
    private ICoreAPI coreApi;

    public override void OnLoaded(ICoreAPI api)
    {
        // blocks with this behavior cannot be chiseled
        block.Attributes ??= new JsonObject(new JObject());
        block.Attributes.Token["canChisel"] = JToken.FromObject(false);

        clientApi = api as ICoreClientAPI;
        iattr = IAttachableToEntity.FromAttributes(collObj);
    }

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);

        if (properties != null)
        {
            shapeByType = properties["shape"].AsObject<Dictionary<string, CompositeShape>>();
            shapeInventoryByType = properties["shapeInventory"].AsObject<Dictionary<string, CompositeShape>>();
            texturesByType = properties["textures"].AsObject<Dictionary<string, Dictionary<string, CompositeTexture>>>();

            ShapeIgnoreElementsByType = properties["shapeIgnoreElements"].AsObject<Dictionary<string, string[]>>();
            ShapeIgnoreElementsCombineByType = properties["shapeIgnoreElementsCombine"].AsObject<Dictionary<string, string[]>>();
            ShapeSelectiveElementsByType = properties["shapeSelectiveElements"].AsObject<Dictionary<string, string[]>>();
            ShapeSelectiveElementsCombineByType = properties["shapeSelectiveElementsCombine"].AsObject<Dictionary<string, string[]>>();

            NameByType = properties["name"].AsObject<Dictionary<string, List<object>>>();
            DescriptionByType = properties["description"].AsObject<Dictionary<string, List<object>>>();
            DropsByType = properties["drops"].AsObject<Dictionary<string, BlockDropItemStack[]>>();
            MiningSpeedByType = properties["miningSpeed"].AsObject<Dictionary<string, Dictionary<EnumBlockMaterial, float>>>();

            AttachedShapeBySlotCodeByType = properties["STFA_attachableToEntity"]?["attachedShapeBySlotCode"].AsObject<Dictionary<string, System.Collections.Generic.OrderedDictionary<string, CompositeShape>>>();
            CategoryCodeByType = properties["STFA_attachableToEntity"]?["categoryCode"].AsObject<Dictionary<string, string>>();
            DisableElementsByType = properties["STFA_attachableToEntity"]?["disableElements"].AsObject<Dictionary<string, string[]>>();
            KeepElementsByType = properties["STFA_attachableToEntity"]?["keepElements"].AsObject<Dictionary<string, string[]>>();

            LoadAndResolveCollisionAndSelectionBoxes(properties);
        }
    }

    private void LoadAndResolveCollisionAndSelectionBoxes(JsonObject properties)
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

    public override void OnUnloaded(ICoreAPI api)
    {
        Dictionary<string, MultiTextureMeshRef> meshRefs = ObjectCacheUtil.TryGet<Dictionary<string, MultiTextureMeshRef>>(api, "AttributeRenderingLibrary_BehaviorShapeTexturesFromAttributes_MeshRefs");
        meshRefs?.Foreach(meshRef => meshRef.Value?.Dispose());
        ObjectCacheUtil.Delete(api, "AttributeRenderingLibrary_BehaviorShapeTexturesFromAttributes_MeshRefs");

        Dictionary<string, MeshData> meshes = ObjectCacheUtil.TryGet<Dictionary<string, MeshData>>(api, "AttributeRenderingLibrary_BehaviorShapeTexturesFromAttributes_Meshes");
        meshes?.Foreach(mesh => mesh.Value?.Dispose());
        ObjectCacheUtil.Delete(api, "AttributeRenderingLibrary_BehaviorShapeTexturesFromAttributes_Meshes");
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

        Variants variants = beBehavior.Variants;
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

    public override Dictionary<EnumBlockMaterial, float> GetMiningSpeeds(ItemSlot slot, ref EnumHandling handling)
    {
        if (MiningSpeedByType == null || MiningSpeedByType.Count == 0)
        {
            return base.GetMiningSpeeds(slot, ref handling);
        }

        Variants variants = Variants.FromStack(slot.Itemstack);
        if (!variants.FindByVariant(MiningSpeedByType, out Dictionary<EnumBlockMaterial, float> miningSpeed))
        {
            return base.GetMiningSpeeds(slot, ref handling);
        }
        handling = EnumHandling.PreventSubsequent;
        return miningSpeed;
    }

    public virtual MeshData GenMesh(ItemSlot slot, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos)
    {
        return GenGuiMesh(slot);
    }

    public virtual string GetMeshCacheKey(ItemSlot slot)
    {
        return $"{slot.Itemstack.Collectible.Code}-{Variants.FromStack(slot.Itemstack)}";
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

    void IAttachableToEntity.CollectTextures(ItemStack stack, Shape shape, string texturePrefixCode, Dictionary<string, CompositeTexture> intoDict)
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

    CompositeShape IAttachableToEntity.GetAttachedShape(ItemStack stack, string slotCode)
    {
        if (AttachedShapeBySlotCodeByType == null || AttachedShapeBySlotCodeByType.Count == 0)
        {
            return iattr?.GetAttachedShape(stack, slotCode);
        }

        Variants variants = Variants.FromStack(stack);
        if (!variants.FindByVariant(AttachedShapeBySlotCodeByType, out System.Collections.Generic.OrderedDictionary<string, CompositeShape> attachedShapeBySlotCode))
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
                        if (coreApi.Assets.Exists(overlay.Base.Clone().CopyWithPathPrefixAndAppendixOnce("shapes/", ".json")))
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

    string IAttachableToEntity.GetTexturePrefixCode(ItemStack stack) => GetMeshCacheKey(new DummySlot(stack));

    bool IAttachableToEntity.IsAttachable(Entity toEntity, ItemStack itemStack) => true;

    int IAttachableToEntity.RequiresBehindSlots { get; set; }
}