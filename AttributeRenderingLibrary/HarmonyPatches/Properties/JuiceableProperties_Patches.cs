using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace AttributeRenderingLibrary.HarmonyPatches.Properties;

[HarmonyPatch]
public static class JuiceableProperties_Patches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CollectibleBehaviorHandbookTextAndExtraInfo), nameof(CollectibleBehaviorHandbookTextAndExtraInfo.getjuiceableProps))]
    public static void Postfix_1(CollectibleBehaviorHandbookTextAndExtraInfo __instance, ref JuiceableProperties __result, ItemStack stack)
    {
        if (stack?.Collectible?.GetCollectibleInterface<ICollectiblePropertiesSupplier>() is not { } propertiesSupplier) return;

        EnumHandling handling = EnumHandling.PassThrough;
        JuiceableProperties value = propertiesSupplier.GetJuiceableProperties(stack, ref handling);

        if (handling is EnumHandling.PreventSubsequent or EnumHandling.PreventDefault)
        {
            __result = value;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BlockEntityFruitPress), nameof(BlockEntityFruitPress.getJuiceableProps))]
    public static void Postfix_2(BlockEntityFruitPress __instance, ref JuiceableProperties __result, ItemStack stack)
    {
        if (stack?.Collectible?.GetCollectibleInterface<ICollectiblePropertiesSupplier>() is not { } propertiesSupplier) return;

        EnumHandling handling = EnumHandling.PassThrough;
        JuiceableProperties value = propertiesSupplier.GetJuiceableProperties(stack, ref handling);

        if (handling is EnumHandling.PreventSubsequent or EnumHandling.PreventDefault)
        {
            __result = value;
        }
    }
}