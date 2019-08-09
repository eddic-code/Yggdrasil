using System;

namespace Yggdrasil
{
    internal struct CoroutineContinuation
    {
        public readonly Action Continuation;
        public readonly IDiscardable Builder;

        public CoroutineContinuation(IDiscardable builder, Action continuation)
        {
            Builder = builder;
            Continuation = continuation;
        }
    }
}
