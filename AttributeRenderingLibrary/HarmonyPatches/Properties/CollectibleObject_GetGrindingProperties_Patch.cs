using HarmonyLib;
using Vintagestory.API.Common;

namespace AttributeRenderingLibrary.HarmonyPatches.Properties;

[HarmonyPatch(typeof(CollectibleObject), nameof(CollectibleObject.GetGrindingProperties))]
public static class CollectibleObject_GetGrindingProperties_Patch
{
    [HarmonyPostfix]
    public static void Postfix(CollectibleObject __instance, ref GrindingProperties __result, IWorldAccessor world, ItemStack itemstack)
    {
        if (__instance?.GetCollectibleInterface<IPropertiesSupplier>() is not IPropertiesSupplier propertiesSupplier) return;

        EnumHandling handling = EnumHandling.PassThrough;
        GrindingProperties value = propertiesSupplier.GetGrindingProperties(world, itemstack, ref handling);

        if (handling is EnumHandling.PreventSubsequent or EnumHandling.PreventDefault)
        {
            __result = value;
        }
    }
}