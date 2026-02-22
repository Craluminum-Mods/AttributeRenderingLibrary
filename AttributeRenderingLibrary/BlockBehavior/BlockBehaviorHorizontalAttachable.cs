using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace AttributeRenderingLibrary;

/// <summary>
/// Version of <see cref="Vintagestory.GameContent.BlockBehaviorHorizontalAttachable"/> that works with <see cref="Variants"/>
/// </summary>
/// <param name="block"></param>
public class BlockBehaviorHorizontalAttachable(Block block) : BlockBehavior(block)
{
    private bool handleDrops = true;
    private string dropBlockFace = "north";
    private string dropBlock = null;
    private Dictionary<string, Cuboidi> attachmentAreas;

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);

        handleDrops = properties["handleDrops"].AsBool(true);

        if (properties["dropBlockFace"].Exists)
        {
            dropBlockFace = properties["dropBlockFace"].AsString();
        }
        if (properties["dropBlock"].Exists)
        {
            dropBlock = properties["dropBlock"].AsString();
        }

        Dictionary<string, RotatableCube> areas = properties["attachmentAreas"].AsObject<Dictionary<string, RotatableCube>>(null);
        attachmentAreas = [];
        if (areas != null)
        {
            foreach (KeyValuePair<string, RotatableCube> val in areas)
            {
                val.Value.Origin.Set(8, 8, 8);
                attachmentAreas[val.Key] = val.Value.RotatedCopy().ConvertToCuboidi();
            }
        }
        else
        {
            attachmentAreas["up"] = properties["attachmentArea"].AsObject<Cuboidi>(null);
        }
    }

    public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode)
    {
        handling = EnumHandling.PreventDefault;

        // Prefer selected block face
        if (blockSel.Face.IsHorizontal)
        {
            if (TryAttachTo(world, byPlayer, blockSel, itemstack, ref failureCode)) return true;
        }

        // Otherwise attach to any possible face
        BlockFacing[] faces = BlockFacing.HORIZONTALS;
        blockSel = blockSel.Clone();
        for (int i = 0; i < faces.Length; i++)
        {
            blockSel.Face = faces[i];
            if (TryAttachTo(world, byPlayer, blockSel, itemstack, ref failureCode)) return true;
        }

        failureCode = "requirehorizontalattachable";

        return false;
    }

    public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref float dropQuantityMultiplier, ref EnumHandling handled)
    {
        if (handleDrops)
        {
            return [OnPickBlock(world, pos, ref handled)];
        }
        else
        {
            handled = EnumHandling.PassThrough;
            return null;
        }
    }

    public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos, ref EnumHandling handled)
    {
        handled = EnumHandling.PreventSubsequent;

        if (world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BlockEntityBehaviorShapeTexturesFromAttributes>() is not { } beBehavior)
        {
            handled = EnumHandling.PassThrough;
            return null;
        }

        if (dropBlock != null)
        {
            ItemStack stack = new(world.BlockAccessor.GetBlock(new AssetLocation(dropBlock)));
            beBehavior.Variants.ToStack(stack);
            return stack;
        }

        ItemStack _stack = new(world.BlockAccessor.GetBlock(block.CodeWithParts(dropBlockFace)));
        beBehavior.Variants.ToStack(_stack);
        return _stack;
    }

    public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos, ref EnumHandling handled)
    {
        handled = EnumHandling.PreventDefault;

        if (!CanBlockStay(world, pos))
        {
            world.BlockAccessor.BreakBlock(pos, null);
        }
    }

    private bool TryAttachTo(IWorldAccessor world, IPlayer player, BlockSelection blockSel, ItemStack itemstack, ref string failureCode)
    {
        BlockFacing oppositeFace = blockSel.Face.Opposite;

        BlockPos attachingBlockPos = blockSel.Position.AddCopy(oppositeFace);
        Block attachingBlock = world.BlockAccessor.GetBlock(attachingBlockPos);
        Block orientedBlock = world.BlockAccessor.GetBlock(block.CodeWithParts(oppositeFace.Code));

        Cuboidi attachmentArea = null;
        attachmentAreas?.TryGetValue(oppositeFace.Code, out attachmentArea);

        if (attachingBlock.CanAttachBlockAt(world.BlockAccessor, block, attachingBlockPos, blockSel.Face, attachmentArea) && orientedBlock.CanPlaceBlock(world, player, blockSel, ref failureCode))
        {
            orientedBlock.DoPlaceBlock(world, player, blockSel, itemstack);
            return true;
        }
        return false;
    }

    private bool CanBlockStay(IWorldAccessor world, BlockPos pos)
    {
        string[] parts = block.Code.Path.Split('-');
        BlockFacing facing = BlockFacing.FromCode(parts[^1]);
        Block attachingblock = world.BlockAccessor.GetBlock(pos.AddCopy(facing));

        Cuboidi attachmentArea = null;
        attachmentAreas?.TryGetValue(facing.Code, out attachmentArea);

        return attachingblock.CanAttachBlockAt(world.BlockAccessor, block, pos.AddCopy(facing), facing.Opposite, attachmentArea);
    }

    public override bool CanAttachBlockAt(IBlockAccessor world, Block block, BlockPos pos, BlockFacing blockFace, ref EnumHandling handled, Cuboidi attachmentArea = null)
    {
        handled = EnumHandling.PreventDefault;
        return false;
    }

    public override AssetLocation GetRotatedBlockCode(int angle, ref EnumHandling handled)
    {
        handled = EnumHandling.PreventDefault;

        BlockFacing beforeFacing = BlockFacing.FromCode(block.LastCodePart());
        int rotatedIndex = GameMath.Mod(beforeFacing.HorizontalAngleIndex - (angle / 90), 4);
        BlockFacing nowFacing = BlockFacing.HORIZONTALS_ANGLEORDER[rotatedIndex];

        return block.CodeWithParts(nowFacing.Code);
    }

    public override AssetLocation GetHorizontallyFlippedBlockCode(EnumAxis axis, ref EnumHandling handling)
    {
        handling = EnumHandling.PreventDefault;

        BlockFacing facing = BlockFacing.FromCode(block.LastCodePart());
        return facing.Axis == axis ? block.CodeWithParts(facing.Opposite.Code) : block.Code;
    }
}