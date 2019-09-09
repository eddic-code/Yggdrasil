﻿using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using Yggdrasil.Coroutines;
using Yggdrasil.Nodes;

namespace Yggdrasil.Scripting
{
    public class ParserNode
    {
        public string File;
        public string Tag;
        public string Guid;
        public string DeclaringTypeDef;
        public bool IsTopmost;
        public bool IsDerivedFromTypeDef;
        public ParserNode TypeDef;
        public XmlNode Xml;
        public Type Type;
        public List<ParserNode> Children = new List<ParserNode>();
        public List<ScriptedFunction> ScriptedFunctions = new List<ScriptedFunction>();
        public List<ScriptedFunctionDefinition> FunctionDefinitions = new List<ScriptedFunctionDefinition>();

        public Node CreateInstance(CoroutineManager manager, Dictionary<string, ParserNode> typeDefMap, List<BuildError> errors)
        {
            return IsDerivedFromTypeDef 
                ? CreateTypeDefInstance(manager, typeDefMap, errors) 
                : CreateStaticTypeInstance(manager, typeDefMap, errors);
        }

        private Node CreateTypeDefInstance(CoroutineManager manager, Dictionary<string, ParserNode> typeDefMap, List<BuildError> errors)
        {
            // Find the TypeDef parser node.
            if (!typeDefMap.TryGetValue(Tag, out var parserDef))
            {
                var error = new BuildError {Message = $"Could not find node type or TypeDef: {Tag}", Target = File, SecondTarget = Xml.Value};
                errors.Add(error);
                return null;
            }

            // Create an instance from the TypeDef parser node.
            var instance = parserDef.CreateInstance(manager, typeDefMap, errors);
            if (instance == null) { return null; }

            // Set manager and guid.
            instance.Manager = manager;
            instance.Guid = Guid;

            // Set function values.
            foreach (var function in ScriptedFunctions)
            {
                function.SetFunctionPropertyValue(instance);
            }

            // Instantiate the children.
            foreach (var parserChild in Children)
            {
                var child = parserChild.CreateInstance(manager, typeDefMap, errors);
                if (child == null) { continue; }

                instance.Children.Add(child);
            }

            return instance;
        }

        private Node CreateStaticTypeInstance(CoroutineManager manager, Dictionary<string, ParserNode> typeDefMap, List<BuildError> errors)
        {
            // Create an instance using reflection.
            Node instance;

            try
            {
                var serializer = new XmlSerializer(Type);
                instance = serializer.Deserialize(new XmlNodeReader(Xml)) as Node;
            }
            catch (Exception e)
            {
                var error = new BuildError {Message = $"Could not instantiate node of type {Tag}. {e.Message}", Target = File, SecondTarget = Xml.Value};
                errors.Add(error);
                return null;
            }

            if (instance == null)
            {
                var error = new BuildError {Message = $"Could not cast {Tag} into {nameof(Node)}.", Target = File, SecondTarget = Xml.Value};
                errors.Add(error);
                return null;
            }

            // Set manager and guid.
            instance.Manager = manager;
            instance.Guid = Guid;

            // Set function values.
            foreach (var function in ScriptedFunctions)
            {
                function.SetFunctionPropertyValue(instance);
            }

            if (instance.Children == null) { instance.Children = new List<Node>(); }

            // Instantiate the children.
            foreach (var parserChild in Children)
            {
                var child = parserChild.CreateInstance(manager, typeDefMap, errors);
                if (child == null) { continue; }

                instance.Children.Add(child);
            }

            return instance;
        }
    }
}
