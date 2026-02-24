using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace AttributeRenderingLibrary.HarmonyPatches.Properties;

[HarmonyPatch]
public static class JuiceableProperties_Patches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CollectibleBehaviorHandbookTextAndExtraInfo), nameof(CollectibleBehaviorHandbookTextAndExtraInfo.getjuiceableProps))]
    public static bool Prefix_1(CollectibleBehaviorHandbookTextAndExtraInfo __instance, ref JuiceableProperties __result, ItemStack stack)
    {
        if (stack?.Collectible?.GetCollectibleInterface<ICollectiblePropertiesSupplier>() is not { } propertiesSupplier) return true;

        EnumHandling handling = EnumHandling.PassThrough;
        JuiceableProperties value = propertiesSupplier.GetJuiceableProperties(stack, ref handling);

        if (handling is EnumHandling.PreventSubsequent or EnumHandling.PreventDefault)
        {
            __result = value;
            return false;
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(BlockEntityFruitPress), nameof(BlockEntityFruitPress.getJuiceableProps))]
    public static bool Prefix_2(BlockEntityFruitPress __instance, ref JuiceableProperties __result, ItemStack stack)
    {
        if (stack?.Collectible?.GetCollectibleInterface<ICollectiblePropertiesSupplier>() is not { } propertiesSupplier) return true;

        EnumHandling handling = EnumHandling.PassThrough;
        JuiceableProperties value = propertiesSupplier.GetJuiceableProperties(stack, ref handling);

        if (handling is EnumHandling.PreventSubsequent or EnumHandling.PreventDefault)
        {
            __result = value;
            return false;
        }
        return true;
    }
}