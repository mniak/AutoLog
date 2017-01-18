using AutoLog.Parse;
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
            var parsed = ArgParser.Parse(args);
            var di = new DirectoryInfo(Path.GetDirectoryName(parsed.Input));
            var dlls = di.EnumerateFiles(Path.GetFileName(parsed.Input));
#if PARALLEL
            Parallel.ForEach(dlls, dll =>
#else
            foreach (var dll in dlls)
#endif
            {
                using (var input = File.OpenRead(dll.FullName))
                using (var output = File.OpenWrite(Path.Combine(parsed.Output, dll.Name)))
                {
                    AutoLogProcessor.AddLoggingToAssembly(input, output, dll.DirectoryName);
                }
            }
#if PARALLEL
            );
#endif


            //-----------------------
            //Console.WriteLine();
            //Console.WriteLine("--END--");
            //Console.ReadLine();
        }



        //private static void RodaExemplo()
        //{
        //    var input = @"..\..\..\ProgramaTeste\bin\debug\ProgramaTeste.exe";
        //    var output = "Patched.exe";
        //    AutoLogWorker.AddLoggingToAssembly(input, output);
        //    Process.Start(output);
        //}

    }
}
