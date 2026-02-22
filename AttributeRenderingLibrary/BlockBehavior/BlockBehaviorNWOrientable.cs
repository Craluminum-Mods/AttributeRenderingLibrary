using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace AttributeRenderingLibrary;

/// <summary>
/// Version of <see cref="Vintagestory.GameContent.BlockBehaviorNWOrientable"/> that works with <see cref="Variants"/>
/// </summary>
public class BlockBehaviorNWOrientable : BlockBehavior
{
    string variantCode = "orientation";

    public BlockBehaviorNWOrientable(Block block) : base(block)
    {
        if (!block.Variant.ContainsKey("orientation"))
        {
            variantCode = "side";
        }
    }

    public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode)
    {
        handling = EnumHandling.PreventDefault;
        BlockFacing[] horVer = Block.SuggestedHVOrientation(byPlayer, blockSel);
        string code = "ns";
        if (horVer[0].Index == 1 || horVer[0].Index == 3) code = "we";
        Block orientedBlock = world.BlockAccessor.GetBlock(block.CodeWithVariant(variantCode, code));

        if (orientedBlock.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
        {
            orientedBlock.DoPlaceBlock(world, byPlayer, blockSel, itemstack);
            return true;
        }

        return false;
    }

    public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref float dropQuantityMultiplier, ref EnumHandling handled)
    {
        return [OnPickBlock(world, pos, ref handled)];
    }

    public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos, ref EnumHandling handled)
    {
        handled = EnumHandling.PreventSubsequent;
        if (world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BlockEntityBehaviorShapeTexturesFromAttributes>() is not { } beBehavior)
        {
            handled = EnumHandling.PassThrough;
            return null;
        }

        ItemStack stack = new ItemStack(world.BlockAccessor.GetBlock(block.CodeWithVariant(variantCode, "ns")));
        beBehavior.Variants.ToStack(stack);
        return stack;
    }

    public override AssetLocation GetRotatedBlockCode(int angle, ref EnumHandling handled)
    {
        handled = EnumHandling.PreventDefault;

        string[] angles = { "ns", "we" };
        var index = GameMath.Mod(angle / 90, 4);
        if (block.Variant[variantCode] == "we") index++;
        return block.CodeWithVariant(variantCode, angles[index % 2]);
    }
}