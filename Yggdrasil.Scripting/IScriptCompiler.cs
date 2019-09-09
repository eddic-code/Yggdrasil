using System.Collections.Generic;

namespace Yggdrasil.Scripting
{
    public interface IScriptCompiler
    {
        YggCompilation Compile<TState>(IEnumerable<string> namespaces, 
            IEnumerable<string> referenceAssemblyPaths,
            List<ScriptedFunctionDefinition> definitions);
    }
}
