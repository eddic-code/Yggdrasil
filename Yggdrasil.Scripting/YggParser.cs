using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Yggdrasil.Scripting
{
    public class YggParser
    {
        public List<Type> NodeTypes { get; set; } = new List<Type>();

        public BaseConditional CompileConditional<T>(string functionText)
        {
            var stateTypeName = typeof(T).FullName?.Replace("+", ".");
            var namespaceName = typeof(T).GetTypeInfo().Namespace;

            var scriptText = $@"using Yggdrasil.Scripting; 
                                using {namespaceName};
                                public class DerivedConditional : BaseConditional
                                {{
                                    public override bool Execute(object baseState){{ var state = ({stateTypeName})baseState; return {functionText}; }} 
                                }}";

            var references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(T).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(GetType().GetTypeInfo().Assembly.Location)
            };

            var options = ScriptOptions.Default.AddReferences(references);
            var script = CSharpScript.Create<bool>(scriptText, options);
            var compilation = script.GetCompilation();

            byte[] compiledAssembly;
            using (var output = new MemoryStream())
            {
                var emitResult = compilation.Emit(output);

                if (!emitResult.Success)
                {
                    throw new Exception("Compilation failed.");
                }

                compiledAssembly = output.ToArray();
            }

            var assembly = Assembly.Load(compiledAssembly);
            var entryType = assembly.GetTypes().First(t => t.Name == "DerivedConditional");
            var instance = (BaseConditional)Activator.CreateInstance(entryType);

            return instance;
        }

        public BaseDynamicConditional CompileDynamicConditional(string functionText)
        {
            var scriptText = $@"using Yggdrasil.Scripting; 
                                using System.Dynamic;
                                public class DerivedDynamicConditional : BaseDynamicConditional
                                {{
                                    public override bool Execute(dynamic state){{ return {functionText}; }} 
                                }}";

            var references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(Microsoft.CSharp.RuntimeBinder.RuntimeBinderException).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Runtime.CompilerServices.DynamicAttribute).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(GetType().GetTypeInfo().Assembly.Location)
            };

            var options = ScriptOptions.Default.AddReferences(references);
            var script = CSharpScript.Create<bool>(scriptText, options);
            var compilation = script.GetCompilation();

            byte[] compiledAssembly;
            using (var output = new MemoryStream())
            {
                var emitResult = compilation.Emit(output);

                if (!emitResult.Success)
                {
                    throw new Exception("Compilation failed.");
                }

                compiledAssembly = output.ToArray();
            }

            var assembly = Assembly.Load(compiledAssembly);
            var entryType = assembly.GetTypes().First(t => t.Name == "DerivedDynamicConditional");
            var instance = (BaseDynamicConditional)Activator.CreateInstance(entryType);

            return instance;
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
            var scriptRegex = new Regex("[>=]*[\\s\n\r]*'[\\s\n\r]*(.*?)[\\s\n\r]*'[\\s\n\r]*<*");
            var innerOpening = new Regex(">[\\s\n\r]*'[\\s\n\r]*");
            var innerClosing = new Regex("[\\s\n\r]*'[\\s\n\r]*<");
            var attributeOpening = new Regex("=[\\s\n\r]*'[\\s\n\r]*");
            var scriptMatches = scriptRegex.Matches(text);

            foreach (var m in scriptMatches)
            {
                var match = (Match)m;
                var matchText = match.Value;
                var innerText = match.Groups[1].Value;
                var cleanText = matchText;

                if (innerOpening.IsMatch(cleanText)) { cleanText = innerOpening.Replace(cleanText, ">"); }
                if (innerClosing.IsMatch(cleanText)) { cleanText = innerClosing.Replace(cleanText, "<"); }
                if (attributeOpening.IsMatch(cleanText)) { cleanText = attributeOpening.Replace(cleanText, "='"); }

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
