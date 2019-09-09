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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using Yggdrasil.Attributes;
using Yggdrasil.Coroutines;
using Yggdrasil.Nodes;

namespace Yggdrasil.Scripting
{
    public class YggParser
    {
        private const string GuidAttribute = "Guid";
        private const string TypeDefAttribute = "TypeDef";

        private static readonly Regex _scriptRegex = new Regex("[>=]*[\\s\n\r]*`[\\s\n\r]*(.*?)[\\s\n\r]*`[\\s\n\r]*<*",
            RegexOptions.Singleline | RegexOptions.Compiled);

        private static readonly Regex _innerOpening = new Regex(">[\\s\n\r]*`[\\s\n\r]*", RegexOptions.Compiled);
        private static readonly Regex _innerClosing = new Regex("[\\s\n\r]*`[\\s\n\r]*<", RegexOptions.Compiled);
        private static readonly Regex _attributeOpening = new Regex("=[\\s\n\r]*`[\\s\n\r]*", RegexOptions.Compiled);
        private static readonly Regex _attributeClosing = new Regex("[\\s\n\r]*`", RegexOptions.Compiled);

        private readonly YggParserConfig _config;

        public YggParser(YggParserConfig config = null)
        {
            _config = config ?? new YggParserConfig();

            BaseNodeMap = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => _config.NodeTypeAssemblies.Contains(a.GetName().Name))
                .SelectMany(a => a.GetTypes())
                .Where(t => typeof(Node).IsAssignableFrom(t) && !t.IsAbstract && t != typeof(Node))
                .GroupBy(t => t.Name)
                .Select(g => g.First())
                .ToDictionary(t => t.Name, t => t);
        }

        public Dictionary<string, Type> BaseNodeMap { get; }

        public XmlDocument LoadFromFile(string path)
        {
            var document = new XmlDocument();
            var text = ConvertToXml(path);

            document.LoadXml(text);

            return document;
        }

        public BuildContext BuildFromFiles<TState>(params string[] files)
        {
            var context = new BuildContext();
            context.TypeDefMap = new Dictionary<string, ParserNode>();

            // Load all xml documents from files.
            var documents = LoadDocuments(files, context.Errors);

            // Extract all behaviour node tags from the xml documents.
            context.NodeTypeTags = GetAllBehaviourNodeTagNames(documents.Values);

            // Create parser nodes from xml documents.
            var parserNodesMap = CreateParserNodes(documents, context.TypeDefMap, context.Errors, context.NodeTypeTags);
            if (parserNodesMap == null) { return context; }

            context.ParserNodes = parserNodesMap.Values.ToList();

            // Determine underlying types.
            if (!TryResolveTypes(context.ParserNodes, context.TypeDefMap, context.Errors)) { return context; }

            // Create function definitions.
            var functionDefinitions = GetFunctionDefinitions(context.ParserNodes, context.NodeTypeTags);
            if (functionDefinitions == null) { return context; }

            // Compile scripted functions.
            context.Compilation = YggCompiler.Compile<TState>(_config, functionDefinitions);
            context.Errors.AddRange(context.Compilation.Errors);
            if (context.Errors.Any(e => e.IsCritical)) { return context; }

            // Set scripted functions on parser nodes.
            foreach (var parserNode in context.ParserNodes)
            {
                if (!context.Compilation.FunctionMap.TryGetValue(parserNode.Guid, out var functions)) { continue; }
                parserNode.ScriptedFunctions = functions;
            }

            // Instantiate nodes once to test them.
            foreach (var parserNode in context.ParserNodes.Where(p => p.IsTopmost))
            {
                var node = parserNode.CreateInstance(null, context.TypeDefMap, context.Errors);
                if (context.Errors.Any(e => e.IsCritical)) { return context; }

                if (node != null) { context.TopmostNodeCount += 1; }
            }

            return context;
        }

        private HashSet<string> GetAllBehaviourNodeTagNames(IEnumerable<XmlDocument> documents)
        {
            var nodeTypes = new HashSet<string>(BaseNodeMap.Keys);

            foreach (var document in documents)
            {
                var elements = document.SelectNodes("//*[@TypeDef]");
                if (elements == null || elements.Count <= 0) { continue; }

                foreach (var element in elements)
                {
                    var node = (XmlNode) element;
                    if (node.NodeType != XmlNodeType.Element) { continue; }

                    var typeDef = GetAttribute(node, TypeDefAttribute)?.Value;
                    if (string.IsNullOrEmpty(typeDef)) { continue; }

                    nodeTypes.Add(typeDef);
                }
            }

            return nodeTypes;
        }

