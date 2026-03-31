using HarmonyLib;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace AttributeRenderingLibrary.HarmonyPatches.Handbook;

[HarmonyPatch(typeof(Block), nameof(Block.GetDropsForHandbook))]
public static class Block_GetDropsForHandbook_Patch
{
    [HarmonyPostfix]
    public static void Postfix(Block __instance, ref BlockDropItemStack[] __result, ItemStack handbookStack, IPlayer forPlayer)
    {
        if (handbookStack?.Collectible?.GetCollectibleInterface<IBlockShapeTexturesFromAttributes>() is not { } shapeTexturesFromAttributes)
        {
            return;
        }

        Variants variants = Variants.FromStack(handbookStack);
        if (variants.FindByVariant(shapeTexturesFromAttributes.DropsByType, out BlockDropItemStack[] unresolvedDrops)
            && unresolvedDrops != null
            && unresolvedDrops.Length > 0)
        {
            List<BlockDropItemStack> resolvedDrops = new(unresolvedDrops.Length);
            for (int i = 0; i < unresolvedDrops.Length; i++)
            {
                BlockDropItemStack dstack = variants.ReplacePlaceholders(unresolvedDrops[i].Clone());
                if (dstack.Resolve(forPlayer.Entity.World, "AttributeRenderingLibrary.BlockShapeTexturesFromAttributes", dstack.Code))
                {
                    resolvedDrops.Add(dstack);
                }
            }
            __result = [.. resolvedDrops];
            return;
        }

        __result = [new BlockDropItemStack(handbookStack)];
    }
}