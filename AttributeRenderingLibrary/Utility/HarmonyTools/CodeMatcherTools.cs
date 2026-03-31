using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace AttributeRenderingLibrary.Utility.HarmonyTools;

public static class CodeMatcherTools
{
    public static CodeInstruction GetOrCreateLoadInstruction(this CodeMatcher matcher, Type type)
    {
        HashSet<OpCode> loadCodes = [
            OpCodes.Ldloc_0,
            OpCodes.Ldloc_1,
            OpCodes.Ldloc_2,
            OpCodes.Ldloc_3,
            
            OpCodes.Ldloc_S,
            OpCodes.Ldloc,
            
            OpCodes.Ldarg_0,
            OpCodes.Ldarg_1,
            OpCodes.Ldarg_2,
            OpCodes.Ldarg_3,
            
            OpCodes.Ldarg_S,
            OpCodes.Ldarg
        ];

        if(matcher.Operand is LocalBuilder || loadCodes.Contains(matcher.Opcode)) return new CodeInstruction(matcher.Opcode, matcher.Operand);
        
        matcher.DeclareLocal(type, out var localBuilder);
        
        var storeInstruction = CodeInstruction.StoreLocal(localBuilder.LocalIndex);
        storeInstruction.labels = matcher.Labels;
        matcher.Labels = [];

        matcher.InsertAndAdvance(
            new CodeInstruction(OpCodes.Dup),
            storeInstruction
        );

        return CodeInstruction.LoadLocal(localBuilder.LocalIndex);
    }
}
