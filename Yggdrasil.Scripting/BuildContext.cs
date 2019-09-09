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

            // Uses a depth first loop instead of recursion to avoid potential stack overflows.
            var root = parserNode.CreateInstance(manager, TypeDefMap, Errors);
            var n = new InstantiationNode {Instance = root, Parser = parserNode};
            var open = new Stack<InstantiationNode>();

            open.Push(n);

            while (open.Count > 0)
            {
                var next = open.Pop();

                var children = next.Parser.IsDerivedFromTypeDef 
                    ? TypeDefMap[next.Parser.Tag].Children 
                    : next.Parser.Children;

                foreach (var parserChild in children)
                {
                    var instance = parserChild.CreateInstance(manager, TypeDefMap, Errors);
                    if (instance == null) { continue; }

                    var c = new InstantiationNode {Instance = instance, Parser = parserChild};

                    if (next.Instance.Children == null) { next.Instance.Children = new List<Node>(); }
                    next.Instance.Children.Add(instance);
                    open.Push(c);
                }
            }

            return root;
        }

        private class InstantiationNode
        {
            public Node Instance;
            public ParserNode Parser;
        }
    }
}