        private void Expand(string file, XmlDocument document, Dictionary<string, ParserNode> typeDefMap,
            List<BuildError> errors, Dictionary<string, ParserNode> guids, HashSet<string> nodeTags)
        {
            var rootXml = document.SelectSingleNode("/__Main");
            var root = new ParserNode {Xml = rootXml, Tag = "__Main", File = file};
            var open = new Stack<ParserNode>();

            open.Push(root);

            while (open.Count > 0)
            {
                var next = open.Pop();
                if (!next.Xml.HasChildNodes) { continue; }

                next.Children = new List<ParserNode>();

                foreach (var xmlChild in GetChildren(next.Xml).ToList())
                {
                    if (xmlChild.NodeType != XmlNodeType.Element) { continue; }

                    // Determine if the element is a behaviour node.
                    var tag = xmlChild.Name;
                    if (!nodeTags.Contains(tag)) { continue; }

                    // Remove this child element from the parent's xml.
                    next.Xml.RemoveChild(xmlChild);

                    // Determine its guid. Check repetitions. Remove guid attribute from the xml.
                    var guidAttribute = GetAttribute(xmlChild, GuidAttribute);
                    var guid = guidAttribute?.Value;
                    if (guidAttribute != null) { xmlChild.Attributes?.Remove(guidAttribute); }

                    if (string.IsNullOrWhiteSpace(guid)) { guid = GetRandomGuid(guids); }
                    else if (guids.TryGetValue(guid, out var prev))
                    {
                        errors.Add(new BuildError
                        {
                            IsCritical = true, Message = $"Repeated node GUID: {guid}", Target = prev.File,
                            SecondTarget = file
                        });
                        continue;
                    }

                    // Search for a type definition attribute on the node. Remove typedef attribute from the xml.
                    var typeDefAttriute = GetAttribute(xmlChild, TypeDefAttribute);
                    var typeDef = typeDefAttriute?.Value;
                    if (typeDefAttriute != null) { xmlChild.Attributes?.Remove(typeDefAttriute); }

                    // Check TypeDef repetitions.
                    if (!string.IsNullOrWhiteSpace(typeDef) && typeDefMap.TryGetValue(typeDef, out var prevTypeDef))
                    {
                        errors.Add(new BuildError
                        {
                            IsCritical = true, Message = $"Repeated TypeDef identifier: {typeDef}",
                            Target = prevTypeDef.File, SecondTarget = file
                        });
                        continue;
                    }

                    // Create the parser node.
                    var n = new ParserNode
                    {
                        Xml = xmlChild,
                        Tag = tag,
                        File = file,
                        Guid = guid,
                        DeclaringTypeDef = typeDef,
                        IsTopmost = next == root
                    };

                    // Try resolve the node type from a compiled type.
                    if (BaseNodeMap.TryGetValue(tag, out var type))
                    {
                        n.Type = type;
                        n.IsDerivedFromTypeDef = false;
                    }
                    else { n.IsDerivedFromTypeDef = true; }

                    guids[guid] = n;
                    if (!string.IsNullOrWhiteSpace(typeDef)) { typeDefMap[typeDef] = n; }

                    next.Children.Add(n);
                    open.Push(n);
                }
            }
        }

        private static string ConvertToXml(string path)
        {
            var text = File.ReadAllText(path);
            var scriptMatches = _scriptRegex.Matches(text);

            foreach (var m in scriptMatches)
            {
                var match = (Match) m;
                var matchText = match.Value;
                var innerText = match.Groups[1].Value;
                var cleanText = matchText;

                if (_innerOpening.IsMatch(cleanText)) cleanText = _innerOpening.Replace(cleanText, ">");
                if (_innerClosing.IsMatch(cleanText)) cleanText = _innerClosing.Replace(cleanText, "<");
                if (_attributeOpening.IsMatch(cleanText))
                {
                    cleanText = _attributeOpening.Replace(cleanText, "=\"");
                    cleanText = _attributeClosing.Replace(cleanText, "\"");
                }

                var cleanInnerText = innerText.Replace("&", "&amp;")
                    .Replace("\"", "&quot;")
                    .Replace("<", "&lt;")
                    .Replace(">", "&gt;");

                cleanText = cleanText.Replace(innerText, cleanInnerText);
                text = text.Replace(matchText, cleanText);
            }

            text = text.Insert(0, "<__Main>");

            return text.Insert(text.Length, "</__Main>");
        }

        private static string GetRandomGuid(Dictionary<string, ParserNode> guids)
        {
            var guid = Guid.NewGuid().ToString().Replace("-", "");
            while (guids.ContainsKey(guid)) { guid = Guid.NewGuid().ToString().Replace("-", ""); }

            return guid;
        }

