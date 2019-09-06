using System;

namespace Yggdrasil.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class ScriptedFunctionAttribute : Attribute
    {
        public readonly bool ReplaceObjectWithDynamic;

        public ScriptedFunctionAttribute(bool replaceObjectWithDynamic = false)
        {
            ReplaceObjectWithDynamic = replaceObjectWithDynamic;
        }
    }
}
