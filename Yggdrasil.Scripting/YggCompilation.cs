using System.Collections.Generic;

namespace Yggdrasil.Scripting
{
    public class YggCompilation
    {
        public object Builder;
        public List<string> Usings = new List<string>();
        public List<string> References = new List<string>();
        public Dictionary<string, List<ScriptedFunction>> FunctionMap = new Dictionary<string, List<ScriptedFunction>>();
        public List<BuildError> Errors = new List<BuildError>();
    }
}
