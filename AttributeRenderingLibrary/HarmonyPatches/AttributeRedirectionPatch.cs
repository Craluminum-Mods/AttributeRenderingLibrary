using AttributeRenderingLibrary.Utility.HarmonyTools;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

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
            catch (FileNotFoundException)
            {
                //Ignore missing method/field exceptions, as these would be caused by mod dependencies not being present (and in the case of optional dependencies this is expected and not an issue)
            }
            catch (Exception ex)
            {
                logger?.Error("Failed to apply attribute redirection to {0}.{1}, exception: {2}", method.DeclaringType?.FullName ?? "unknown", method.Name, ex);
            }
        }
        Logger = null;
    }

    internal static bool ShouldTrace(StackSimulator simulator, StackEntry entry)
    {
        if(typeof(IItemStack).IsAssignableFrom(entry.Type)) return true;
        if(typeof(CollectibleObject).IsAssignableFrom(entry.Type)) return entry.Instruction.opcode == OpCodes.Ldarg_0 || (entry.Instruction.operand is FieldInfo field && typeof(CollectibleBehavior).IsAssignableFrom(field.DeclaringType));
        return false;
    }

    internal static bool ShouldTraceAdvance(StackSimulator simulator, StackEntry entry, Trace trace)
    {
        return (typeof(CollectibleObject).IsAssignableFrom(entry.Type) && typeof(IItemStack).IsAssignableFrom(trace.Origin.Type)) ||
            entry.Instruction.Calls(AccessTools.PropertyGetter(typeof(ItemStack), nameof(ItemStack.ItemAttributes))) ||
            entry.Instruction.LoadsField(AccessTools.Field(typeof(CollectibleObject), nameof(CollectibleObject.Attributes)));
    }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase __originalMethod)
    {
        var matcher = new CodeMatcher(instructions, generator);

        var simulator = new StackSimulator(
            matcher,
            __originalMethod,
            shouldTrace: ShouldTrace,
            shouldTraceAdvance: ShouldTraceAdvance
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
        MethodInfo[] targetMethods = [indexer, keyExists, isTrue];

        var uniqueTraces = simulator.Traces.Values
            .Where(trace => trace.Usage.Any(usage => targetMethods.Any(target => usage.Calls(target))))
            .Distinct()
            .ToList();
        if(uniqueTraces.Count <= 0) return instructions;

        if(uniqueTraces.Any(trace => typeof(IItemStack).IsAssignableFrom(trace.Origin.Type)))
        {
            //We can only support fallback to parameters if there is no direct link, otherwise we might accidentally load the parameter for a different stack than intended
            uniqueTraces.RemoveAll(trace => !typeof(IItemStack).IsAssignableFrom(trace.Origin.Type));
        }
        HashSet<CodeInstruction> handledInstructions = [];

        foreach(var trace in uniqueTraces)
        {
            foreach(var usage in trace.Usage)
            {
                if(handledInstructions.Contains(usage)) continue;

                if (usage.Calls(indexer))
                {
                    HandleIntercept(simulator, trace, usage, AccessTools.Method(typeof(CollectibleAttributeExtensions), nameof(CollectibleAttributeExtensions.GetAttribute)));
                }
                else if (usage.Calls(keyExists))
                {
                    HandleIntercept(simulator, trace, usage, AccessTools.Method(typeof(CollectibleAttributeExtensions), nameof(CollectibleAttributeExtensions.GetKeyExists)));
                }
                else if (usage.Calls(isTrue))
                {
                    HandleIntercept(simulator, trace, usage, AccessTools.Method(typeof(CollectibleAttributeExtensions), nameof(CollectibleAttributeExtensions.GetIsTrue)));
                }

                handledInstructions.Add(usage);
            }
        }

        //TODO maybe also intercept token usage?

        return matcher.InstructionEnumeration();
    }

    private static void HandleIntercept(StackSimulator simulator, Trace trace, CodeInstruction usage, MethodInfo method)
    {
        CodeInstruction? loadInstruction = trace.LastFoundLoadInstruction;
        var matcher = simulator.Matcher;
        if(loadInstruction is null)
        {
            if (typeof(IItemStack).IsAssignableFrom(trace.Origin.Type))
            {
                loadInstruction = trace.FindOrCreateLoadInstruction(matcher);
            }
            else if (typeof(CollectibleObject).IsAssignableFrom(trace.Origin.Type))
            {
                for(int paramIndex = 0; paramIndex < simulator.parameters.Length; paramIndex++)
                {
                    var paramType = simulator.parameters[paramIndex];
                    if (!typeof(IItemStack).IsAssignableFrom(paramType) && !typeof(ItemSlot).IsAssignableFrom(paramType)) continue;

                    matcher.Start();
                    matcher.DeclareLocal(typeof(IItemStack), out var itemStackLocal);
                    matcher.InsertAndAdvance(CodeInstruction.LoadArgument(paramIndex));
                    if (typeof(ItemSlot).IsAssignableFrom(paramType))
                    {
                        matcher.InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AttributeRedirectionPatch), nameof(GetItemStackFromItemSlot))));
                    }
                    matcher.InsertAndAdvance(CodeInstruction.StoreLocal(itemStackLocal.LocalIndex));
                    loadInstruction = trace.LastFoundLoadInstruction = CodeInstruction.LoadLocal(itemStackLocal.LocalIndex);

                    break;
                }
            }
        }
        if(loadInstruction is null) return; //Failed to find ItemStack/ItemSlot

        loadInstruction = loadInstruction.Clone();
        matcher.Start().MatchStartForward(new CodeMatch(instruction => instruction == usage));

        loadInstruction.labels = usage.labels;

        usage.labels = [];
        usage.opcode = OpCodes.Call;
        usage.operand = method;

        matcher.Insert(loadInstruction);
    }

    private static ItemStack? GetItemStackFromItemSlot(ItemSlot? itemSlot) => itemSlot?.Itemstack;
}
