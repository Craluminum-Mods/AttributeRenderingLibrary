using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace AttributeRenderingLibrary.Systems;

public class BehaviorPrioritySystem : ModSystem
{
    public override void AssetsFinalize(ICoreAPI api)
    {
        foreach (Block block in api.World.Blocks)
        {
            if (block == null || block.Code == null) continue;

            AddMissingBlockEntityStuff(block);
            EnsureBlockBehaviorOrder<BlockBehaviorShapeTexturesFromAttributes, BlockBehaviorHorizontalAttachable>(block);
            EnsureBlockBehaviorOrder<BlockBehaviorShapeTexturesFromAttributes, BlockBehaviorNWOrientable>(block);
            EnsureBlockBehaviorOrder<BlockBehaviorShapeTexturesFromAttributes, BlockBehaviorHorizontalOrientable>(block);
        }
    }

    private static void AddMissingBlockEntityStuff(Block block)
    {
        if (block.HasBehavior<BlockBehaviorShapeTexturesFromAttributes>())
        {
            block.EntityClass ??= "Generic";
        }
    }

    public static void EnsureBlockBehaviorOrder<T1, T2>(Block block) where T1 : BlockBehavior where T2 : BlockBehavior
    {
        if (!block.HasBehavior<T1>() || !block.HasBehavior<T2>()) return;

        int mainIndex = block.CollectibleBehaviors.IndexOf(b => b is T1);
        int otherIndex = block.CollectibleBehaviors.IndexOf(b => b is T2);
        if (mainIndex > otherIndex)
        {
            Swap(block.CollectibleBehaviors, mainIndex, otherIndex);
        }

        mainIndex = block.BlockBehaviors.IndexOf(b => b is T1);
        otherIndex = block.BlockBehaviors.IndexOf(b => b is T2);
        if (mainIndex > otherIndex)
        {
            Swap(block.BlockBehaviors, mainIndex, otherIndex);
        }
    }

    public static void Swap<T>(IList<T> list, int indexA, int indexB)
    {
        T tmp = list[indexA];
        list[indexA] = list[indexB];
        list[indexB] = tmp;
    }
}
