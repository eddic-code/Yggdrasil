using System;

namespace Yggdrasil.Coroutines
{
    public abstract class CoroutineManagerBase
    {
        [ThreadStatic]
        internal static CoroutineManagerBase CurrentInstance;

        internal abstract void AddContinuation(IContinuation continuation);

        internal abstract void SetException(Exception exception);
    }
}
