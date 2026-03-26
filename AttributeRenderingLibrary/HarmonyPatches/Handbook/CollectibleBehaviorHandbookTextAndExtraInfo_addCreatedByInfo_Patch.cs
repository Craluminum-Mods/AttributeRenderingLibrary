using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace AttributeRenderingLibrary.HarmonyPatches.Handbook;

[HarmonyPatchCategory("Client")]
[HarmonyPatch(typeof(CollectibleBehaviorHandbookTextAndExtraInfo), "addCreatedByInfo")]
public static class CollectibleBehaviorHandbookTextAndExtraInfo_addCreatedByInfo_Patch
{
    const int TinyPadding = 2;
    const int SmallPadding = 7;

    [HarmonyPostfix]
    public static void Postfix(
        CollectibleBehaviorHandbookTextAndExtraInfo __instance,
        ref bool __result,
        ICoreClientAPI capi,
        ItemStack[] allStacks,
        ActionConsumable<string> openDetailPageFor,
        ItemStack stack,
        List<RichTextComponentBase> components,
        float marginTop,
        List<ItemStack> containers,
        List<ItemStack> fuels,
        List<ItemStack> molds,
        List<ItemStack> anvils,
        bool haveText)
    {
        var verticalSpaceSmall = new ClearFloatTextComponent(capi, SmallPadding);
        var verticalSpace = new ClearFloatTextComponent(capi, TinyPadding + 1);

        Dictionary<string, List<ItemStack>> groundstoredprocessables = [];

        foreach (var val in allStacks)
        {
            DummySlot inSlot = new DummySlot(val);
            if (val.Collectible.GetCollectibleBehavior<AttributeRenderingLibrary.CollectibleBehaviorGroundStoredProcessable>(true) is { } gsp)
            {
                BlockDropItemStack[]? processedStacks = gsp.GetProcessedStacks(inSlot);
                ItemStack? rStack = gsp.GetRemainingItem(inSlot)?.ResolvedItemStack;
                string handbookCreatedByTitle = gsp.GetHandbookCreatedByTitle(inSlot) ?? "";

                if (processedStacks != null)
                {
                    foreach (var processedStack in processedStacks)
                    {
                        if (processedStack?.ResolvedItemstack is not { } pStack ||
                            !stack.Equals(capi.World, pStack, GlobalConstants.IgnoredStackAttributes))
                        {
                            continue;
                        }

                        if (groundstoredprocessables.TryGetValue(handbookCreatedByTitle, out var gspList))
                        {
                            addToListUniquely(capi, gspList, val);
                        }
                        else groundstoredprocessables[handbookCreatedByTitle] = [val];
                    }
                }

                if (rStack != null)
                {
                    if (stack.Equals(capi.World, rStack, GlobalConstants.IgnoredStackAttributes))
                    {
                        if (groundstoredprocessables.TryGetValue(handbookCreatedByTitle, out var gspList))
                        {
                            addToListUniquely(capi, gspList, val);
                        }
                        else groundstoredprocessables[handbookCreatedByTitle] = [val];
                    }
                }
            }
        }

        if (groundstoredprocessables.Count > 0)
        {
            foreach ((string processTitle, List<ItemStack>? processables) in groundstoredprocessables)
            {
                if (processables.Count <= 0) continue;

                components.Add(verticalSpace);
                verticalSpace = verticalSpaceSmall;
                CollectibleBehaviorHandbookTextAndExtraInfo.AddSubHeading(components, capi, openDetailPageFor, processTitle, null);
                __instance.AddSlideShowComponent(components, capi, processables, openDetailPageFor, false);
            }
        }

        void addToListUniquely(ICoreClientAPI capi, List<ItemStack> list, ItemStack entry)
        {
            if (!list.Any(s => s.Equals(capi.World, entry, GlobalConstants.IgnoredStackAttributes)))
            {
                list.Add(entry);
            }
        }
    }
}