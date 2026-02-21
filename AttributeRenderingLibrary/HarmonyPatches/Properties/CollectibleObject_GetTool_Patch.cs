using HarmonyLib;
using Vintagestory.API.Common;

namespace AttributeRenderingLibrary.HarmonyPatches.Properties;

[HarmonyPatch(typeof(CollectibleObject), nameof(CollectibleObject.GetTool))]
public static class CollectibleObject_GetTool_Patch
{
    [HarmonyPostfix]
    public static void Postfix(CollectibleObject __instance, ref EnumTool? __result, ItemSlot slot)
    {
        if (__instance?.GetCollectibleInterface<IPropertiesSupplier>() is not IPropertiesSupplier propertiesSupplier) return;

        EnumHandling handling = EnumHandling.PassThrough;
        EnumTool? value = propertiesSupplier.GetTool(slot, ref handling);

        if (handling is EnumHandling.PreventSubsequent or EnumHandling.PreventDefault)
        {
            __result = value;
        }
    }
}
