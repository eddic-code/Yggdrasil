﻿using System;
using System.Collections.Generic;
using Yggdrasil.Coroutines;
using Yggdrasil.Enums;

namespace Yggdrasil.Behaviour
{
    public class BehaviourTree
    {
        [ThreadStatic]
        internal static BehaviourTree CurrentInstance;

        private readonly CoroutineManager _manager;

        private readonly Dictionary<CoroutineThread, Stack<Node>> _providersMap =
            new Dictionary<CoroutineThread, Stack<Node>>();

        internal Coroutine Yield => _manager.Yield;
        internal readonly Coroutine<Result> Success;
        internal readonly Coroutine<Result> Failure;

        public BehaviourTree(Node root)
        {
            Root = root;

            Success = Coroutine<Result>.CreateConst(Result.Success);
            Failure = Coroutine<Result>.CreateConst(Result.Failure);

            _manager = new CoroutineManager();
            _manager.Root = root;
        }

        public Node Root { get; }

        public event EventHandler<Node> NodeActiveEventHandler;

        public event EventHandler<Node> NodeInactiveEventHandler;

        public object State { get; private set; }

        public ulong TickCount => _manager.TickCount;

        public Result Result
        {
            get
            {
                var result = _manager.Result;
                if (result == null) { return Result.Unknown; }

                return (Result) result;
            }
        }

        public void Update(object state = null)
        {
            if (Root == null) { return; }

            CurrentInstance = this;
            State = state;

            _manager.Update();

            State = null;
            CurrentInstance = null;
        }

        public void Reset()
        {
            _manager.Reset();

            State = null;
        }

        internal void OnNodeTickStarted(Node node)
        {
            if (!_providersMap.TryGetValue(_manager.ActiveThread, out var providers))
            {
                providers = new Stack<Node>();
                _providersMap[_manager.ActiveThread] = providers;
            }

            providers.Push(node);
            OnNodeActiveEvent(node);
        }

        internal void OnNodeTickFinished(Node node)
        {
            if (!_providersMap.TryGetValue(_manager.ActiveThread, out var providers))
            {
                providers = new Stack<Node>();
                _providersMap[_manager.ActiveThread] = providers;
            }

            providers.Pop();
            OnNodeInactiveEvent(node);
        }

        internal void ProcessThread(CoroutineThread thread)
        {
            _manager.ProcessThread(thread);
        }

        internal void ProcessThreadAsDependency(CoroutineThread thread)
        {
            _manager.ProcessThreadAsDependency(thread);
        }

        internal void TerminateThread(CoroutineThread thread)
        {
            if (_providersMap.TryGetValue(thread, out var nodes))
            {
                foreach (var node in nodes) { node.Terminate(); }
                nodes.Clear();
            }

            _manager.TerminateThreadAndDependencies(thread);
        }

        protected virtual void OnNodeActiveEvent(Node node)
        {
            var handler = NodeActiveEventHandler;
            handler?.Invoke(this, node);
        }

        protected virtual void OnNodeInactiveEvent(Node node)
        {
            var handler = NodeInactiveEventHandler;
            handler?.Invoke(this, node);
        }
    }
}
