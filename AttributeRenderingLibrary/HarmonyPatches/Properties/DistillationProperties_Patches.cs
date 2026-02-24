using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace AttributeRenderingLibrary.HarmonyPatches.Properties;

[HarmonyPatch]
public static class DistillationProperties_Patches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CollectibleBehaviorHandbookTextAndExtraInfo), nameof(CollectibleBehaviorHandbookTextAndExtraInfo.getDistillationProps))]
    public static bool Prefix_1(CollectibleBehaviorHandbookTextAndExtraInfo __instance, ref DistillationProps __result, ItemStack stack)
    {
        if (stack?.Collectible?.GetCollectibleInterface<ICollectiblePropertiesSupplier>() is not { } propertiesSupplier) return true;

        EnumHandling handling = EnumHandling.PassThrough;
        DistillationProps value = propertiesSupplier.GetDistillationProperties(stack, ref handling);

        if (handling is EnumHandling.PreventSubsequent or EnumHandling.PreventDefault)
        {
            __result = value;
            return false;
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(BlockEntityBoiler), nameof(BlockEntityBoiler.DistProps), MethodType.Getter)]
    public static bool Prefix_2(BlockEntityBoiler __instance, ref DistillationProps __result)
    {
        if (__instance.InputStack?.Collectible?.GetCollectibleInterface<ICollectiblePropertiesSupplier>() is not { } propertiesSupplier) return true;

        EnumHandling handling = EnumHandling.PassThrough;
        DistillationProps value = propertiesSupplier.GetDistillationProperties(__instance.InputStack, ref handling);

        if (handling is EnumHandling.PreventSubsequent or EnumHandling.PreventDefault)
        {
            __result = value;
            return false;
        }
        return true;
    }
}