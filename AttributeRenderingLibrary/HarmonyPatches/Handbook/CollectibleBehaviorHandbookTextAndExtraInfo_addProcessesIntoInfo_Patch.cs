using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace AttributeRenderingLibrary.HarmonyPatches.Handbook;

[HarmonyPatchCategory("Client")]
[HarmonyPatch(typeof(CollectibleBehaviorHandbookTextAndExtraInfo), "addProcessesIntoInfo")]
public static class CollectibleBehaviorHandbookTextAndExtraInfo_addProcessesIntoInfo_Patch
{
    const int TinyIndent = 2;

    [HarmonyPostfix]
    public static void Postfix(
        CollectibleBehaviorHandbookTextAndExtraInfo __instance,
        ref bool __result,
        ICoreClientAPI capi,
        ActionConsumable<string> openDetailPageFor,
        ItemStack stack,
        List<RichTextComponentBase> components,
        float marginTop,
        float marginBottom,
        List<ItemStack> containers,
        List<ItemStack> fuels,
        bool haveText)
    {
        if (stack.Collectible.GetCollectibleBehavior<AttributeRenderingLibrary.CollectibleBehaviorGroundStoredProcessable>(true) is { } gsp)
        {
            DummySlot inSlot = new DummySlot(stack);
            List<ItemStack?>? processedStacks = [.. gsp.GetProcessedStacks(inSlot)?.Select(pstack => pstack.ResolvedItemstack) ?? []];
            ItemStack? rStack = gsp.GetRemainingItem(inSlot)?.ResolvedItemStack;
            if (rStack != null) processedStacks.Insert(0, rStack);
            string? extraTooltipText = null;
            ItemStack[]? toolStacks = null;
            if (gsp.GetTool(inSlot) is { } tool)
            {
                extraTooltipText = "\n\n<font color=\"orange\">" + Lang.Get("processing-requires-tool-" + tool.ToString()!.ToLowerInvariant()) + "</font>";
                toolStacks = ObjectCacheUtil.GetToolStacks(capi, tool);
            }

            if (processedStacks.Count > 0)
            {
                CollectibleBehaviorHandbookTextAndExtraInfo.AddHeading(components, capi, gsp.GetHandbookProcessIntoTitle(inSlot), ref haveText);
                var indent = TinyIndent;

                while (processedStacks.Count > 0)
                {
                    ItemStack? pstack = processedStacks[0];
                    processedStacks.RemoveAt(0);

                    if (pstack == null) continue;

                    components.Add(new SlideshowItemstackTextComponent(capi, pstack, processedStacks, 40, EnumFloat.Inline, (cs) => openDetailPageFor(getPageCodeForStack(capi, cs)))
                    {
                        ShowStackSize = true,
                        PaddingLeft = indent,
                        ExtraTooltipText = extraTooltipText
                    });
                    indent = 0;

                    if (toolStacks == null) continue;

                    components.Add(new SlideshowItemstackTextComponent(capi, toolStacks, 24, EnumFloat.Left, (cs) => openDetailPageFor(getPageCodeForStack(capi, cs)))
                    {
                        renderOffset = { X = -(float)GuiElement.scaled(17), Z = 100 },
                        ShowTooltip = false
                    });
                }

                components.Add(new ClearFloatTextComponent(capi, marginBottom));
            }
        }

        string getPageCodeForStack(ICoreClientAPI capi, ItemStack stack)
        {
            return stack.Collectible.GetCollectibleInterface<IHandBookPageCodeProvider>()?.HandbookPageCodeForStack(capi.World, stack)
                ?? GuiHandbookItemStackPage.PageCodeForStack(stack);
        }
    }
}