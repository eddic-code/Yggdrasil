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

        private static IEnumerable<char> ConvertToXml(string path)
        {
	        const char escape = '|';

	        using (var stream = new StreamReader(path))
	        {
		        var openingCdata = "<![CDATA[".ToCharArray();
		        var closeingCdata = "]]>".ToCharArray();
		        var prev = '\0';
		        var started = false;
		        var ampersand = "&amp;".ToCharArray();
		        var lessThan = "&lt;".ToCharArray();
		        var greaterThan = "&gt;".ToCharArray();
		        var quotes = "&quot;".ToCharArray();

		        while (!stream.EndOfStream)
		        {
			        var next = (char)stream.Read();

			        if (prev == '"' && next == escape)
			        {
				        yield return prev;

				        var n0 = '\0';
				        var started0 = false;

				        while (!stream.EndOfStream)
				        {
					        var n = (char)stream.Read();

					        if (n0 == escape && n == '"') { yield return n; break; }

					        if (n == '&')
					        {
						        if (n0 != '&') { yield return n0; }
						        foreach (var c in ampersand) { yield return c; }
						        started0 = false;
						        n0 = n;
					        }
					        else if (n == '"')
					        {
						        if (n0 != '"') { yield return n0; }
						        foreach (var c in quotes) { yield return c; }
						        started0 = false;
						        n0 = n;
					        }
					        else if (n == '<')
					        {
						        if (n0 != '<') { yield return n0; }
						        foreach (var c in lessThan) { yield return c; }
						        started0 = false;
						        n0 = n;
					        }
					        else if (n == '>')
					        {
						        if (n0 != '>') { yield return n0; }
						        foreach (var c in greaterThan) { yield return c; }
						        started0 = false;
						        n0 = n;
					        }
					        else if (started0)
					        {
						        yield return n0;
						        n0 = n;
					        }
					        else
					        {
						        n0 = n;
						        started0 = true;
					        }
				        }

				        started = false;
			        }
			        else if (prev == '>' && next == escape)
			        {
				        yield return prev;

				        foreach (var c in openingCdata) { yield return c; }

				        started = false;
			        }
			        else if (prev == escape && next == '<')
			        {
				        foreach (var c in closeingCdata) { yield return c; }

				        started = false;
				        yield return next;
			        }
			        else if (started) { yield return prev; prev = next; }
			        else { prev = next; started = true; }
		        }
	        }
        }

        private static string ConvertToXml2(string path)
        {
            var text = File.ReadAllText(path);

            var innerScriptRegex = new Regex(">\\s*<\\s*#\\s*>\\s*(.*?)\\s*<\\s*/#\\s*>");
            var innerScriptMatches = innerScriptRegex.Matches(text);

            foreach (var m in innerScriptMatches)
            {
                var match = (Match)m;
                var innerText = match.Value;

                var cleanText = Regex.Replace(innerText, ">\\s*<\\s*#\\s*>\\s*", "");
                cleanText = Regex.Replace(cleanText, "\\s*<\\s*/#\\s*>", "");

                cleanText = cleanText.Replace("&", "&amp;")
                    .Replace("\"", "&quot;")
                    .Replace("<", "&lt;")
                    .Replace(">", "&gt;")
                    .Insert(0, ">");

                text = text.Replace(innerText, cleanText);
            }

            var attributeScriptRegex = new Regex("\\s*=\\s*<\\s*#\\s*>\\s*(.*?)\\s*<\\s*/#\\s*>");
            var attributeScriptMatches = attributeScriptRegex.Matches(text);

            foreach (var m in attributeScriptMatches)
            {
                var match = (Match)m;
                var innerText = match.Value;

                var cleanText = Regex.Replace(innerText, "\\s*=\\s*<\\s*#\\s*>\\s*", "");
                cleanText = Regex.Replace(cleanText, "\\s*<\\s*/#\\s*>", "");

                cleanText = cleanText.Replace("&", "&amp;")
                    .Replace("\"", "&quot;")
                    .Replace("<", "&lt;")
                    .Replace(">", "&gt;")
                    .Insert(0, "\"");

                cleanText = cleanText.Insert(cleanText.Length - 1, "\"");

                text = text.Replace(innerText, cleanText);
            }

            return text;
        }
    }
}
