#region License

// // /*
// // MIT License
// //
// // Copyright (c) 2019 eddic-code
// //
// // Permission is hereby granted, free of charge, to any person obtaining a copy
// // of this software and associated documentation files (the "Software"), to deal
// // in the Software without restriction, including without limitation the rights
// // to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// // copies of the Software, and to permit persons to whom the Software is
// // furnished to do so, subject to the following conditions:
// //
// // The above copyright notice and this permission notice shall be included in all
// // copies or substantial portions of the Software.
// //
// // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// // FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// // SOFTWARE.
// //
// // */

#endregion

using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using Yggdrasil.Behaviour;

namespace Yggdrasil.Scripting
{
    public class ParserNode
    {
        public List<ParserNode> Children = new List<ParserNode>();
        public string DeclaringTypeDef;
        public string File;
        public List<ScriptedFunctionDefinition> FunctionDefinitions = new List<ScriptedFunctionDefinition>();
        public string Guid;
        public bool IsDerivedFromTypeDef;
        public bool IsTopmost;
        public List<ScriptedFunction> ScriptedFunctions = new List<ScriptedFunction>();
        public string Tag;
        public Type Type;
        public ParserNode TypeDef;
        public XmlNode Xml;

        public Node CreateInstance(Dictionary<string, ParserNode> typeDefMap,
            List<BuildError> errors)
        {
            return IsDerivedFromTypeDef
                ? CreateTypeDefInstance(typeDefMap, errors)
                : CreateStaticTypeInstance(errors);
        }

        private Node CreateTypeDefInstance(Dictionary<string, ParserNode> typeDefMap, List<BuildError> errors)
        {
            // Find the TypeDef parser node.
            if (!typeDefMap.TryGetValue(Tag, out var parserDef))
            {
                errors.Add(ParserErrorHelper.MissingTypeDefInstance(Tag, File));
                return null;
            }

            // Create an instance from the TypeDef parser node.
            var instance = parserDef.CreateInstance(typeDefMap, errors);
            if (instance == null) { return null; }

            // Set manager and guid.
            instance.Guid = Guid;

            // Set function values.
            foreach (var function in ScriptedFunctions) { function.SetFunctionPropertyValue(instance); }

            if (instance.Children == null) { instance.Children = new List<Node>(); }

            return instance;
        }

        private Node CreateStaticTypeInstance(List<BuildError> errors)
        {
            // Create an instance using reflection.
            Node instance;

            try
            {
                var serializer = new XmlSerializer(Type);
                instance = serializer.Deserialize(new XmlNodeReader(Xml)) as Node;
            }
            catch (Exception e)
            {
                errors.Add(ParserErrorHelper.UnableToInstantiate(Tag, File, e.Message));
                return null;
            }

            if (instance == null)
            {
                errors.Add(ParserErrorHelper.CannotCastToNode(Tag, File));
                return null;
            }

            // Set manager and guid.
            instance.Guid = Guid;

            // Set function values.
            foreach (var function in ScriptedFunctions) { function.SetFunctionPropertyValue(instance); }

            if (instance.Children == null) { instance.Children = new List<Node>(); }

            return instance;
        }
    }
}