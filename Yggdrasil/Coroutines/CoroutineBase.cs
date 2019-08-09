using Yggdrasil.Utility;

namespace Yggdrasil.Coroutines
{
    public abstract class CoroutineBase
    {
        internal static readonly ConcurrentPoolMap<IStateMachineWrapper> SmPool = new ConcurrentPoolMap<IStateMachineWrapper>();
    }
}
