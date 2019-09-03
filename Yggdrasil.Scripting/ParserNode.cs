using System;
using System.Collections.Generic;
using System.Xml;
using Yggdrasil.Nodes;

namespace Yggdrasil.Scripting
{
    internal class ParserNode
    {
        public ulong Id;
        public string Name;
        public string TypeDef;
        public XmlNode Xml;
        public Type Type;
        public List<ParserNode> Children;

        public Node CreateInstance(Dictionary<string, ParserNode> typeDefMap)
        {
            return Type != null 
                ? CreateStaticTypeInstance(typeDefMap) 
                : CreateTypeDefInstance(typeDefMap);
        }

        private Node CreateTypeDefInstance(Dictionary<string, ParserNode> typeDefMap)
        {
            if (!typeDefMap.TryGetValue(Name, out var parserDef)) { return null; }

            var n = parserDef.CreateInstance(typeDefMap);
            if (n == null) { return null; }

            foreach (var parserChild in Children)
            {
                var child = parserChild.CreateInstance(typeDefMap);
                n.Children.Add(child);
            }

            return n;
        }

        private Node CreateStaticTypeInstance(Dictionary<string, ParserNode> typeDefMap)
        {
            if (!(Activator.CreateInstance(Type) is Node n)) { return null; }

            foreach (var parserChild in Children)
            {
                var child = parserChild.CreateInstance(typeDefMap);
                n.Children.Add(child);
            }

            return n;
        }
    }
}
