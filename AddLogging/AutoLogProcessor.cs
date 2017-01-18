using Mono.Cecil;
using Mono.Cecil.Cil;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoLog
{
    public static class AutoLogProcessor
    {
        const string PREFERRABLE_LOGGER_FIELD_NAME = "logger";
        private static ILogger logger = LogManager.GetCurrentClassLogger();

        public static void AddLoggingToAssembly(string input, string output, string searchDirectory = null)
        {
            using (var input2 = File.OpenRead(input))
            using (var output2 = File.OpenWrite(output))
            {
                AddLoggingToAssembly(input2, output2, searchDirectory);
            }
        }
        public static void AddLoggingToAssembly(Stream input, Stream output, string searchDirectory = null)
        {
            var @params = new ReaderParameters();
            if (searchDirectory != null)
            {
                var resolver = new DefaultAssemblyResolver();
                resolver.AddSearchDirectory(searchDirectory);
                @params.AssemblyResolver = resolver;
            }
            var asm = AssemblyDefinition.ReadAssembly(input, @params);
            var module = asm.MainModule;

            ProcessModule(module, t => t.FullName == "Sda.Accounts.Services.AccountService", m => m.Name == "SaveJoinedUser");
            logger.Info($"Saving Assembly {asm.Name}");
            asm.Write(output);
        }

        private static void ProcessModule(ModuleDefinition module, Func<TypeDefinition, bool> typeFilter = null, Func<MethodDefinition, bool> methodFilter = null)
        {
            logger.Info($"Processing Module '{module.Name}'");

            var refs = new ModuleReferences(module);

            // ------------------------------------------------------
            logger.Info($"Processing Module '{module.Name}'");

            var types = module.Types.Where(x => !x.IsEnum);
            if (typeFilter != null)
                types = types.Where(typeFilter);

            foreach (var type in types)
            {
                logger.Info($"Processing Type '{type.Name}'");

                var loggerField = AddStaticLoggerField(type, refs);
                IEnumerable<MethodDefinition> methods = type.Methods;
                if (methodFilter != null)
                    methods = methods.Where(methodFilter);
                foreach (var method in methods)
                {
                    ProcessMethod(refs, method, loggerField);
                }
            }
        }

        private static void ProcessMethod(ModuleReferences refs, MethodDefinition method, FieldDefinition loggerField)
        {
            logger.Info($"Processing Method '{method.Name}'");
            if (method.Body == null)
                return;
            int shift = AddCatchLogger(refs, method, loggerField);
            ReplaceShortInstructions(method);
        }

        private static void ReplaceShortInstructions(MethodDefinition method)
        {
            var convDict = new Dictionary<OpCode, OpCode>() {
                {OpCodes.Br_S       , OpCodes.Br       },
                {OpCodes.Brfalse_S  , OpCodes.Brfalse  },
                {OpCodes.Brtrue_S   , OpCodes.Brtrue   },
                {OpCodes.Beq_S      , OpCodes.Beq      },
                {OpCodes.Bge_S      , OpCodes.Bge      },
                {OpCodes.Bgt_S      , OpCodes.Bgt      },
                {OpCodes.Ble_S      , OpCodes.Ble      },
                {OpCodes.Blt_S      , OpCodes.Blt      },
                {OpCodes.Bne_Un_S   , OpCodes.Bne_Un   },
                {OpCodes.Bge_Un_S   , OpCodes.Bge_Un   },
                {OpCodes.Bgt_Un_S   , OpCodes.Bgt_Un   },
                {OpCodes.Ble_Un_S   , OpCodes.Ble_Un   },
                {OpCodes.Blt_Un_S   , OpCodes.Blt_Un   },
                {OpCodes.Leave_S    , OpCodes.Leave    },
            };
            foreach (var ins in method.Body.Instructions)
            {
                if (ins.OpCode.OperandType == OperandType.ShortInlineBrTarget && ins.Operand is Instruction)
                {
                    var target = ins.Operand as Instruction;
                    var dif = target.Offset - ins.Offset;
                    //if (dif + shift > byte.MaxValue && convDict.ContainsKey(ins.OpCode))
                    {
                        var newOpCode = convDict[ins.OpCode];
                        ins.OpCode = newOpCode;
                    }
                }
            }
        }

        private static int AddCatchLogger(ModuleReferences refs, MethodDefinition method, FieldDefinition loggerField)
        {
            var body = method.Body;
            var catches = body.ExceptionHandlers.Where(x => x.HandlerType == ExceptionHandlerType.Catch);
            int result = 0;
            foreach (var exh in catches)
            {
                var hstart = exh.HandlerStart;
                if (exh.CatchType.FullName == typeof(object).FullName)
                    exh.CatchType = refs.Exception;

                var il = body.GetILProcessor();
                int slot = 0;
                if (hstart.OpCode == OpCodes.Pop)
                {
                    slot = method.Body.Variables.Count();
                    method.Body.Variables.Add(new VariableDefinition(refs.Exception));
                    var newhstart = InstructionHelper.CreateStloc(method, slot);
                    il.Replace(hstart, newhstart);
                    hstart = newhstart;
                    exh.TryEnd = newhstart;
                    exh.HandlerStart = newhstart;
                }
                else
                {
                    slot = InstructionHelper.GetSlot(hstart);
                }
                var instructions = new[] {
                    Instruction.Create(OpCodes.Ldsfld, loggerField),
                    InstructionHelper.CreateLdloc(method, slot),
                    Instruction.Create(OpCodes.Ldstr, "AUTOLOG EXCEPTION"),
                    #if NLOG_LT_4311
                    Instruction.Create(OpCodes.Ldc_I4_0),
                    Instruction.Create(OpCodes.Newarr, refs.Object),
                    #endif
                    Instruction.Create(OpCodes.Callvirt, refs.LogError),
                };
                il.Body.MaxStackSize += 10;
                il.InjectAfter(hstart, instructions);

                result += instructions.Sum(x => x.GetSize());
            }
            return result;
        }
        private static FieldDefinition AddStaticLoggerField(TypeDefinition type, ModuleReferences refs)
        {
            var loggerField = new FieldDefinition(DetermineLoggerFieldName(type), FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly, refs.Ilogger);
            type.Fields.Add(loggerField);

            FieldReference refLoggerField = loggerField;
            if (type.HasGenericParameters)
            {
                var declaringType = new GenericInstanceType(loggerField.DeclaringType);
                foreach (var parameter in loggerField.DeclaringType.GenericParameters)
                {
                    declaringType.GenericArguments.Add(parameter);
                }
                refLoggerField = new FieldReference(loggerField.Name, loggerField.FieldType, declaringType);
            }

            var initr = new[] {
                Instruction.Create(OpCodes.Call, refs.GetCurrentClassLogger),
                Instruction.Create(OpCodes.Stsfld, refLoggerField),
            };

            var cctor = type.Methods.SingleOrDefault(x => x.Name == ".cctor");
            if (cctor != null)
            { // If there is already a private constructor
                var first = cctor.Body.Instructions.FirstOrDefault();
                var il = cctor.Body.GetILProcessor();
                if (first != null)
                    il.InjectBefore(first, initr);
                else
                    cctor.Body.Instructions.AddMany(initr);
            }
            else
            { // If there is NO private constructor
                var methodAttributes = MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.Static;
                cctor = new MethodDefinition(".cctor", methodAttributes, type.Module.TypeSystem.Void);
                type.Methods.Add(cctor);
                cctor.Body.Instructions.AddMany(initr);
                cctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            }
            return loggerField;
        }
        private static bool IsValidNewMemberName(TypeDefinition type, string name)
        {
            var memberNames = type.Fields.Select(x => x.Name)
                            .Union(type.Properties.Select(x => x.Name))
                            .Union(type.Methods.Select(x => x.Name))
                            .Union(type.GenericParameters.Select(x => x.Name));
            var isValid = !memberNames.Contains(name);
            return isValid;
        }
        private static string DetermineLoggerFieldName(TypeDefinition type)
        {
            if (IsValidNewMemberName(type, PREFERRABLE_LOGGER_FIELD_NAME))
                return PREFERRABLE_LOGGER_FIELD_NAME;

            var secondaryName = "_" + PREFERRABLE_LOGGER_FIELD_NAME;
            if (IsValidNewMemberName(type, secondaryName))
                return secondaryName;

            for (int i = 0; true; i++)
            {
                var name = secondaryName + i.ToString();
                if (IsValidNewMemberName(type, name))
                    return name;
            }
        }
    }
}
