
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace AttributeRenderingLibrary;

[HarmonyPatch(typeof(BlockEntityDisplay), "applyDefaultTranforms")]
public static class CustomTransformPatch
{
    [HarmonyPrefix]
    public static bool Prefix(BlockEntityDisplay __instance, ItemStack stack, ref MeshData mesh)
    {
        if (stack.Collectible?.GetCollectibleInterface<IContainedTransform>() is not IContainedTransform containedTransform
            || containedTransform.GetTransform(__instance, __instance.AttributeTransformCode, stack) is not ModelTransform transform)
        {
            return true;
        }

        transform.EnsureDefaultValues();
        mesh.ModelTransform(transform);
        return false;
    }
}