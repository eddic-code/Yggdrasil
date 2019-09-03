using System.Collections.Generic;

namespace Yggdrasil.Scripting
{
    public class BuildContext
    {
        public bool Success { get; internal set; }

        public List<BuildError> Errors { get; internal set; } = new List<BuildError>();
    }
}
