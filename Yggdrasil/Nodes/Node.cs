﻿using Yggdrasil.Coroutines;

namespace Yggdrasil.Nodes
{
    public abstract class Node
    {
        private readonly CoroutineManager _manager;

        protected Coroutine Yield => _manager.Yield;

        protected Node(CoroutineManager tree)
        {
            _manager = tree;
        }

        public string Guid { get; }

        public virtual string Name { get; set; }

        public abstract Coroutine Tick();
    }
}