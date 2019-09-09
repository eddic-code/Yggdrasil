using System.Collections.Generic;

namespace Yggdrasil.Scripting
{
    public class YggCompilation
    {
        public object Builder;
        public Dictionary<string, List<ScriptedFunction>> FunctionMap = new Dictionary<string, List<ScriptedFunction>>();
        public List<BuildError> Errors = new List<BuildError>();
    }
}
