using Mono.Cecil;
using Mono.Cecil.Cil;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoLog
{
    class Program
    {
        static void Main(string[] args)
        {
            //LogManager.DisableLogging();
            //RodaExemplo();
            RodaNaPastaInput();

            //-----------------------
            //Console.WriteLine();
            //Console.WriteLine("--END--");
            //Console.ReadLine();
        }

        private static void RodaNaPastaInput()
        {
            var di = new DirectoryInfo("Input");
            var dlls = di.EnumerateFiles("Sda.*.dll").Skip(1).Take(1);
#if PARALLEL
            Parallel.ForEach(dlls, dll =>
#else
            foreach (var dll in dlls)
#endif
            {
                using (var input = File.OpenRead(dll.FullName))
                using (var output = File.OpenWrite(Path.Combine("Output", dll.Name)))
                {
                    AutoLogWorker.AddLoggingToAssembly(input, output, dll.DirectoryName);
                }
            }
#if PARALLEL
            );
#endif
        }

        private static void RodaExemplo()
        {
            var input = @"..\..\..\ProgramaTeste\bin\debug\ProgramaTeste.exe";
            var output = "Patched.exe";
            AutoLogWorker.AddLoggingToAssembly(input, output);
            Process.Start(output);
        }

    }
}
