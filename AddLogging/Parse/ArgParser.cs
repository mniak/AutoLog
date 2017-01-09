using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoLog.Parse
{
    internal static class ArgParser
    {
        public static ParseResult Parse(string[] args)
        {
            var result = new ParseResult();
            var lex = Lex(args);
            var kvInput = lex.KeyValues.Where(x => new[] { "i", "input" }.Contains(x.Key));
            if (kvInput.Any())
            {
                if (kvInput.Skip(1).Any())
                    return result.Fail("Parameter specified more than once: input.");

                result.Input = kvInput.Single().Value;
            }
            else return result.Fail("Missing parameter: input.");
            var kvOutput = lex.KeyValues.Where(x => new[] { "o", "output" }.Contains(x.Key));
            if (kvOutput.Any())
            {
                if (kvOutput.Skip(1).Any())
                    return result.Fail("Parameter specified more than once: output.");

                result.Output = kvOutput.Single().Value;
            }
            else return result.Fail("Missing parameter: output.");
            return result;
        }
        private static LexResult Lex(string[] args)
        {
            var lex = new LexResult();
            string previous = null;
            foreach (var arg in args)
            {
                if (arg.Length == 2 && arg.StartsWith("-") || arg.Length > 2 && arg.StartsWith("--"))
                {
                    var cleanedArg = arg.TrimStart(new[] { '-' });
                    if (previous != null)
                    {
                        lex.Flags.Add(previous);
                        previous = cleanedArg;
                    }
                    else
                    {
                        previous = cleanedArg;
                    }
                }
                else
                {
                    if (previous != null)
                    {
                        lex.KeyValues.Add(new KeyValuePair<string, string>(previous, arg));
                        previous = null;
                    }
                    else
                    {
                        previous = arg;
                    }
                }
            }
            if (previous != null)
                lex.Flags.Add(previous);

            return lex;
        }
    }
}
