using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Yggdrasil.Scripting
{
    public class BuildError
    {
        public bool IsCritical;
        public string Message;
        public List<string> Data = new List<string>();
        public List<Diagnostic> Diagnostics = new List<Diagnostic>();
    }
}
