using System;

namespace Yggdrasil
{
    internal struct CoroutineContinuation
    {
        public readonly Action Continuation;
        public readonly Node Node;
        public readonly object Coroutine;
        public readonly object Builder;

        public CoroutineContinuation(object builder, object coroutine, Action continuation, Node node)
        {
            Builder = builder;
            Coroutine = coroutine;
            Continuation = continuation;
            Node = node;
        }

        public void Discard()
        {
            
        }
    }
}
