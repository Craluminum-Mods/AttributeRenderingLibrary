
using HarmonyLib;
using System.Runtime.CompilerServices;
using Vintagestory.API.Common;

namespace AttributeRenderingLibrary;

[HarmonyPatch(typeof(Block), nameof(Block.GetDropsForHandbook))]
public static class GetDropsForHandbookPatch
{
    [HarmonyReversePatch(HarmonyReversePatchType.Original)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static BlockDropItemStack[] Base(this Block instance, ItemStack handbookStack, IPlayer forPlayer) => default;

    [HarmonyPrefix]
    public static bool Prefix(Block __instance, ref BlockDropItemStack[] __result, ItemStack handbookStack, IPlayer forPlayer)
    {
        if (handbookStack?.Collectible?.GetCollectibleInterface<IShapeTexturesFromAttributes>() is not IShapeTexturesFromAttributes shapeTexturesFromAttributes)
        {
            return true;
        }

        BlockDropItemStack[] drops = Base(__instance, handbookStack, forPlayer) ?? [];
        drops[0] = drops[0].Clone();
        drops[0].ResolvedItemstack.SetFrom(handbookStack);
        __result = drops;
        return false;
    }
}