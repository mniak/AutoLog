using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoLog.Parse
{
    internal class ParseResult
    {
        public string Input { get; set; }
        public string Output { get; set; }

        public bool Failed { get; set; }
        public string FailureReason { get; set; }

        public ParseResult Fail(string reason)
        {
            this.Failed = true;
            this.FailureReason = reason;
            return this;
        }
    }
}
