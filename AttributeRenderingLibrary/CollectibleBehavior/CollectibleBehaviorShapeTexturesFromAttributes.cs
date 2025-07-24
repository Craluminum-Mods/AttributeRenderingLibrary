using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace AttributeRenderingLibrary;

public class CollectibleBehaviorShapeTexturesFromAttributes : CollectibleBehavior, IContainedMeshSource
{
    public Dictionary<string, List<object>> NameByType { get; protected set; } = new();
    public Dictionary<string, List<object>> DescriptionByType { get; protected set; } = new();

    protected Dictionary<string, CompositeShape> shapeByType = new();
    protected Dictionary<string, Dictionary<string, CompositeTexture>> texturesByType = new();
    private ICoreClientAPI clientApi;

    public CollectibleBehaviorShapeTexturesFromAttributes(CollectibleObject collObj) : base(collObj) { }

    public override void OnLoaded(ICoreAPI api)
    {
        clientApi = api as ICoreClientAPI;
    }

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);

        if (properties != null)
        {
            NameByType = properties["name"].AsObject(defaultValue: new Dictionary<string, List<object>>());
            DescriptionByType = properties["description"].AsObject(defaultValue: new Dictionary<string, List<object>>());

            shapeByType = properties["shape"].AsObject(defaultValue: new Dictionary<string, CompositeShape>());
            texturesByType = properties["textures"].AsObject(defaultValue: new Dictionary<string, Dictionary<string, CompositeTexture>>());
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
        variants.FindByVariant(shapeByType, out CompositeShape _shape);
        _shape ??= (collObj as Item)?.Shape;

        if (_shape == null) return mesh;

        CompositeShape rcshape = _shape.Clone();
        rcshape.Base.Path = variants.ReplacePlaceholders(rcshape.Base.Path);
        rcshape.Base.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/");

        Shape shape = clientApi.Assets.TryGet(rcshape.Base)?.ToObject<Shape>();
        if (shape == null) return mesh;

        variants.FindByVariant(texturesByType, out Dictionary<string, CompositeTexture> _textures);
        _textures ??= (collObj as Item)?.Textures;

        UniversalShapeTextureSource stexSource = new UniversalShapeTextureSource(clientApi, targetAtlas, shape, rcshape.Base.ToString());

        foreach (KeyValuePair<string, CompositeTexture> val in _textures)
        {
            CompositeTexture ctex = val.Value.Clone();
            ctex = variants.ReplacePlaceholders(ctex);
            ctex.Bake(clientApi.Assets);
            stexSource.textures[val.Key] = ctex;
        }
        clientApi.Tesselator.TesselateShape("ShapeTexturesFromAttributes item", shape, out mesh, stexSource);
        return mesh;
    }

    public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
    {
        Dictionary<string, MultiTextureMeshRef> meshRefs = ObjectCacheUtil.GetOrCreate(capi, "AttributeRenderingLibrary_BehaviorShapeTexturesFromAttributes_MeshRefs", () => new Dictionary<string, MultiTextureMeshRef>());

        string key = GetMeshCacheKey(itemstack);

        if (!meshRefs.TryGetValue(key, out MultiTextureMeshRef meshref))
        {
            MeshData mesh = GenMesh(itemstack, capi.ItemTextureAtlas, null);
            meshref = capi.Render.UploadMultiTextureMesh(mesh);
            meshRefs[key] = meshref;
        }

        renderinfo.ModelRef = meshref;
        renderinfo.NormalShaded = true;

        base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
    }

    public override void GetHeldItemName(StringBuilder sb, ItemStack itemStack)
    {
        Variants variants = Variants.FromStack(itemStack);
        variants.FindByVariant(NameByType, out List<object> _langKeys);

        string name = variants.GetName(_langKeys);
        if (string.IsNullOrEmpty(name))
        {
            return;
        }
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        Variants variants = Variants.FromStack(inSlot.Itemstack);
        variants.FindByVariant(DescriptionByType, out List<object> _langKeys);
        variants.GetDescription(dsc, _langKeys);
        variants.GetDebugDescription(dsc, withDebugInfo);
    }

    public virtual MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos)
    {
        return GetOrCreateMesh(itemstack, targetAtlas);
    }

    public virtual string GetMeshCacheKey(ItemStack itemstack)
    {
        return $"{itemstack.Collectible.Code}-{Variants.FromStack(itemstack)}";
    }
}
