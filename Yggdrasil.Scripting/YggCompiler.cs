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
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CSharp.RuntimeBinder;

namespace Yggdrasil.Scripting
{
    public class YggCompiler
    {
        private static readonly Regex _returnStatement =
            new Regex("return[\\s\n\r]+.+[\\s\n\r]*;", RegexOptions.Compiled);

        private static readonly string[] _invalidFunctionCharacters = {"-", ";", ".", ",", " ", "\n", "\r"};

        private readonly YggParserConfig _config;

        public YggCompiler(YggParserConfig config = null)
        {
            _config = config ?? new YggParserConfig();
        }

        public ScriptedFunction CreateScriptedFunction<TState>(string guid, PropertyInfo property, string functionText)
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

            var sf = new ScriptedFunction();
            sf.PropertyName = propertyName;
            sf.BuilderName = builderName;
            sf.FunctionName = functionName;
            sf.FunctionText = functionText;
            sf.Usings = new HashSet<string>(_config.ScriptUsings.Select(s => $"using {s};"));

            // References.
            sf.References.Add(stateType.GetTypeInfo().Assembly.Location);
            if (firstGenericType != null) { sf.References.Add(firstGenericType.GetTypeInfo().Assembly.Location); }
            if (secondGenericType != null) { sf.References.Add(secondGenericType.GetTypeInfo().Assembly.Location); }
            foreach (var reference in _config.ReferenceAssemblyPaths) { sf.References.Add(reference); }

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
                    ? $@"public static void {functionName}({firstGenericName} state) {{ {returnOpenText}{functionText}{returnCloseText} }} 
                         public System.Action<{firstGenericName}> {builderName}() {{ return {functionName}; }} " 
                    : $@"public static void {functionName}({firstGenericName} baseState) {{ var state = ({stateTypeName})baseState; {returnOpenText}{functionText}{returnCloseText} }} 
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

        public ImmutableArray<Diagnostic> CompileFunction<TState>(object obj, string propertyName, string functionText)
        {
            var property = obj.GetType().GetProperty(propertyName);
            if (property == null) return ImmutableArray<Diagnostic>.Empty;

            var functionType = property.PropertyType;
            var genericTypeDefinition = functionType.GetGenericTypeDefinition();

            // Action.
            if (functionType == typeof(Action)) return CompileAction(obj, property, functionText);

            // Action with single generic.
            if (functionType.IsGenericType && genericTypeDefinition == typeof(Action<>))
                return CompileActionSingleGeneric<TState>(obj, property, functionText);

            // Function with single generic.
            if (functionType.IsGenericType && genericTypeDefinition == typeof(Func<>))
                return CompileFuncSingleGeneric(obj, property, functionText);

            // Function with double generic.
            if (functionType.IsGenericType && genericTypeDefinition == typeof(Func<,>))
                return CompileFuncDoubleGeneric<TState>(obj, property, functionText);

            return ImmutableArray<Diagnostic>.Empty;
        }

        public ImmutableArray<Diagnostic> CompileDynamicFunction(object obj, string propertyName, string functionText)
        {
            var property = obj.GetType().GetProperty(propertyName);
            if (property == null) return ImmutableArray<Diagnostic>.Empty;

            var functionType = property.PropertyType;
            var genericTypeDefinition = functionType.GetGenericTypeDefinition();

            // Dynamic action with single generic.
            if (functionType.IsGenericType && genericTypeDefinition == typeof(Action<>))
                return CompileDynamicActionSingleGeneric(obj, property, functionText);

            // Dynamic function with single generic.
            if (functionType.IsGenericType && genericTypeDefinition == typeof(Func<>))
                return CompileDynamicFuncSingleGeneric(obj, property, functionText);

            // Dynamic function with double generic.
            if (functionType.IsGenericType && genericTypeDefinition == typeof(Func<,>))
                return CompileDynamicFuncDoubleGeneric(obj, property, functionText);

            return ImmutableArray<Diagnostic>.Empty;
        }

