using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace AttributeRenderingLibrary.HarmonyPatches.Properties;

[HarmonyPatch(typeof(CollectibleObject), nameof(CollectibleObject.GetLightHsv))]
public static class CollectibleObject_GetLightHsv_Patch
{
    [HarmonyPostfix]
    public static void Postfix(Block __instance, ref byte[] __result, IBlockAccessor blockAccessor, BlockPos pos, ItemStack stack)
    {
        if (stack != null)
        {
            if (stack?.Collectible?.GetCollectibleInterface<IPropertiesSupplier>() is not { } propertiesSupplier) return;

            EnumHandling handling = EnumHandling.PassThrough;
            byte[] value = propertiesSupplier.GetLightHsv(blockAccessor, pos, stack, ref handling);

            if (handling is EnumHandling.PreventSubsequent or EnumHandling.PreventDefault)
            {
                __result = value;
            }
        }

        if (pos != null)
        {
            lock (pos)
            {
                if (__instance?.GetInterface<IPropertiesSupplier>(Core.Api.World, pos) is not { } propertiesSupplier) return;

                EnumHandling handling = EnumHandling.PassThrough;
                byte[] value = propertiesSupplier.GetLightHsv(blockAccessor, pos, stack, ref handling);

                if (handling is EnumHandling.PreventSubsequent or EnumHandling.PreventDefault)
                {
                    __result = value;
                }
            }
        }
    }
}