using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace AttributeRenderingLibrary.HarmonyPatches.Properties;

[HarmonyPatch(typeof(Block), nameof(Block.GetBlockMaterial))]
public static class Block_GetBlockMaterial_Patch
{
    [HarmonyPostfix]
    public static void Postfix(Block __instance, ref EnumBlockMaterial __result, IBlockAccessor blockAccessor, BlockPos pos, ItemStack stack)
    {
        if (stack != null)
        {
            if (stack?.Collectible?.GetCollectibleInterface<IBlockPropertiesSupplier>() is not { } propertiesSupplier) return;

            EnumHandling handling = EnumHandling.PassThrough;
            EnumBlockMaterial value = propertiesSupplier.GetBlockMaterial(blockAccessor, pos, stack, ref handling);

            if (handling is EnumHandling.PreventSubsequent or EnumHandling.PreventDefault)
            {
                __result = value;
            }
        }

        if (pos != null)
        {
            lock (pos)
            {
                if (__instance?.GetInterface<IBlockPropertiesSupplier>(Core.Api.World, pos) is not { } propertiesSupplier) return;

                EnumHandling handling = EnumHandling.PassThrough;
                EnumBlockMaterial value = propertiesSupplier.GetBlockMaterial(blockAccessor, pos, stack, ref handling);

                if (handling is EnumHandling.PreventSubsequent or EnumHandling.PreventDefault)
                {
                    __result = value;
                }
            }
        }
    }
}