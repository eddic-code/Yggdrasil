using System.Collections.Generic;

namespace Yggdrasil.Scripting
{
    public class BuildError
    {
        public bool IsCritical;
        public string Message;
        public string Target;
        public string SecondTarget;
        public List<string> Data = new List<string>();
    }
}
