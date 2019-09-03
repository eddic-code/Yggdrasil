using System.Collections.Generic;

namespace Yggdrasil.Scripting
{
    public class YggParserConfig
    {
        public List<string> NodeTypes { get; set; } = new List<string>();

        public List<string> ScriptUsings { get; set; } = new List<string>();
    }
}
