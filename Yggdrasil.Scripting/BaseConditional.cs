namespace Yggdrasil.Scripting
{
    public abstract class BaseConditional
    {
        public abstract bool Execute(object baseState);
    }

    public abstract class BaseDynamicConditional
    {
        public abstract bool Execute(dynamic state);
    }
}
