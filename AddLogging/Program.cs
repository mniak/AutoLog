using Mono.Cecil;
using Mono.Cecil.Cil;
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
            var dlls = di.EnumerateFiles("Sda.*.dll");
            Action<FileInfo> runForDll = dll =>
            {
                using (var input = File.OpenRead(dll.FullName))
                using (var output = File.OpenWrite(Path.Combine("Output", dll.Name)))
                {
                    AutoLogWorker.AddLoggingToAssembly(input, output, dll.DirectoryName);
                }
            };

            //Parallel.ForEach(dlls, runForDll);
            foreach (var dll in dlls)
            {
                runForDll(dll);
            }
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
