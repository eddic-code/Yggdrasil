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
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CSharp.RuntimeBinder;

namespace Yggdrasil.Scripting
{
    public class YggCompiler : IScriptCompiler
    {
        private static readonly Regex _returnStatement =
            new Regex("return[\\s\n\r]+.+[\\s\n\r]*;", RegexOptions.Compiled);

        private static readonly HashSet<Type> _supportedScriptedFunctionTypes =
            new HashSet<Type> {typeof(Action), typeof(Action<>), typeof(Func<>), typeof(Func<,>)};

        private static readonly string[] _invalidFunctionCharacters = {"-", ";", ".", ",", " ", "\n", "\r"};

        public YggCompilation Compile<TState>(IEnumerable<string> namespaces, IEnumerable<string> referenceAssemblyPaths, 
            List<ScriptedFunctionDefinition> definitions)
        {
            var compilation = new YggCompilation();
            var builderClassText = new StringBuilder();
            var usings = new List<string>(namespaces.Distinct().Select(s => $"using {s};\n"));
            var referencePaths = new HashSet<string>(referenceAssemblyPaths);

            // Add dynamic using if necessary.
            if (definitions.Any(d => d.ReplaceObjectWithDynamic)) { usings.Add("using System.Dynamic;"); }

            foreach (var u in usings) { builderClassText.Append(u); }
            builderClassText.Append("public class FunctionBuilder\n{");

            foreach (var definition in definitions)
            {
                var sf = CreateScriptedFunction<TState>(definition.Guid, definition.FunctionProperty,
                    definition.FunctionText, definition.ReplaceObjectWithDynamic, compilation.Errors);

                if (sf == null) { continue; }

                if (!compilation.FunctionMap.TryGetValue(sf.Guid, out var functions))
                {
                    functions = new List<ScriptedFunction>();
                    compilation.FunctionMap[sf.Guid] = functions;
                }

                functions.Add(sf);
                builderClassText.Append(sf.ScriptText);

                foreach (var reference in sf.References)
                {
                    referencePaths.Add(reference);
                }
            }

            builderClassText.Append("\n}");

            var references = referencePaths
                .Select(p => MetadataReference.CreateFromFile(p))
                .ToList();

            var options = ScriptOptions.Default.AddReferences(references);
            var script = CSharpScript.Create(builderClassText.ToString(), options);
            var comp = script.GetCompilation();

            byte[] compiledAssembly;
            using (var output = new MemoryStream())
            {
                var emitResult = comp.Emit(output);

                if (!emitResult.Success)
                {
                    var error = new BuildError {Message = "Emit compilation error.", IsCritical = true};
                    foreach (var diag in emitResult.Diagnostics) { error.Diagnostics.Add(diag); }
                    compilation.Errors.Add(error);
                    return compilation;
                }

                compiledAssembly = output.ToArray();
            }

            var assembly = Assembly.Load(compiledAssembly);
            var entryType = assembly.GetTypes().First(t => t.Name == "FunctionBuilder");
            var builder = Activator.CreateInstance(entryType);

            foreach (var sf in compilation.FunctionMap.Values.SelectMany(g => g))
            {
                sf.Builder = builder;
                sf.BuilderMethod = entryType.GetMethod(sf.BuilderMethodName);
            }

            compilation.Builder = builder;

            return compilation;
        }

