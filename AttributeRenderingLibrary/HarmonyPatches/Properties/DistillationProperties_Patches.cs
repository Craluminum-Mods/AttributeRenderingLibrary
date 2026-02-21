using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace AttributeRenderingLibrary.HarmonyPatches.Properties;

[HarmonyPatch]
public static class DistillationProperties_Patches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CollectibleBehaviorHandbookTextAndExtraInfo), nameof(CollectibleBehaviorHandbookTextAndExtraInfo.getDistillationProps))]
    public static void Postfix_1(CollectibleBehaviorHandbookTextAndExtraInfo __instance, ref DistillationProps __result, ItemStack stack)
    {
        if (stack?.Collectible?.GetCollectibleInterface<ICollectiblePropertiesSupplier>() is not ICollectiblePropertiesSupplier propertiesSupplier) return;

        EnumHandling handling = EnumHandling.PassThrough;
        DistillationProps value = propertiesSupplier.GetDistillationProperties(stack, ref handling);

        if (handling is EnumHandling.PreventSubsequent or EnumHandling.PreventDefault)
        {
            __result = value;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BlockEntityBoiler), nameof(BlockEntityBoiler.DistProps), MethodType.Getter)]
    public static void Postfix_2(BlockEntityBoiler __instance, ref DistillationProps __result)
    {
        if (__instance.InputStack?.Collectible?.GetCollectibleInterface<ICollectiblePropertiesSupplier>() is not ICollectiblePropertiesSupplier propertiesSupplier) return;

        EnumHandling handling = EnumHandling.PassThrough;
        DistillationProps value = propertiesSupplier.GetDistillationProperties(__instance.InputStack, ref handling);

        if (handling is EnumHandling.PreventSubsequent or EnumHandling.PreventDefault)
        {
            __result = value;
        }
    }
}