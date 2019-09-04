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
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CSharp.RuntimeBinder;
using Yggdrasil.Coroutines;
using Yggdrasil.Nodes;
using Yggdrasil.ScriptTypes;

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

        public (BaseConditional Conditional, ImmutableArray<Diagnostic> Errors) CompileConditional<TState>(
            string functionText)
        {
            var stateTypeName = typeof(TState).FullName?.Replace("+", ".");
            var namespaceName = typeof(TState).GetTypeInfo().Namespace;

            var scriptText = $@"using Yggdrasil.ScriptTypes;

                                public class DerivedConditional : BaseConditional
                                {{
                                    public override bool Execute(object baseState){{ var state = ({stateTypeName})baseState; return {functionText}; }} 
                                }}";

            if (namespaceName != null) scriptText = scriptText.Insert(0, $"using {namespaceName};");
            foreach (var name in _config.ScriptUsings) scriptText = scriptText.Insert(0, $"using {name};");

            var defaultReferences = new[]
            {
                typeof(TState).GetTypeInfo().Assembly.Location,
                typeof(BaseConditional).GetTypeInfo().Assembly.Location
            };

            var references = _config.ReferenceAssemblyPaths
                .Union(defaultReferences)
                .Distinct()
                .Select(p => MetadataReference.CreateFromFile(p))
                .ToList();

            var options = ScriptOptions.Default.AddReferences(references);
            var script = CSharpScript.Create<bool>(scriptText, options);
            var compilation = script.GetCompilation();

            byte[] compiledAssembly;
            using (var output = new MemoryStream())
            {
                var emitResult = compilation.Emit(output);
                if (!emitResult.Success) return (null, emitResult.Diagnostics);

                compiledAssembly = output.ToArray();
            }

            var assembly = Assembly.Load(compiledAssembly);
            var entryType = assembly.GetTypes().First(t => t.Name == "DerivedConditional");
            var instance = (BaseConditional) Activator.CreateInstance(entryType);

            return (instance, ImmutableArray<Diagnostic>.Empty);
        }

        public (BaseDynamicConditional Conditional, ImmutableArray<Diagnostic> Errors) CompileDynamicConditional(
            string functionText)
        {
            var scriptText = $@"using Yggdrasil.ScriptTypes;
                                using System.Dynamic;
                                public class DerivedDynamicConditional : BaseDynamicConditional
                                {{
                                    public override bool Execute(dynamic state){{ return {functionText}; }} 
                                }}";

            foreach (var name in _config.ScriptUsings) scriptText = scriptText.Insert(0, $"using {name};");

            var defaultReferences = new[]
            {
                typeof(RuntimeBinderException).GetTypeInfo().Assembly.Location,
                typeof(DynamicAttribute).GetTypeInfo().Assembly.Location,
                typeof(BaseConditional).GetTypeInfo().Assembly.Location
            };

            var references = _config.ReferenceAssemblyPaths
                .Union(defaultReferences)
                .Distinct()
                .Select(p => MetadataReference.CreateFromFile(p))
                .ToList();

            var options = ScriptOptions.Default.AddReferences(references);
            var script = CSharpScript.Create<bool>(scriptText, options);
            var compilation = script.GetCompilation();

            byte[] compiledAssembly;
            using (var output = new MemoryStream())
            {
                var emitResult = compilation.Emit(output);
                if (!emitResult.Success) return (null, emitResult.Diagnostics);

                compiledAssembly = output.ToArray();
            }

            var assembly = Assembly.Load(compiledAssembly);
            var entryType = assembly.GetTypes().First(t => t.Name == "DerivedDynamicConditional");
            var instance = (BaseDynamicConditional) Activator.CreateInstance(entryType);

            return (instance, ImmutableArray<Diagnostic>.Empty);
        }

        public XmlDocument LoadFromFile(string path)
        {
            var document = new XmlDocument();
            var text = ConvertToXml(path);

            document.LoadXml(text);

            return document;
        }

        public (List<Node> Nodes, BuildContext Context) BuildFromFile(CoroutineManager manager, string file)
        {
            var files = new List<string> {file};
            return BuildFromFiles(manager, files);
        }

        public (List<Node> Nodes, BuildContext Context) BuildFromFiles(CoroutineManager manager, List<string> files)
        {
            var context = new BuildContext();
            var output = new List<Node>();
            var parserNodes = new List<ParserNode>();
            var guids = new Dictionary<string, ParserNode>();
            var documents = new Dictionary<string, XmlDocument>();
            var typeDefMap = new Dictionary<string, ParserNode>();

            // Extract all parser nodes from files.
            foreach (var file in files)
            {
                // Check file exists.
                if (!File.Exists(file))
                {
                    context.Errors.Add(new BuildError {Message = "File does not exit.", Target = file});
                    continue;
                }

                // Try load file into xml.
                XmlDocument xmlDocument;
                try { xmlDocument = LoadFromFile(file); }
                catch (Exception e)
                {
                    context.Errors.Add(new BuildError {Message = $"Could not load file. {e.Message}", Target = file});
                    continue;
                }

                documents[file] = xmlDocument;
            }

            // Extract all behaviour node tags.
            var nodeTypeTags = GetAllBehaviourNodeTagNames(documents.Values);

            // Extract nodes from documents.
            foreach (var kvp in documents)
            {
                // Early exit if there are critical errors.
                if (context.Errors.Count(e => e.IsCritical) > 0)
                {
                    context.Success = false;
                    return (output, context);
                }

                var file = kvp.Key;
                var document = kvp.Value;

                // Extract nodes from document.
                var fileNodes = Expand(file, document, typeDefMap, context.Errors, guids, nodeTypeTags);
                parserNodes.AddRange(fileNodes);
            }

            // Instantiate nodes.
            foreach (var parserNode in parserNodes.Where(p => p.IsTopmost))
            {
                // Early exit if there are critical errors.
                if (context.Errors.Count(e => e.IsCritical) > 0)
                {
                    context.Success = false;
                    return (output, context);
                }

                var node = parserNode.CreateInstance(manager, typeDefMap, context.Errors);
                if (node != null) { output.Add(node); }
            }

            context.Success = context.Errors.Count == 0;
            return (output, context);
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

        private List<ParserNode> Expand(string file, XmlDocument document, Dictionary<string, ParserNode> typeDefMap, 
            List<BuildError> errors, Dictionary<string, ParserNode> guids, HashSet<string> nodeTags)
        {
            var rootXml = document.SelectSingleNode("/__Main");
            var root = new ParserNode {Xml = rootXml, Tag = "__Main", File = file};
            var open = new Stack<ParserNode>();
            var nodes = new List<ParserNode>();

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
                        errors.Add(new BuildError {IsCritical = true, Message = $"Repeated node GUID: {guid}", Target = prev.File, SecondTarget = file});
                        continue;
                    }

                    // Search for a type definition attribute on the node. Remove typedef attribute from the xml.
                    var typeDefAttriute = GetAttribute(xmlChild, TypeDefAttribute);
                    var typeDef = typeDefAttriute?.Value;
                    if (typeDefAttriute != null) { xmlChild.Attributes?.Remove(typeDefAttriute); }

                    // Check TypeDef repetitions.
                    if (!string.IsNullOrWhiteSpace(typeDef) && typeDefMap.TryGetValue(typeDef, out var prevTypeDef))
                    {
                        errors.Add(new BuildError {IsCritical = true, Message = $"Repeated TypeDef identifier: {typeDef}", Target = prevTypeDef.File, SecondTarget = file});
                        continue;
                    }

                    // Create the parser node.
                    var n = new ParserNode
                    {
                        Xml = xmlChild, 
                        Tag = tag, 
                        File = file, 
                        Guid = guid, 
                        TypeDef = typeDef, 
                        IsTopmost = next == root
                    };

                    // Try resolve the node type from a compiled type.
                    if (BaseNodeMap.TryGetValue(tag, out var type))
                    {
                        n.Type = type;
                    }

                    guids[guid] = n;
                    if (!string.IsNullOrWhiteSpace(typeDef)) { typeDefMap[typeDef] = n; }

                    next.Children.Add(n);
                    nodes.Add(n);
                    open.Push(n);
                }
            }

            return nodes;
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

            foreach (var a in xml.Attributes)
            {
                yield return (XmlAttribute)a;
            }
        }

        private static IEnumerable<XmlNode> GetChildren(XmlNode xml)
        {
            foreach (var a in xml.ChildNodes)
            {
                yield return (XmlNode)a;
            }
        }
    }
}