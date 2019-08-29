﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using Microsoft.CodeAnalysis;
using Yggdrasil.Nodes;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Yggdrasil.Serialization
{
    public class YggCompiler
    {
        public Node Compile(XmlDocument document)
        {
            return null;
        }

        public Delegate DynamicStateCompiledConditional(string functionText)
        {
            var scriptText = $"using System.Dynamic; public static class YggEntry {{ public static bool Conditional(dynamic state){{ return {functionText}; }} }}";

            var references = new List<MetadataReference>{
                MetadataReference.CreateFromFile(typeof(Microsoft.CSharp.RuntimeBinder.RuntimeBinderException).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Runtime.CompilerServices.DynamicAttribute).GetTypeInfo().Assembly.Location)};

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
            var entryType = assembly.GetTypes().First(t => t.Name == "YggEntry");

            var method = entryType.GetMethod("Conditional");
            if (method == null) { throw new Exception(); }

            var function = Delegate.CreateDelegate(typeof(Func<dynamic, bool>), method);

            return function;
        }
    }
}
