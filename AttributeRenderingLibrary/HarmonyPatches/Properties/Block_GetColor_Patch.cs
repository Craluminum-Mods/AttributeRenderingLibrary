using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace AttributeRenderingLibrary.HarmonyPatches.Properties;

[HarmonyPatch(typeof(Block), nameof(Block.GetColor))]
public static class Block_GetColor_Patch
{
    [HarmonyPostfix]
    public static void Postfix(Block __instance, ref int __result, ICoreClientAPI capi, BlockPos pos)
    {
        if (__instance?.GetCollectibleInterface<IBlockPropertiesSupplier>() is not { } propertiesSupplier) return;

        EnumHandling handling = EnumHandling.PassThrough;
        int value = propertiesSupplier.GetColor(capi, pos, ref handling);

        if (handling is EnumHandling.PreventSubsequent or EnumHandling.PreventDefault)
        {
            __result = value;
        }
    }
}