using AttributeRenderingLibrary.Utility.HarmonyTools;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using static HarmonyLib.Code;

namespace AttributeRenderingLibrary.HarmonyPatches;

public static class AttributeRedirectionPatch
{
    private static ILogger? Logger;
    public static void ScanAndApply(Harmony harmony, ILogger? logger = null)
    {
        Logger = logger;
        var targetMethods = DynamicTargetTools.GetMethodsUsing(
            methods:    [ AccessTools.PropertyGetter(typeof(ItemStack), nameof(ItemStack.ItemAttributes)) ],
            fields:     [ AccessTools.Field(typeof(CollectibleObject), nameof(CollectibleObject.Attributes)) ],
            exclude:    [ typeof(AttributeRedirectionPatch).Assembly ],
            logger
        );
        
        var transpilerMethod = AccessTools.Method(typeof(AttributeRedirectionPatch), nameof(Transpiler));
        foreach(var method in targetMethods)
        {
            try
            {
                harmony.Patch(method, transpiler: transpilerMethod);
            }
            catch(Exception ex)
            {
                logger?.Error("Failed to apply attribute redirection to {0}.{1}, exception: {2}", method.DeclaringType?.FullName ?? "unknown", method.Name, ex);
            }
        }
        Logger = null;
    }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase __originalMethod)
    {
        var matcher = new CodeMatcher(instructions, generator);

        var itemAttributesGetter = AccessTools.PropertyGetter(typeof(ItemStack), nameof(ItemStack.ItemAttributes));
        var attributesField = AccessTools.Field(typeof(CollectibleObject), nameof(CollectibleObject.Attributes));

        var simulator = new StackSimulator(
            matcher,
            __originalMethod,
            shouldTrace: entry => typeof(IItemStack).IsAssignableFrom(entry.Type),
            shouldTraceAdvance: (entry, trace) => 
                typeof(CollectibleObject).IsAssignableFrom(entry.Type) ||
                entry.Instruction.Calls(itemAttributesGetter) || 
                entry.Instruction.LoadsField(attributesField)
        );
        
        try
        {
            simulator.Simulate();
        }
        catch(Exception ex)
        {
            Logger?.Error("Possibly uncaught attribute due to stack simulation failure for {0}.{1}, exception: {2}", __originalMethod.DeclaringType?.FullName ?? "unknown", __originalMethod.Name, ex);
        }

        if(simulator.Traces.Count <= 0) return instructions;

        var indexer = AccessTools.IndexerGetter(typeof(JsonObject), [typeof(string)]);
        var keyExists = AccessTools.Method(typeof(JsonObject), nameof(JsonObject.KeyExists));
        var isTrue = AccessTools.Method(typeof(JsonObject), nameof(JsonObject.IsTrue));

        var uniqueTraces = simulator.Traces.Values.Distinct().ToList();
        HashSet<CodeInstruction> handledInstructions = [];

        foreach(var trace in uniqueTraces)
        {
            foreach(var usage in trace.Usage)
            {
                if(handledInstructions.Contains(usage)) continue;

                if (usage.Calls(indexer))
                {
                    HandleIntercept(matcher, trace, usage, AccessTools.Method(typeof(AttributeRedirectionPatch), nameof(GetAttribute)));
                }
                else if (usage.Calls(keyExists))
                {
                    HandleIntercept(matcher, trace, usage, AccessTools.Method(typeof(AttributeRedirectionPatch), nameof(GetKeyExists)));
                }
                else if (usage.Calls(isTrue))
                {
                    HandleIntercept(matcher, trace, usage, AccessTools.Method(typeof(AttributeRedirectionPatch), nameof(GetIsTrue)));
                }

                handledInstructions.Add(usage);
            }
        }

        //TODO maybe also intercept token usage?

        return matcher.InstructionEnumeration();
    }

    private static void HandleIntercept(CodeMatcher matcher, Trace trace, CodeInstruction usage, MethodInfo method)
    {
        var loadInstruction = trace.FindOrCreateLoadInstruction(matcher);

        matcher.Start().MatchStartForward(new CodeMatch(instruction => instruction == usage));

        usage.opcode = OpCodes.Call;
        usage.operand = method;

        matcher.Insert(loadInstruction);
    }

    private static JsonObject GetAttribute(JsonObject collectibleAttributes, string attributeKey, IItemStack? itemStack)
    {
        if(itemStack is null) return collectibleAttributes[attributeKey];
        
        //TODO decide based on attributes
        return collectibleAttributes[attributeKey];
    }

    private static bool GetKeyExists(JsonObject collectibleAttributes, string attributeKey, IItemStack? itemStack)
    {
        if(itemStack is null) return collectibleAttributes.KeyExists(attributeKey);
        
        //TODO decide based on attributes
        return collectibleAttributes.KeyExists(attributeKey);
    }

    private static bool GetIsTrue(JsonObject collectibleAttributes, string attributeKey, IItemStack? itemStack)
    {
        if (itemStack is null) return collectibleAttributes.IsTrue(attributeKey);

        //TODO decide based on attributes
        return collectibleAttributes.IsTrue(attributeKey);
    }
}
