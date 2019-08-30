namespace Yggdrasil.Scripting
{
    public abstract class BaseConditional
    {
        public abstract bool Execute(object baseState);
    }

    public abstract class BaseConditional<T>
    {
        public abstract bool Execute(T state);
    }

    public abstract class BaseDynamicConditional
    {
        public abstract bool Execute(dynamic state);
    }
}
