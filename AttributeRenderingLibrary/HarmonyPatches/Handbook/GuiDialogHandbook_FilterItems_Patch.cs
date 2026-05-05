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
    public static bool Prefix(
        GuiDialogHandbook __instance,
        List<IFlatListItem> ___shownHandbookPages,
        GuiComposer ___overviewGui,
        bool ___loadingPagesAsync,
        string ___currentSearchText,
        string ___currentCatgoryCode,
        List<GuiHandbookPage> ___allHandbookPages,
        double ___listHeight)
    {
        if (___overviewGui == null) return true;

        ___shownHandbookPages.Clear();

        if (!___loadingPagesAsync)
        {
            string searchText = ___currentSearchText ?? "";

            Regex regex = GuiDialogHandbook.RegexFromSearchText(searchText);
            Regex strictRegex = GuiDialogHandbook.RegexFromSearchText(searchText, true);

            List<WeightedHandbookPage> weightedPages = new List<WeightedHandbookPage>();

            foreach (var page in ___allHandbookPages)
            {
                #region Handbook search include and exclude logic
                if (page is GuiHandbookItemStackPage { Stack: not null } stackPage)
                {
                    if (stackPage.Stack.Collectible?.GetCollectibleInterface<IHandbookARL>() is { } arl && arl.ExcludeFromHandbookSearch(stackPage.Stack))
                    {
                        continue;
                    }
                }
                #endregion

                if ((___currentCatgoryCode != null && page.CategoryCode != ___currentCatgoryCode) || page.IsDuplicate)
                {
                    continue;
                }

                PageText pageText = page.GetPageText();
                int titleMatches = CountMatches(__instance, pageText.Title ?? "", regex);
                int strictTitleMatches = CountMatches(__instance, pageText.Title ?? "", strictRegex);
                int textMatches = CountMatches(__instance, pageText.Text ?? "", regex);

                if (titleMatches > 0 || textMatches > 0)
                {
                    weightedPages.Add(new WeightedHandbookPage
                    {
                        Page = page,
                        TitleMatches = titleMatches,
                        StrictTitleMatches = strictTitleMatches,
                        TitleLength = pageText.Title?.Length ?? 0,
                        TextMatches = textMatches,
                        SearchWeight = 1f + page.SearchWeightOffset
                    });
                }
            }

            if (searchText.Length > 0)
            {
                weightedPages.Sort((a, b) =>
                {
                    int strictSort = b.StrictTitleMatches - a.StrictTitleMatches;
                    if (strictSort != 0) return strictSort;

                    int titleSort = b.TitleMatches - a.TitleMatches;
                    if (titleSort != 0) return titleSort;

                    int weightSort = b.SearchWeight.CompareTo(a.SearchWeight);
                    if (weightSort != 0) return weightSort;

                    int lengthSort = a.TitleLength - b.TitleLength;
                    if (lengthSort != 0) return lengthSort;

                    return b.TextMatches - a.TextMatches;
                });
            }

            foreach (var p in weightedPages)
            {
                ___shownHandbookPages.Add(p.Page);
            }
        }

        GuiElementFlatList stacklist = ___overviewGui.GetFlatList("stacklist");
        if (stacklist != null)
        {
            stacklist.CalcTotalHeight();
            ___overviewGui.GetScrollbar("scrollbar")?.SetHeights((float)___listHeight,(float)stacklist.insideBounds.fixedHeight);
        }

        return false;
    }

    private static int CountMatches(GuiDialogHandbook dialog, string text, Regex regex)
    {
        return dialog.CallMethod<int>("CountMatches", text, regex);
    }
}