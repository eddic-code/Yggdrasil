using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
            var scriptText = $@"using Yggdrasil.Benchmarks; 
                                public class DerivedConditional : BaseConditional
                                {{
                                    public override bool Execute(object baseState){{ var state = (T)baseState; return {functionText}; }} 
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
            var scriptText = $@"using Yggdrasil.Benchmarks;
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
    }
}
