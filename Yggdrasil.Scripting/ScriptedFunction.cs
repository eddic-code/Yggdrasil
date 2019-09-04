using System.Collections.Generic;

namespace Yggdrasil.Scripting
{
    public class ScriptedFunction
    {
        public string PropertyName { get; set; }

        public string FunctionName { get; set; }

        public string BuilderName { get; set; }

        public string ScriptText { get; set; }

        public string FunctionText { get; set; }

        public HashSet<string> Usings { get; set; } = new HashSet<string>();

        public HashSet<string> References { get; set; } = new HashSet<string>();
    }
}
