//#define PARALLEL
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
    public static class AutoLogWorker
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

            ProcessModule(module);

            asm.Write(output);
        }

        private static void ProcessModule(ModuleDefinition module)
        {
            logger.Info($"Processing Module '{module.Name}'");

            var refs = new ModuleReferences(module);

            // ------------------------------------------------------
            logger.Info($"Processing Module '{module.Name}'");

            var types = module.Types.Where(x => !x.IsEnum);
#if PARALLEL
            Parallel.ForEach(module.Types, type =>
#else
            foreach (var type in module.Types)
#endif
            {
                logger.Info($"Processing Type '{type.Name}'");

                var loggerField = AddStaticLoggerField(type, refs);
#if PARALLEL
                Parallel.ForEach(type.Methods, method =>
#else
                foreach (var method in type.Methods)
#endif
                {
                    logger.Info($"Processing Method '{method.Name}'");
                    if (method.Body == null)
#if PARALLEL
                        return;
#else
                        continue;
#endif
                    //AddCatchLogger(loggerField, method, method.Body, refs);
                }
#if PARALLEL
                );
#endif
            }
#if PARALLEL
            );
#endif
        }

        private static void AddCatchLogger(FieldDefinition loggerField, MethodDefinition method, MethodBody body, ModuleReferences refs)
        {
            var catches = body.ExceptionHandlers.Where(x => x.HandlerType == ExceptionHandlerType.Catch);
#if PARALLEL
            Parallel.ForEach(catches, exh =>
#else
            foreach (var exh in catches)
#endif

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

                il.InjectAfter(hstart,
                    Instruction.Create(OpCodes.Ldsfld, loggerField),
                    InstructionHelper.CreateLdloc(method, slot),
                    Instruction.Create(OpCodes.Ldstr, "AUTOLOG EXCEPTION"),
#if NLOG_LT_4311
                    Instruction.Create(OpCodes.Ldc_I4_0),
                    Instruction.Create(OpCodes.Newarr, refs.Object),
#endif
                Instruction.Create(OpCodes.Callvirt, refs.LogError));

            }
#if PARALLEL
            );
#endif
        }
        private static FieldDefinition AddStaticLoggerField(TypeDefinition type, ModuleReferences refs)
        {
            var loggerField = new FieldDefinition(DetermineLoggerFieldName(type), FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly, refs.Ilogger);
            type.Fields.Add(loggerField);

            var initr = new[] {
                Instruction.Create(OpCodes.Call, refs.GetCurrentClassLogger),
                Instruction.Create(OpCodes.Stsfld, loggerField),
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
            return !memberNames.Contains(name);
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
