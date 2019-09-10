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

using System.Collections.Generic;
using Yggdrasil.Behaviour;

namespace Yggdrasil.Scripting
{
    internal static class ParserErrorHelper
    {
        public static BuildError UnsupportedScriptFunctionType(string guid, string declaringType,
            string propertyType, string genericDefinitionType, string functionText)
        {
            var error = new BuildError();

            error.IsCritical = true;
            error.Message = "Unsupported function type.";
            error.Data.Add($"Guid: {guid}");
            error.Data.Add($"Declaring Type: {declaringType}");
            error.Data.Add($"Property Type: {propertyType}");
            error.Data.Add($"Generic Type Definition: {genericDefinitionType}");
            error.Data.Add(functionText);

            return error;
        }

        public static BuildError CannotLoadReference(string exception)
        {
            var error = new BuildError();

            error.IsCritical = true;
            error.Message = "Could not load reference assembly.";
            error.Data.Add(exception);

            return error;
        }

        public static BuildError CannotCastToNode(string type, string file)
        {
            var error = new BuildError();

            error.IsCritical = true;
            error.Message = $"Could not cast {type} into {nameof(Node)}.";
            error.Data.Add($"File: {file}");

            return error;
        }

        public static BuildError UnableToInstantiate(string type, string file, string exception)
        {
            var error = new BuildError();

            error.IsCritical = true;
            error.Message = $"Could not instantiate node of type: {type}";
            error.Data.Add(exception);
            error.Data.Add($"File: {file}");

            return error;
        }

        public static BuildError MissingTypeDefInstance(string typeDef, string file)
        {
            var error = new BuildError();

            error.IsCritical = true;
            error.Message = $"Could not find node type or TypeDef during instantiation: {typeDef}";
            error.Data.Add($"File: {file}");

            return error;
        }

        public static BuildError RepeatedNodeGuid(string guid, params string[] files)
        {
            var error = new BuildError();

            error.IsCritical = true;
            error.Message = $"Repeated node GUID: {guid}";

            foreach (var file in files) { error.Data.Add($"File: {file}"); }

            return error;
        }

        public static BuildError RepeatedTypeDef(string typeDef, params string[] files)
        {
            var error = new BuildError();

            error.IsCritical = true;
            error.Message = $"Repeated TypeDef identifier: {typeDef}";

            foreach (var file in files) { error.Data.Add($"File: {file}"); }

            return error;
        }

        public static BuildError TypeDefsUnresolved(IEnumerable<string> typeDefs)
        {
            var error = new BuildError();

            error.IsCritical = true;
            error.Message = "Some TypeDefs could not be resolved.";

            foreach (var typeDef in typeDefs) { error.Data.Add(typeDef); }

            return error;
        }

        public static BuildError MissingTypeDef(string typeDef, params string[] files)
        {
            var error = new BuildError();

            error.IsCritical = true;
            error.Message = $"Missing TypeDef: {typeDef}";

            foreach (var file in files) { error.Data.Add($"File: {file}"); }

            return error;
        }

        public static BuildError FileMissing(string file)
        {
            var error = new BuildError();

            error.IsCritical = false;
            error.Message = $"File does not exit: {file}";

            return error;
        }

        public static BuildError FileLoad(string file, string exception)
        {
            var error = new BuildError();

            error.IsCritical = false;
            error.Message = "Could not load file.";

            error.Data.Add($"File: {file}");
            error.Data.Add(exception);

            return error;
        }
    }
}