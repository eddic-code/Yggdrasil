using System.Runtime.CompilerServices;

namespace Yggdrasil.Coroutines
{
    internal class StateMachineWrapper<T> : IStateMachineWrapper where T : IAsyncStateMachine
    {
        public T StateMachine;

        public void MoveNext()
        {
            StateMachine.MoveNext();
        }
    }
}