        private ImmutableArray<Diagnostic> CompileAction(object obj, PropertyInfo property, string functionText)
        {
            var scriptText = $@"public class FunctionBuilder
                                {{
                                    public static void Execute() {{ {functionText}; }} 

                                    public System.Action GetFunction() {{ return Execute; }} 
                                }}";

            foreach (var name in _config.ScriptUsings) scriptText = scriptText.Insert(0, $"using {name};");

            var references = _config.ReferenceAssemblyPaths
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
                if (!emitResult.Success) return emitResult.Diagnostics;

                compiledAssembly = output.ToArray();
            }

            var assembly = Assembly.Load(compiledAssembly);
            var entryType = assembly.GetTypes().First(t => t.Name == "FunctionBuilder");
            var instance = Activator.CreateInstance(entryType);
            var builderMethod = entryType.GetMethod("GetFunction");
            var function = builderMethod?.Invoke(instance, null);

            property.SetValue(obj, function);

            return ImmutableArray<Diagnostic>.Empty;
        }

        private ImmutableArray<Diagnostic> CompileActionSingleGeneric<TState>(object obj, PropertyInfo property,
            string functionText)
        {
            var generics = property.PropertyType.GetGenericArguments();
            var firstGenericType = generics[0];
            var firstGenericName = firstGenericType.FullName?.Replace("+", ".");
            var stateType = typeof(TState);
            var stateTypeName = stateType.FullName?.Replace("+", ".");

            var hasReturnStatement = _returnStatement.IsMatch(functionText);
            var returnOpenText = hasReturnStatement ? string.Empty : "return";
            var returnCloseText = hasReturnStatement ? string.Empty : ";";

            string scriptText;
            if (firstGenericType == stateType)
                // If the state type is the same as the generic type, we don't need to cast.
                scriptText = $@"public class FunctionBuilder
                                {{
                                    public static void Execute({firstGenericName} state) {{ {returnOpenText} {functionText}{returnCloseText} }} 

                                    public System.Action<{firstGenericName}> GetFunction() {{ return Execute; }} 
                                }}";
            else
                scriptText = $@"public class FunctionBuilder
                                {{
                                    public static void Execute({firstGenericName} baseState) {{ var state = ({stateTypeName})baseState; {returnOpenText} {functionText}{returnCloseText} }} 

                                    public System.Action<{firstGenericName}> GetFunction() {{ return Execute; }} 
                                }}";

            foreach (var name in _config.ScriptUsings) scriptText = scriptText.Insert(0, $"using {name};");

            var references = _config.ReferenceAssemblyPaths
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
                if (!emitResult.Success) return emitResult.Diagnostics;

                compiledAssembly = output.ToArray();
            }

            var assembly = Assembly.Load(compiledAssembly);
            var entryType = assembly.GetTypes().First(t => t.Name == "FunctionBuilder");
            var instance = Activator.CreateInstance(entryType);
            var builderMethod = entryType.GetMethod("GetFunction");
            var function = builderMethod?.Invoke(instance, null);

            property.SetValue(obj, function);

