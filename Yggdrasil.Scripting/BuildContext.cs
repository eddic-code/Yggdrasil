using System.Collections.Generic;

namespace Yggdrasil.Scripting
{
    public class BuildContext
    {
        public bool Success { get; internal set; }

        public List<BuildError> Errors { get; internal set; } = new List<BuildError>();

        public YggCompilation Compilation { get; internal set; }
    }
}
