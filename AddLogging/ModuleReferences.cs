using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoLog
{
    class ModuleReferences
    {
        public ModuleReferences(ModuleDefinition module)
        {
            var logManager = module.Import(typeof(NLog.LogManager)).Resolve();
            var ilogger = module.Import(typeof(NLog.ILogger)).Resolve();

            var getCurrentClassLogger = logManager.Methods.Single(x => x.Name == "GetCurrentClassLogger" && !x.HasParameters);
            var logerror = ilogger.Methods.Single(x =>
                x.Name == "Error"
#if NLOG_LT_4311
                && x.Parameters.Count == 3
#else
                && x.Parameters.Count == 2
#endif
                && x.Parameters[0].ParameterType.FullName == typeof(Exception).FullName
                && x.Parameters[1].ParameterType.FullName == typeof(string).FullName
#if NLOG_LT_4311
                && x.Parameters[2].ParameterType.FullName == typeof(object[]).FullName
                && x.Parameters[2].HasCustomAttributes
                && x.Parameters[2].CustomAttributes.Any(ca => ca.AttributeType.FullName == typeof(ParamArrayAttribute).FullName)
#endif
            );

            this.Ilogger = module.Import(ilogger);
            this.GetCurrentClassLogger = module.Import(getCurrentClassLogger);
            this.LogError = module.Import(logerror);
            this.Exception = module.Import(typeof(Exception));
            this.Object = module.Import(typeof(object));
        }

        public TypeReference Ilogger { get; private set; }
        public MethodReference GetCurrentClassLogger { get; private set; }
        public MethodReference LogError { get; private set; }
        public TypeReference Exception { get; private set; }
        public TypeReference Object { get; private set; }
    }
}