        private static XmlAttribute GetAttribute(XmlNode node, string attributeName)
        {
            return GetAttributes(node).FirstOrDefault(a => a.Name == attributeName);
        }

        private static IEnumerable<XmlAttribute> GetAttributes(XmlNode xml)
        {
            if (xml.Attributes == null) { yield break; }

            foreach (var a in xml.Attributes) { yield return (XmlAttribute) a; }
        }

        private static IEnumerable<XmlNode> GetChildren(XmlNode xml)
        {
            foreach (var a in xml.ChildNodes) { yield return (XmlNode) a; }
        }

        private Dictionary<string, ParserNode> CreateParserNodes(Dictionary<string, XmlDocument> documents,
            Dictionary<string, ParserNode> typeDefMap, List<BuildError> errors, HashSet<string> nodeTypeTags)
        {
            var parserNodes = new Dictionary<string, ParserNode>();

            foreach (var kvp in documents)
            {
                var file = kvp.Key;
                var document = kvp.Value;

                // Extract nodes from document.
                Expand(file, document, typeDefMap, errors, parserNodes, nodeTypeTags);

                // Early exit if there are critical errors.
                if (errors.Count(e => e.IsCritical) > 0) { return null; }
            }

            return parserNodes;
        }

        private Dictionary<string, XmlDocument> LoadDocuments(string[] files, List<BuildError> errors)
        {
            var documents = new Dictionary<string, XmlDocument>();

            foreach (var file in files)
            {
                // Check file exists.
                if (!File.Exists(file))
                {
                    errors.Add(new BuildError {Message = "File does not exit.", Target = file});
                    continue;
                }

                // Try load file into xml.
                XmlDocument xmlDocument;
                try { xmlDocument = LoadFromFile(file); }
                catch (Exception e)
                {
                    errors.Add(new BuildError {Message = $"Could not load file. {e.Message}", Target = file});
                    continue;
                }

                documents[file] = xmlDocument;
            }

            return documents;
        }

        private static bool TryResolveTypes(List<ParserNode> parserNodes, Dictionary<string, ParserNode> typeDefMap,
            List<BuildError> errors)
        {
            var criticalError = false;

            // Determine TypeDef for each node.
            foreach (var node in parserNodes)
            {
                if (!node.IsDerivedFromTypeDef) { continue; }

                if (typeDefMap.TryGetValue(node.Tag, out var typeDef)) { node.TypeDef = typeDef; }
                else
                {
                    var error = new BuildError {Message = "Missing TypeDef."};
                    error.Target = node.Tag;
                    error.SecondTarget = node.File;
                    error.IsCritical = true;
                    criticalError = true;
                }
            }

            // Return early on critical errors.
            if (criticalError) { return false; }

            // Resolve underlying types for all nodes derived from a TypeDef.
            // One TypeDef can be derived from another TypeDef, which means we must resolve them with recursion (or loop).
            var open = parserNodes
                .Where(n => n.Type == null)
                .ToList();

            while (open.Count > 0)
            {
                var next = open.FirstOrDefault(n => n.TypeDef.Type != null);

                if (next == null)
                {
                    var error = new BuildError {Message = "TypeDefs could not be resolved fully resolved."};
                    error.Data.AddRange(open.Select(n => n.Tag));
                    errors.Add(error);
                    return false;
                }

                next.Type = next.TypeDef.Type;
                open.Remove(next);
            }

            return true;
        }

        private static List<ScriptedFunctionDefinition> GetFunctionDefinitions(List<ParserNode> parserNodes, HashSet<string> nodeTypeTags)
        {
            var functionDefinitions = new List<ScriptedFunctionDefinition>();

            foreach (var parserNode in parserNodes)
            {
                var attType = typeof(ScriptedFunctionAttribute);

                var scriptedFunctionProperties = parserNode.Type
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(prop => Attribute.IsDefined(prop, attType));

                foreach (var property in scriptedFunctionProperties)
                {
                    var functionAttribute = GetAttribute(parserNode.Xml, property.Name);

                    var functionElement = nodeTypeTags.Contains(property.Name) 
                        ? null 
                        : GetChildren(parserNode.Xml).FirstOrDefault(n => n.Name == property.Name);

                    string functionText;
                    if (functionAttribute != null) { functionText = functionAttribute.Value; }
                    else if (functionElement != null) { functionText = functionElement.Value; }
                    else { continue; }

                    var definition = new ScriptedFunctionDefinition
                    {
                        Guid = parserNode.Guid,
                        FunctionProperty = property,
                        FunctionText = functionText
                    };

                    functionDefinitions.Add(definition);
                }
            }

            return functionDefinitions;
        }
    }
}