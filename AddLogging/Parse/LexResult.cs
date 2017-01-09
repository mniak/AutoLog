using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoLog.Parse
{
    internal class LexResult
    {
        public LexResult()
        {
            Flags = new List<string>();
            KeyValues = new List<KeyValuePair<string, string>>();
        }
        public List<string> Flags { get; set; }
        public List<KeyValuePair<string, string>> KeyValues { get; set; }
    }
}
