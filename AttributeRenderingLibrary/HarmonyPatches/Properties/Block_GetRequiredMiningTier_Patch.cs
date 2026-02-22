using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace AttributeRenderingLibrary.HarmonyPatches.Properties;

[HarmonyPatch(typeof(Block), nameof(Block.GetRequiredMiningTier))]
public static class Block_GetRequiredMiningTier_Patch
{
    [HarmonyPostfix]
    public static void Postfix(Block __instance, ref int __result, IWorldAccessor world, BlockPos pos)
    {
        if (__instance?.GetInterface<IBlockPropertiesSupplier>(world, pos) is not { } propertiesSupplier) return;

        EnumHandling handling = EnumHandling.PassThrough;
        int value = propertiesSupplier.GetRequiredMiningTier(world, pos, ref handling);

        if (handling is EnumHandling.PreventSubsequent or EnumHandling.PreventDefault)
        {
            __result = value;
        }
    }
}