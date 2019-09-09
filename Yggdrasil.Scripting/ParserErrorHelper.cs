using System.Collections.Generic;
using Yggdrasil.Nodes;

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