            return ImmutableArray<Diagnostic>.Empty;
        }

        private ImmutableArray<Diagnostic> CompileFuncSingleGeneric(object obj, PropertyInfo property,
            string functionText)
        {
            var generics = property.PropertyType.GetGenericArguments();
            var firstGenericType = generics[0];
            var firstGenericName = firstGenericType.FullName?.Replace("+", ".");

            var hasReturnStatement = _returnStatement.IsMatch(functionText);
            var returnOpenText = hasReturnStatement ? string.Empty : "return";
            var returnCloseText = hasReturnStatement ? string.Empty : ";";

            var scriptText = $@"public class FunctionBuilder
                                {{
                                    public static {firstGenericName} Execute() {{ {returnOpenText} {functionText}{returnCloseText} }} 

                                    public System.Func<{firstGenericName}> GetFunction() {{ return Execute; }} 
                                }}";

            foreach (var name in _config.ScriptUsings) scriptText = scriptText.Insert(0, $"using {name};");

            var defaultReferences = new[]
            {
                firstGenericType.GetTypeInfo().Assembly.Location
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
                if (!emitResult.Success) return emitResult.Diagnostics;

                compiledAssembly = output.ToArray();
            }

            var assembly = Assembly.Load(compiledAssembly);
            var entryType = assembly.GetTypes().First(t => t.Name == "FunctionBuilder");
            var instance = Activator.CreateInstance(entryType);
            var builderMethod = entryType.GetMethod("GetFunction");
            var function = builderMethod?.Invoke(instance, null);

            property.SetValue(obj, function);

            return ImmutableArray<Diagnostic>.Empty;
        }

        private ImmutableArray<Diagnostic> CompileFuncDoubleGeneric<TState>(object obj, PropertyInfo property,
            string functionText)
        {
            var generics = property.PropertyType.GetGenericArguments();
            var firstGenericType = generics[0];
            var secondGenericType = generics[1];
            var firstGenericName = firstGenericType.FullName?.Replace("+", ".");
            var secondGenericName = secondGenericType.FullName?.Replace("+", ".");
            var stateType = typeof(TState);
            var stateTypeName = stateType.FullName?.Replace("+", ".");

            var hasReturnStatement = _returnStatement.IsMatch(functionText);
            var returnOpenText = hasReturnStatement ? string.Empty : "return";
            var returnCloseText = hasReturnStatement ? string.Empty : ";";

            string scriptText;
            if (firstGenericType == stateType)
                scriptText = $@"public class FunctionBuilder
                                {{
                                    public static {secondGenericName} Execute({firstGenericName} state) {{ {returnOpenText} {functionText}{returnCloseText} }} 

                                    public System.Func<{firstGenericName}, {secondGenericName}> GetFunction() {{ return Execute; }} 
                                }}";
            else
                scriptText = $@"public class FunctionBuilder
                                {{
                                    public static {secondGenericName} Execute({firstGenericName} baseState) {{ var state = ({stateTypeName})baseState; {returnOpenText} {functionText}{returnCloseText} }} 

                                    public System.Func<{firstGenericName}, {secondGenericName}> GetFunction() {{ return Execute; }} 
                                }}";

            foreach (var name in _config.ScriptUsings) scriptText = scriptText.Insert(0, $"using {name};");

            var defaultReferences = new[]
            {
                firstGenericType.GetTypeInfo().Assembly.Location,
                secondGenericType.GetTypeInfo().Assembly.Location,
                stateType.GetTypeInfo().Assembly.Location
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
                if (!emitResult.Success) return emitResult.Diagnostics;

                compiledAssembly = output.ToArray();
            }

            var assembly = Assembly.Load(compiledAssembly);
            var entryType = assembly.GetTypes().First(t => t.Name == "FunctionBuilder");
            var instance = Activator.CreateInstance(entryType);
            var builderMethod = entryType.GetMethod("GetFunction");
            var function = builderMethod?.Invoke(instance, null);

            property.SetValue(obj, function);

            return ImmutableArray<Diagnostic>.Empty;
        }

        private ImmutableArray<Diagnostic> CompileDynamicActionSingleGeneric(object obj, PropertyInfo property,
            string functionText)
        {
            var scriptText = $@"using System.Dynamic;
                                public class FunctionBuilder
                                {{
                                    public static dynamic Execute() {{ {functionText}; }} 

                                    public System.Action<dynamic> GetFunction() {{ return Execute; }} 
                                }}";

            foreach (var name in _config.ScriptUsings) scriptText = scriptText.Insert(0, $"using {name};");

            var defaultReferences = new[]
            {
                typeof(RuntimeBinderException).GetTypeInfo().Assembly.Location,
                typeof(DynamicAttribute).GetTypeInfo().Assembly.Location
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
                if (!emitResult.Success) return emitResult.Diagnostics;

                compiledAssembly = output.ToArray();
            }

            var assembly = Assembly.Load(compiledAssembly);
            var entryType = assembly.GetTypes().First(t => t.Name == "FunctionBuilder");
            var instance = Activator.CreateInstance(entryType);
            var builderMethod = entryType.GetMethod("GetFunction");
            var function = builderMethod?.Invoke(instance, null);

            property.SetValue(obj, function);

            return ImmutableArray<Diagnostic>.Empty;
        }

        private ImmutableArray<Diagnostic> CompileDynamicFuncSingleGeneric(object obj, PropertyInfo property,
            string functionText)
        {
            var hasReturnStatement = _returnStatement.IsMatch(functionText);
            var returnOpenText = hasReturnStatement ? string.Empty : "return";
            var returnCloseText = hasReturnStatement ? string.Empty : ";";

            var scriptText = $@"using System.Dynamic;
                                public class FunctionBuilder
                                {{
                                    public static dynamic Execute() {{ {returnOpenText} {functionText}{returnCloseText} }} 

                                    public System.Func<dynamic> GetFunction() {{ return Execute; }} 
                                }}";

            foreach (var name in _config.ScriptUsings) scriptText = scriptText.Insert(0, $"using {name};");

            var defaultReferences = new[]
            {
                typeof(RuntimeBinderException).GetTypeInfo().Assembly.Location,
                typeof(DynamicAttribute).GetTypeInfo().Assembly.Location
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
                if (!emitResult.Success) return emitResult.Diagnostics;

                compiledAssembly = output.ToArray();
            }

            var assembly = Assembly.Load(compiledAssembly);
            var entryType = assembly.GetTypes().First(t => t.Name == "FunctionBuilder");
            var instance = Activator.CreateInstance(entryType);
            var builderMethod = entryType.GetMethod("GetFunction");
            var function = builderMethod?.Invoke(instance, null);

            property.SetValue(obj, function);

            return ImmutableArray<Diagnostic>.Empty;
        }

        private ImmutableArray<Diagnostic> CompileDynamicFuncDoubleGeneric(object obj, PropertyInfo property,
            string functionText)
        {
            var generics = property.PropertyType.GetGenericArguments();
            var secondGenericType = generics[1];
            var secondGenericName = secondGenericType.FullName?.Replace("+", ".");

            var hasReturnStatement = _returnStatement.IsMatch(functionText);
            var returnOpenText = hasReturnStatement ? string.Empty : "return";
            var returnCloseText = hasReturnStatement ? string.Empty : ";";

            var scriptText = $@"using System.Dynamic;
                                public class FunctionBuilder
                                {{
                                    public static {secondGenericName} Execute(dynamic state) {{ {returnOpenText} {functionText}{returnCloseText} }} 

                                    public System.Func<dynamic, {secondGenericName}> GetFunction() {{ return Execute; }} 
                                }}";

            foreach (var name in _config.ScriptUsings) scriptText = scriptText.Insert(0, $"using {name};");

            var defaultReferences = new[]
            {
                typeof(RuntimeBinderException).GetTypeInfo().Assembly.Location,
                typeof(DynamicAttribute).GetTypeInfo().Assembly.Location,
                secondGenericType.GetTypeInfo().Assembly.Location
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
                if (!emitResult.Success) return emitResult.Diagnostics;

                compiledAssembly = output.ToArray();
            }

            var assembly = Assembly.Load(compiledAssembly);
            var entryType = assembly.GetTypes().First(t => t.Name == "FunctionBuilder");
            var instance = Activator.CreateInstance(entryType);
            var builderMethod = entryType.GetMethod("GetFunction");
            var function = builderMethod?.Invoke(instance, null);

            property.SetValue(obj, function);

            return ImmutableArray<Diagnostic>.Empty;
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