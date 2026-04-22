using HarmonyLib;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Vintagestory.API.Client;
using Vintagestory.GameContent;

namespace AttributeRenderingLibrary.HarmonyPatches.Handbook;

[HarmonyPatch(typeof(GuiDialogHandbook), nameof(GuiDialogHandbook.FilterItems))]
public static class GuiDialogHandbook_FilterItems_Patch
{
    [HarmonyPrefix]
    public static bool Prefix(GuiDialogHandbook __instance, ref List<IFlatListItem> ___shownHandbookPages, ref GuiComposer ___overviewGui, ref bool ___loadingPagesAsync, ref string ___currentSearchText, string ___currentCatgoryCode, ref List<GuiHandbookPage> ___allHandbookPages, double ___listHeight)
    {
        List<IFlatListItem> shownHandbookPages = [];
        ref var overviewGui = ref ___overviewGui;
        ref var loadingPagesAsync = ref ___loadingPagesAsync;
        ref var currentSearchText = ref ___currentSearchText;
        ref var allHandbookPages = ref ___allHandbookPages;
        var currentCatgoryCode = ___currentCatgoryCode;
        var listHeight = ___listHeight;
        
        if (!loadingPagesAsync)
        {
            string text = currentSearchText ?? "";
            Regex regex = GuiDialogHandbook.RegexFromSearchText(text);
            Regex strictRegex = GuiDialogHandbook.RegexFromSearchText(text, strict: true);
            List<WeightedHandbookPage> weightedPages = new List<WeightedHandbookPage>();
            allHandbookPages.ForEach(delegate (GuiHandbookPage page)
            {

                #region Handbook search include and exclude logic
                if (page is GuiHandbookItemStackPage { Stack: not null } stackPage)
                {
                    if (stackPage.Stack.Collectible?.GetCollectibleInterface<IHandbookARL>() is { } handbookARL && handbookARL.ExcludeFromHandbookSearch(stackPage.Stack))
                    {
                        return;
                    }
                }
                #endregion


                if ((currentCatgoryCode == null || !(page.CategoryCode != currentCatgoryCode)) && !page.IsDuplicate)
                {
                    PageText pageText = page.GetPageText();
                    int num = CountMatches(__instance, pageText.Title ?? "", regex);
                    int strictTitleMatches = CountMatches(__instance, pageText.Title ?? "", strictRegex);
                    int num2 = CountMatches(__instance, pageText.Text ?? "", regex);
                    if (num > 0 || num2 > 0)
                    {
                        weightedPages.Add(new WeightedHandbookPage
                        {
                            Page = page,
                            TitleMatches = num,
                            StrictTitleMatches = strictTitleMatches,
                            TitleLength = (pageText.Title?.Length ?? 0),
                            TextMatches = num2,
                            SearchWeight = 1f + page.SearchWeightOffset
                        });
                    }
                }
            });
            if (text.Length > 0)
            {
                weightedPages.Sort(delegate (WeightedHandbookPage a, WeightedHandbookPage b)
                {
                    int num = b.StrictTitleMatches - a.StrictTitleMatches;
                    if (num != 0)
                    {
                        return num;
                    }

                    int num2 = b.TitleMatches - a.TitleMatches;
                    if (num2 != 0)
                    {
                        return num2;
                    }

                    int num3 = b.SearchWeight.CompareTo(a.SearchWeight);
                    if (num3 != 0)
                    {
                        return num3;
                    }

                    int num4 = a.TitleLength - b.TitleLength;
                    return (num4 != 0) ? num4 : (b.TextMatches - a.TextMatches);
                });
            }

            weightedPages.ForEach(delegate (WeightedHandbookPage page)
            {
                shownHandbookPages.Add(page.Page);
            });
        }

        ___shownHandbookPages = shownHandbookPages;

        GuiElementFlatList flatList = overviewGui.GetFlatList("stacklist");
        flatList.CalcTotalHeight();
        overviewGui.GetScrollbar("scrollbar").SetHeights((float)listHeight, (float)flatList.insideBounds.fixedHeight);
        return false;
    }

    private static int CountMatches(GuiDialogHandbook dialog, string text, Regex regex)
    {
        return dialog.CallMethod<int>("CountMatches", text, regex);
    }
}