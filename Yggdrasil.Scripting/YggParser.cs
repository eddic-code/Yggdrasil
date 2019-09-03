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
using Yggdrasil.ScriptTypes;

namespace Yggdrasil.Scripting
{
    public class YggParser
    {
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

            NodeTypes = _config.NodeTypes
                .Select(Type.GetType)
                .ToList();
        }

        public List<Type> NodeTypes { get; }

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
            var text = new string(ConvertToXml(path).ToArray());

            document.LoadXml(text);

            return document;
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
    }
}