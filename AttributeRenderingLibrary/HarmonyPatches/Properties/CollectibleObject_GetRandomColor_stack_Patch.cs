using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace AttributeRenderingLibrary.HarmonyPatches.Properties;

[HarmonyPatch(typeof(CollectibleObject), nameof(CollectibleObject.GetRandomColor))]
public static class CollectibleObject_GetRandomColor_stack_Patch
{
    [HarmonyPostfix]
    public static void Postfix(CollectibleObject __instance, ref int __result, ICoreClientAPI capi, ItemStack stack)
    {
        if (__instance?.GetCollectibleInterface<IPropertiesSupplier>() is not { } propertiesSupplier) return;

        EnumHandling handling = EnumHandling.PassThrough;
        int value = propertiesSupplier.GetRandomColor(capi, stack, ref handling);

        if (handling is EnumHandling.PreventSubsequent or EnumHandling.PreventDefault)
        {
            __result = value;
        }
    }
}