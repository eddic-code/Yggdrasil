using System;

namespace Yggdrasil
{
    internal struct CoroutineContinuation
    {
        public readonly Action Continuation;
        public readonly IContinuation Builder;

        public CoroutineContinuation(IContinuation builder, Action continuation)
        {
            Builder = builder;
            Continuation = continuation;
        }
    }
}
