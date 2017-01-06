using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoLog
{
    internal static class InstructionHelper
    {
        public static Instruction CreateStloc(MethodDefinition method, int slot)
        {
            Instruction result;
            switch (slot)
            {
                case 0:
                    result = Instruction.Create(OpCodes.Stloc_0);
                    break;
                case 1:
                    result = Instruction.Create(OpCodes.Stloc_1);
                    break;
                case 2:
                    result = Instruction.Create(OpCodes.Stloc_2);
                    break;
                case 3:
                    result = Instruction.Create(OpCodes.Stloc_3);
                    break;
                default:
                    result = Instruction.Create(OpCodes.Stloc_S, method.Body.Variables[slot]);
                    break;
            }
            return result;
        }

        public static Instruction CreateLdloc(MethodDefinition method, int slot)
        {
            Instruction result;
            switch (slot)
            {
                case 0:
                    result = Instruction.Create(OpCodes.Ldloc_0);
                    break;
                case 1:
                    result = Instruction.Create(OpCodes.Ldloc_1);
                    break;
                case 2:
                    result = Instruction.Create(OpCodes.Ldloc_2);
                    break;
                case 3:
                    result = Instruction.Create(OpCodes.Ldloc_3);
                    break;
                default:
                    result = Instruction.Create(OpCodes.Ldloc_S, method.Body.Variables[slot]);
                    break;
            }
            return result;
        }
        public static int GetSlot(Instruction instruction)
        {
            if (instruction.OpCode == OpCodes.Stloc_0) return 0;
            else if (instruction.OpCode == OpCodes.Stloc_1) return 1;
            else if (instruction.OpCode == OpCodes.Stloc_2) return 2;
            else if (instruction.OpCode == OpCodes.Stloc_3) return 3;
            else if (instruction.OpCode == OpCodes.Stloc_S) return (instruction.Operand as VariableDefinition).Index;
            else if (instruction.OpCode == OpCodes.Stloc) return (instruction.Operand as VariableDefinition).Index;
            else throw new ArgumentException("The instruction opcode must be a stloc");
        }

        public static void AddMany(this Collection<Instruction> instructions, IEnumerable<Instruction> toAdd)
        {
            foreach (var instr in toAdd)
            {
                instructions.Add(instr);
            }
        }
        public static void AddMany(this Collection<Instruction> instructions, params Instruction[] toAdd)
        {
            AddMany(instructions, toAdd.AsEnumerable());
        }
        public static void InjectAfter(this ILProcessor ilProcessor, Instruction target, IEnumerable<Instruction> toInject)
        {
            foreach (var instr in toInject.Reverse())
                ilProcessor.InsertAfter(target, instr);
        }
        public static void InjectAfter(this ILProcessor ilProcessor, Instruction target, params Instruction[] toInject)
        {
            InjectAfter(ilProcessor, target, toInject.AsEnumerable());
        }

        public static void InjectBefore(this ILProcessor ilProcessor, Instruction target, IEnumerable<Instruction> toInject)
        {
            foreach (var instr in toInject)
                ilProcessor.InsertBefore(target, instr);
        }
        public static void InjectBefore(this ILProcessor ilProcessor, Instruction target, params Instruction[] toInject)
        {
            InjectBefore(ilProcessor, target, toInject.AsEnumerable());
        }
    }
}
