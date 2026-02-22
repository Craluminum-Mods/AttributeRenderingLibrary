using HarmonyLib;
using Vintagestory.API.Common;

namespace AttributeRenderingLibrary.HarmonyPatches.Properties;

[HarmonyPatch(typeof(CollectibleObject), nameof(CollectibleObject.GetToolTier))]
public static class CollectibleObject_GetToolTier_Patch
{
    [HarmonyPostfix]
    public static void Postfix(CollectibleObject __instance, ref int __result, ItemSlot slot)
    {
        if (__instance?.GetCollectibleInterface<IPropertiesSupplier>() is not { } propertiesSupplier) return;

        EnumHandling handling = EnumHandling.PassThrough;
        int value = propertiesSupplier.GetToolTier(slot, ref handling);

        if (handling is EnumHandling.PreventSubsequent or EnumHandling.PreventDefault)
        {
            __result = value;
        }
    }
}