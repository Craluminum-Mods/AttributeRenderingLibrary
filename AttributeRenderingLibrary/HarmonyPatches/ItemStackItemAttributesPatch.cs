using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace AttributeRenderingLibrary.HarmonyPatches;

[HarmonyPatch]
public static class ItemStackItemAttributesPatch
{
    [HarmonyPatch(typeof(ItemStack), nameof(ItemStack.ItemAttributes), MethodType.Getter)]
    [HarmonyPrefix]
    public static bool Prefix(ItemStack __instance, ref Vintagestory.API.Datastructures.JsonObject __result)
    {
        //TODO You can redirect ItemAttributes to be variant specific here
        return true;
    }
}
