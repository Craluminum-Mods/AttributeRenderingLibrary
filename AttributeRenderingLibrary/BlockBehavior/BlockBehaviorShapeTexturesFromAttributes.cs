using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace AttributeRenderingLibrary;

public class BlockBehaviorShapeTexturesFromAttributes(Block block) : StrongBlockBehavior(block), IShapeTexturesFromAttributes
{
    public Dictionary<string, List<object>> NameByType { get; protected set; } = new();
    public Dictionary<string, List<object>> DescriptionByType { get; protected set; } = new();
    public Dictionary<string, Cuboidf[]> CollisionBoxesByType { get; protected set; } = new();
    public Dictionary<string, Cuboidf[]> SelectionBoxesByType { get; protected set; } = new();
    public Dictionary<string, BlockDropItemStack[]> DropsByType { get; protected set; } = new();

    public Dictionary<string, CompositeShape> shapeByType { get; protected set; } = new();
    public Dictionary<string, CompositeShape> shapeInventoryByType { get; protected set; } = new();
    public Dictionary<string, Dictionary<string, CompositeTexture>> texturesByType { get; protected set; } = new();
    private ICoreClientAPI clientApi;

    public override void OnLoaded(ICoreAPI api)
    {
        clientApi = api as ICoreClientAPI;
    }

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);

        if (properties != null)
        {
            NameByType = properties["name"].AsObject<Dictionary<string, List<object>>>();
            DescriptionByType = properties["description"].AsObject<Dictionary<string, List<object>>>();
            DropsByType = properties["drops"].AsObject<Dictionary<string, BlockDropItemStack[]>>();

            shapeByType = properties["shape"].AsObject<Dictionary<string, CompositeShape>>();
            shapeInventoryByType = properties["shapeInventory"].AsObject<Dictionary<string, CompositeShape>>();
            texturesByType = properties["textures"].AsObject<Dictionary<string, Dictionary<string, CompositeTexture>>>();

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

    public virtual MeshData GenGuiMesh(ItemStack itemstack)
    {
        MeshData mesh = RenderExtensions.GenEmptyMesh();

        Variants variants = Variants.FromStack(itemstack);
        variants.FindByVariant(shapeInventoryByType, out CompositeShape ucshape);

        if (ucshape == null)
        {
            variants.FindByVariant(shapeByType, out ucshape);
        }

        ucshape ??= block.ShapeInventory ?? block.Shape;

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
            overlayPrefix = GetMeshCacheKey(itemstack);
            prefixedTextureCodes = ShapeOverlayHelper.AddOverlays(clientApi, overlayPrefix, variants, stexSource, shape, rcshape);
        }

        foreach ((string textureCode, CompositeTexture texture) in itemstack.Block.Textures)
        {
            stexSource.textures[textureCode] = texture;
        }

        ShapeOverlayHelper.BakeVariantTextures(clientApi, stexSource, variants, texturesByType, prefixedTextureCodes, overlayPrefix);

        TesselationMetaData meta = new TesselationMetaData
        {
            QuantityElements = rcshape.QuantityElements,
            SelectiveElements = rcshape.SelectiveElements,
            IgnoreElements = rcshape.IgnoreElements,
            TexSource = stexSource,
            TypeForLogging = "ShapeTexturesFromAttributes block behavior"
        };

        clientApi.Tesselator.TesselateShape(meta, shape, out mesh);
        return mesh;
    }

    public virtual MeshData GetOrCreateMesh(Variants variants, ITexPositionSource overrideTexturesource = null)
    {
        Dictionary<string, MeshData> cMeshes = ObjectCacheUtil.GetOrCreate(clientApi, "AttributeRenderingLibrary_BehaviorShapeTexturesFromAttributes_Meshes", () => new Dictionary<string, MeshData>());

        string key = $"{block.Code}-{variants}";
        if (overrideTexturesource != null || !cMeshes.TryGetValue(key, out MeshData mesh))
        {
            mesh = RenderExtensions.GenEmptyMesh();

            variants.FindByVariant(shapeByType, out CompositeShape ucshape);
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
                SelectiveElements = rcshape.SelectiveElements,
                IgnoreElements = rcshape.IgnoreElements,
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

    public override void OnBeforeRender(ICoreClientAPI clientApi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
    {
        Dictionary<string, MultiTextureMeshRef> meshRefs = ObjectCacheUtil.GetOrCreate(clientApi, "AttributeRenderingLibrary_BehaviorShapeTexturesFromAttributes_MeshRefs", () => new Dictionary<string, MultiTextureMeshRef>());

        string key = GetMeshCacheKey(itemstack);

        if (!meshRefs.TryGetValue(key, out MultiTextureMeshRef meshref))
        {
            MeshData mesh = GenGuiMesh(itemstack);
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
            if (beBehavior.Variants.FindByVariant(DropsByType, out BlockDropItemStack[] unresolvedDrops))
            {
                List<ItemStack> todrop = [];
                for (int i = 0; i < unresolvedDrops.Length; i++)
                {
                    BlockDropItemStack dstack = unresolvedDrops[i];
                    ItemStack stack = dstack.ToRandomItemstackForPlayer(byPlayer, world, dropQuantityMultiplier);
                    if (stack != null)
                    {
                        todrop.Add(stack);
                        if (dstack.LastDrop)
                        {
                            break;
                        }
                    }
                }
                handling = EnumHandling.Handled;
                return todrop.ToArray();
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
            handling = EnumHandling.Handled;
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
        if (world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BlockEntityBehaviorShapeTexturesFromAttributes>() is not BlockEntityBehaviorShapeTexturesFromAttributes beBehavior)
        {
            return Vec3f.Zero;
        }

        beBehavior.Variants.FindByVariant(shapeByType, out CompositeShape shapeForRotation);
        shapeForRotation ??= block.Shape;

        return new Vec3f
        {
            X = (shapeForRotation?.rotateX ?? 0) * GameMath.DEG2RAD,
            Y = (shapeForRotation?.rotateY ?? 0) * GameMath.DEG2RAD,
            Z = (shapeForRotation?.rotateZ ?? 0) * GameMath.DEG2RAD
        };
    }

    public override void GetHeldItemName(StringBuilder sb, ItemStack itemStack)
    {
        if (NameByType == null || !NameByType.Any())
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
        if (NameByType == null || !NameByType.Any())
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
        if (DescriptionByType == null || !DescriptionByType.Any())
        {
            return;
        }

        Variants variants = Variants.FromStack(inSlot.Itemstack);
        variants.FindByVariant(DescriptionByType, out List<object> _langKeys);
        variants.GetDescription(dsc, _langKeys);
        variants.GetDebugDescription(dsc, withDebugInfo);
    }

    public virtual MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos)
    {
        return GenGuiMesh(itemstack);
    }

    public virtual string GetMeshCacheKey(ItemStack itemstack)
    {
        return $"{itemstack.Collectible.Code}-{Variants.FromStack(itemstack)}";
    }

    public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos, ref EnumHandling handled)
    {
        if (blockAccessor.GetBlockEntity(pos)?.GetBehavior<BlockEntityBehaviorShapeTexturesFromAttributes>() is BlockEntityBehaviorShapeTexturesFromAttributes beBehavior
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
        if (blockAccessor.GetBlockEntity(pos)?.GetBehavior<BlockEntityBehaviorShapeTexturesFromAttributes>() is BlockEntityBehaviorShapeTexturesFromAttributes beBehavior
            && beBehavior.Variants.FindByVariant(SelectionBoxesByType, out Cuboidf[] cuboids)
            && cuboids != null
            && cuboids.Length > 0)
        {
            handled = EnumHandling.PreventSubsequent;
            return cuboids;
        }
        return base.GetSelectionBoxes(blockAccessor, pos, ref handled);
    }
}