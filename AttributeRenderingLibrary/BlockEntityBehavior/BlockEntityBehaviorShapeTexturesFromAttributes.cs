using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace AttributeRenderingLibrary;

public class BlockEntityBehaviorShapeTexturesFromAttributes(BlockEntity blockentity) : BlockEntityBehavior(blockentity)
{
    public BlockBehaviorShapeTexturesFromAttributes OwnBehavior => Block?.GetBehavior<BlockBehaviorShapeTexturesFromAttributes>();
    public Variants Variants { get; protected set; } = new Variants();
    protected MeshData mesh;

    public override void Initialize(ICoreAPI api, JsonObject properties)
    {
        base.Initialize(api, properties);
        if (mesh == null) Init();
    }

    protected void Init()
    {
        if (Api == null || OwnBehavior == null) return;

        if (Api.Side == EnumAppSide.Client)
        {
            mesh = OwnBehavior.GetOrCreateMesh(Variants);
        }
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);
        Variants.ToTreeAttribute(tree);
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
    {
        Variants = Variants.FromTreeAttribute(tree);
        base.FromTreeAttributes(tree, worldForResolving);
        Init();
    }

    public override void OnBlockPlaced(ItemStack byItemStack = null)
    {
        if (byItemStack != null)
        {
            Variants = Variants.FromStack(byItemStack);
        }
        Init();
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
    {
        Vec3f rotationRad = OwnBehavior.GetRotation(Api.World, Pos);
        MeshData clonedMesh = mesh.Clone();
        clonedMesh = clonedMesh.Rotate(Vec3f.Half, rotationRad.X, rotationRad.Y, rotationRad.Z);
        mesher.AddMeshData(clonedMesh);
        return true; // skip default mesh
    }
}
