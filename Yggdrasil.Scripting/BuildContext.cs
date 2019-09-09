using System.Collections.Generic;
using System.Linq;
using Yggdrasil.Coroutines;
using Yggdrasil.Nodes;

namespace Yggdrasil.Scripting
{
    public class BuildContext
    {
        public List<BuildError> Errors { get; internal set; } = new List<BuildError>();

        public YggCompilation Compilation { get; internal set; }

        public int TopmostNodeCount { get; internal set; }

        public HashSet<string> NodeTypeTags { get; set; } = new HashSet<string>();

        public List<ParserNode> ParserNodes { get; set; } = new List<ParserNode>();

        public Dictionary<string, ParserNode> TypeDefMap = new Dictionary<string, ParserNode>();

        public Node Instantiate(string guid, CoroutineManager manager)
        {
            var parserNode = ParserNodes.FirstOrDefault(p => p.Guid == guid);

            if (parserNode == null)
            {
                var error = new BuildError {Message = $"Could not find a node with GUID: {guid}."};
                Errors.Add(error);

                return null;
            }

            var node = parserNode.CreateInstance(manager, TypeDefMap, Errors);

            return node;
        }
    }
}
