using System.Collections.Generic;

namespace Yggdrasil.Scripting
{
    public class YggCompilation
    {
        public Dictionary<string, List<ScriptedFunction>> GuidFunctionMap = new Dictionary<string, List<ScriptedFunction>>();
        public List<BuildError> Errors = new List<BuildError>();
    }
}
