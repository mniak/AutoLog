//#define NLOG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if NLOG
using NLog;
#endif

namespace ProgramaTeste
{
    class Program
    {
#if NLOG
        private static readonly NLog.ILogger logger = NLog.LogManager.GetCurrentClassLogger();
#endif
        static void Main(string[] args)
        {
            var teste = "meu testes => ";
            try
            {
                Console.WriteLine("Este é um programa");
                throw new Exception("Exeption teste");
            }
            catch (Exception ex)
            {
#if NLOG
                logger.Error(ex, "deu erro");
#endif
                Console.WriteLine(teste + "Deu um erro mas tudo bem");
                //Console.WriteLine(ex);
            }
            //----------------------------------
            Console.WriteLine();
            Console.WriteLine("--END--");
            Console.Read();
        }
    }
}
