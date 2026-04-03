using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace AttributeRenderingLibrary;

public class BlockEntityBehaviorShapeTexturesFromAttributes(BlockEntity blockentity): BlockEntityBehavior(blockentity), IRotatablePlaceable
{
    public BlockBehaviorShapeTexturesFromAttributes? OwnBehavior => Block?.GetBehavior<BlockBehaviorShapeTexturesFromAttributes>();
    public Variants Variants { get; protected set; } = new Variants();

    protected MeshData? mesh;

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
            mesh = OwnBehavior?.GetOrCreateMesh(Variants, null, Pos, "");
        }
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);
        Variants.ToTreeAttribute(tree);

        #region IRotatablePlaceable
        tree.SetFloat("meshAngleRad", MeshAngleRad);
        #endregion
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
    {
        base.FromTreeAttributes(tree, worldForResolving);
        Variants = Variants.FromTreeAttribute(tree);

        #region IRotatablePlaceable
        MeshAngleRad = tree.GetFloat("meshAngleRad");
        #endregion

        Init();
    }

    public override void OnBlockPlaced(ItemStack? byItemStack = null)
    {
        if (byItemStack != null)
        {
            Variants = Variants.FromStack(byItemStack);
        }
        Init();
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
    {
        base.GetBlockInfo(forPlayer, dsc);

        if (Api is ICoreClientAPI capi)
        {
            Variants.GetDebugDescription(dsc);
        }
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
    {
        Vec3f? rotationRad = OwnBehavior?.GetRotation(Api.World, Pos);
        MeshData? clonedMesh = mesh?.Clone() ?? RenderExtensions.GetUnknownBlockModelData((Api as ICoreClientAPI)!);

        if (Block.RandomizeRotations)
        {
            int randomSelector = GameMath.MurmurHash3(-Blockentity.Pos.X, (Blockentity.Block.RandomizeAxes == EnumRandomizeAxes.XYZ) ? Blockentity.Pos.Y : 0, Blockentity.Pos.Z);
            float[] matrix = TesselationMetaData.randomRotMatrices[GameMath.Mod(randomSelector, TesselationMetaData.randomRotMatrices.Length)];
            clonedMesh = clonedMesh?.MatrixTransform(matrix);
        }
        else if (rotationRad != null)
        {
            clonedMesh = clonedMesh?.Rotate(rotationRad.X, rotationRad.Y, rotationRad.Z);
        }

        mesher.AddMeshData(clonedMesh);
        return true; // skip default mesh
    }

    #region IRotatablePlaceable
    public float MeshAngleRad { get; set; }

    public bool DoPartialSelection()
    {
        var tempHandling = EnumHandling.PassThrough;
        return OwnBehavior?.DoPartialSelection(Api.World, Pos, ref tempHandling) ?? false;
    }

    public Cuboidf[] GetCollisionBoxes()
    {
        var tempHandling = EnumHandling.PassThrough;
        return OwnBehavior?.GetCollisionBoxes(Api.World.BlockAccessor, Pos, ref tempHandling) ?? Block.CollisionBoxes ?? [ Block.DefaultCollisionBox ];
    }

    public Cuboidf[] GetSelectionBoxes()
    {
        var tempHandling = EnumHandling.PassThrough;
        return OwnBehavior?.GetSelectionBoxes(Api.World.BlockAccessor, Pos, ref tempHandling) ?? Block.SelectionBoxes ?? [ Block.DefaultCollisionBox ];
    }
    #endregion
}