        private static ScriptedFunction CreateScriptedFunction<TState>(string guid, PropertyInfo property, 
            string functionText, bool replaceObjectWithDynamic, List<BuildError> errors)
        {
            var propertyName = property.Name;
            var builderName = GetFunctionName("B", guid, propertyName);
            var functionName = GetFunctionName("F", guid, propertyName);
            var functionType = property.PropertyType;
            var genericTypeDefinition = functionType.GetGenericTypeDefinition();
            var hasReturnStatement = _returnStatement.IsMatch(functionText);
            var returnOpenText = hasReturnStatement ? string.Empty : "return ";
            var returnCloseText = hasReturnStatement ? string.Empty : ";";
            var stateType = typeof(TState);
            var stateTypeName = stateType.FullName?.Replace("+", ".");
            var generics = property.PropertyType.GetGenericArguments();
            var firstGenericType = generics.Length > 0 ? generics[0] : null;
            var firstGenericName = firstGenericType?.FullName?.Replace("+", ".");
            var secondGenericType = generics.Length > 1 ? generics[1] : null;
            var secondGenericName = secondGenericType?.FullName?.Replace("+", ".");
            var isSameType = firstGenericType == stateType;

            if (!_supportedScriptedFunctionTypes.Contains(functionType)
                && !_supportedScriptedFunctionTypes.Contains(genericTypeDefinition))
            {
                var error = ParserErrorHelper.UnsupportedScriptFunctionType(guid, property.DeclaringType?.Name,
                    functionType.Name, genericTypeDefinition.Name, functionText);

                errors.Add(error);
                return null;
            }

            var sf = new ScriptedFunction();
            sf.Guid = guid;
            sf.PropertyName = propertyName;
            sf.BuilderMethodName = builderName;
            sf.FunctionMethodName = functionName;
            sf.FunctionText = functionText;
            sf.Property = property;

            // References.
            sf.References.Add(stateType.GetTypeInfo().Assembly.Location);
            if (firstGenericType != null) { sf.References.Add(firstGenericType.GetTypeInfo().Assembly.Location); }
            if (secondGenericType != null) { sf.References.Add(secondGenericType.GetTypeInfo().Assembly.Location); }

            if (replaceObjectWithDynamic)
            {
                sf.References.Add(typeof(RuntimeBinderException).GetTypeInfo().Assembly.Location);
                sf.References.Add(typeof(DynamicAttribute).GetTypeInfo().Assembly.Location);

                if (firstGenericName != null && firstGenericType == typeof(object)) { firstGenericName = "dynamic"; }
                if (secondGenericName != null && secondGenericType == typeof(object)) { secondGenericName = "dynamic"; }
            }

            // Action.
            if (functionType == typeof(Action))
            {
                sf.ScriptText = $@"public static void {functionName}() {{ {functionText}; }} 
                                   public System.Action {builderName}() {{ return {functionName}; }}";
                return sf;
            }

            // Action with single generic.
            if (functionType.IsGenericType && genericTypeDefinition == typeof(Action<>))
            {
                sf.ScriptText = isSameType
                    ? $@"public static void {functionName}({firstGenericName} state) {{ {functionText}; }} 
                         public System.Action<{firstGenericName}> {builderName}() {{ return {functionName}; }} " 
                    : $@"public static void {functionName}({firstGenericName} baseState) {{ var state = ({stateTypeName})baseState; {functionText}; }} 
                         public System.Action<{firstGenericName}> {builderName}() {{ return {functionName}; }} ";

                return sf;
            }

            // Function with single generic.
            if (functionType.IsGenericType && genericTypeDefinition == typeof(Func<>))
            {
                sf.ScriptText = $@"public static {firstGenericName} {functionName}() {{ {returnOpenText}{functionText}{returnCloseText} }} 
                                   public System.Func<{firstGenericName}> {builderName}() {{ return {functionName}; }}";

                return sf;
            }

            // Function with double generic.
            if (functionType.IsGenericType && genericTypeDefinition == typeof(Func<,>))
            {
                sf.ScriptText = isSameType
                    ? $@"public static {secondGenericName} {functionName}({firstGenericName} state) {{ {returnOpenText}{functionText}{returnCloseText} }} 
                         public System.Func<{firstGenericName}, {secondGenericName}> {builderName}() {{ return {functionName}; }}" 
                    : $@"public static {secondGenericName} {functionName}({firstGenericName} baseState) {{ var state = ({stateTypeName})baseState; {returnOpenText}{functionText}{returnCloseText} }} 
                         public System.Func<{firstGenericName}, {secondGenericName}> {builderName}() {{ return {functionName}; }}";

                return sf;
            }

            return null;
        }

        private static string GetFunctionName(string type, string guid, string propertyName)
        {
            var name = new StringBuilder();

            name.Append(type);
            name.Append("_");
            name.Append(guid);
            name.Append("_");
            name.Append(propertyName);

            foreach (var character in _invalidFunctionCharacters) { name.Replace(character, ""); }

            return name.ToString();
        }
    }
}