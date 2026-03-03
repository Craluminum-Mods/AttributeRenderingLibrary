
using HarmonyLib;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Vintagestory.API.Common;

namespace AttributeRenderingLibrary.HarmonyPatches;

public static partial class AttributeRedirectionPatches
{
    public static void Apply(Harmony harmony, ILogger logger)
    {
        foreach(var (assembly, assemblyDefinition) in GetTargetAssemblies())
        {
            PatchAssembly(harmony, assembly, assemblyDefinition, logger);
        }
    }

    private static void PatchAssembly(Harmony harmony, Assembly assembly, AssemblyDefinition assemblyDefinition, ILogger logger)
    {
        var targetMethods = FindTargetMethods(assembly, assemblyDefinition, logger);

        foreach(var targetMethod in targetMethods)
        {
            try
            {
                harmony.Patch(targetMethod, transpiler: new HarmonyMethod(AccessTools.Method(typeof(AttributeRedirectionPatches), nameof(TranspileAttributeLoading))));

                #if DEBUG
                    logger.Notification("Redirected itemStack attribute loading in: {0}", targetMethod.FullDescription());
                #endif
            }
            catch(Exception ex)
            {
                logger.Error("Failed to patch method {0} in assembly {1}: {2}", targetMethod.FullDescription(), assembly.FullName, ex);
            }
        }
    }

    private static IEnumerable<CodeInstruction> TranspileAttributeLoading(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        matcher.Start().Advance(1);

        var matchTarget = new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(CollectibleObject), nameof(CollectibleObject.Attributes)));
        matcher.MatchEndForward(matchTarget);

        while (matcher.IsValid)
        {
            TryReplaceAttributeLoading(matcher);

            matcher.MatchEndForward(matchTarget);
        }
        

        return matcher.InstructionEnumeration();
    }

    private static void TryReplaceAttributeLoading(CodeMatcher matcher)
    {
        var previousInstruction = matcher.Instructions()[matcher.Pos - 1];
        if(!LoadsCollectibleFromItemStack(previousInstruction))
        {
            matcher.Advance(1);
            return;
        }

        matcher.Advance(-1);
        var previousLabels = matcher.Instruction.labels;
        matcher.RemoveInstructions(2);
        matcher.Insert(
            new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(ItemStack), nameof(ItemStack.ItemAttributes)))
            {
                labels = previousLabels
            }
        );
    }

    private static bool LoadsCollectibleFromItemStack(CodeInstruction instruction) => instruction.operand is MethodBase {
        DeclaringType.Name: nameof(ItemStack), 
        Name: "get_Collectible" or "get_Block" or "get_Item" 
    };
}
