using System.Collections.Generic;
using System.Reflection;

namespace Yggdrasil.Scripting
{
    public class ScriptedFunction
    {
        public string Guid;
        public string PropertyName;
        public string FunctionMethodName;
        public string BuilderMethodName;
        public string ScriptText;
        public string FunctionText;

        public object Builder;
        public MethodInfo BuilderMethod;
        public PropertyInfo Property;

        public HashSet<string> References { get; set; } = new HashSet<string>();

        public void SetFunctionPropertyValue(object obj)
        {
            var function = BuilderMethod?.Invoke(Builder, null);
            Property.SetValue(obj, function);
        }
    }
}
