using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Yggdrasil.ScriptTypes;

public class YggParser
    {
        private static readonly Regex _scriptRegex = new Regex("[>=]*[\\s\n\r]*`[\\s\n\r]*(.*?)[\\s\n\r]*`[\\s\n\r]*<*", RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex _innerOpening = new Regex(">[\\s\n\r]*`[\\s\n\r]*", RegexOptions.Compiled);
        private static readonly Regex _innerClosing = new Regex("[\\s\n\r]*`[\\s\n\r]*<", RegexOptions.Compiled);
        private static readonly Regex _attributeOpening = new Regex("=[\\s\n\r]*`[\\s\n\r]*", RegexOptions.Compiled);
        private static readonly Regex _attributeClosing = new Regex("[\\s\n\r]*`", RegexOptions.Compiled);

        public (BaseConditional Conditional, ImmutableArray<Diagnostic> Errors) CompileConditional<TState>(string functionText)
        {
            var stateTypeName = typeof(TState).FullName?.Replace("+", ".");
            var namespaceName = typeof(TState).GetTypeInfo().Namespace;

            var scriptText = $@"using Yggdrasil.ScriptTypes;

                                public class DerivedConditional : BaseConditional
                                {{
                                    public override bool Execute(object baseState){{ var state = ({stateTypeName})baseState; return {functionText}; }} 
                                }}";

            if (namespaceName != null) { scriptText = scriptText.Insert(0, $"using {namespaceName};"); }

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var netstandard = assemblies.First(a => a.GetName().Name == "netstandard");

            var references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(netstandard.Location),
                MetadataReference.CreateFromFile(typeof(TState).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(BaseConditional).GetTypeInfo().Assembly.Location)
            };

            var options = ScriptOptions.Default.AddReferences(references)
                .WithEmitDebugInformation(false);

            var script = CSharpScript.Create<bool>(scriptText, options);
            var compilation = script.GetCompilation();

            byte[] compiledAssembly;
            using (var output = new MemoryStream())
            {
                var emitResult = compilation.Emit(output);
                if (!emitResult.Success) { return (null, emitResult.Diagnostics); }

                compiledAssembly = output.ToArray();
            }

            var assembly = Assembly.Load(compiledAssembly);
            var entryType = assembly.GetTypes().First(t => t.Name == "DerivedConditional");
            var instance = (BaseConditional)Activator.CreateInstance(entryType);

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
                var match = (Match)m;
                var matchText = match.Value;
                var innerText = match.Groups[1].Value;
                var cleanText = matchText;

                if (_innerOpening.IsMatch(cleanText)) { cleanText = _innerOpening.Replace(cleanText, ">"); }
                if (_innerClosing.IsMatch(cleanText)) { cleanText = _innerClosing.Replace(cleanText, "<"); }
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
