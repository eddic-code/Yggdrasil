using System;
using System.IO;
using System.Linq;
using UnityEngine;
using Yggdrasil.Behaviour;
using Yggdrasil.Enums;
using Yggdrasil.Scripting;

public class ExampleScriptLoader : MonoBehaviour
{
    public ulong Ticks;

    private BehaviourTree _tree;
    private ExampleState _state;

    public void Start()
    {
        // You need to pass the "netstandard" assembly reference when in Unity.
        // This is not necessary on a standalone .NET Core 2.2 console program.
        // You can also pass more references and namespace usings for the C# snippets if needed.
        var netstandard = AppDomain.CurrentDomain.GetAssemblies().First(n => n.GetName().Name == "netstandard").Location;
        var config = new YggParserConfig();
        config.ReferenceAssemblyPaths.Add(netstandard);

        // You choose which currently loaded assemblies are searched for types deriving from the Node class.
        config.NodeTypeAssemblies.Add(typeof(Node).Assembly.GetName().Name);
        config.NodeTypeAssemblies.Add(typeof(ExampleCustomAction).Assembly.GetName().Name);

        // Create the compiler and parser. They can be reused for multiple parsings.
        // The only state maintained by a parser instance is the initial config passed on its constructor.
        var compiler = new YggCompiler();
        var parser = new YggParser(config, compiler);

        // The parser gives back a behaviour tree definition, which can be used to instantiate the tree multiple times (or any node within by its GUID).
        // The definition also keeps track of errors that might have happened during parsing, and other debug information.
        // Node GUIDs can be assigned manually on the script files themselves. If they don't have one, the parser assigns a random one. They must be unique
        // withing the context of all the files parsed. The same goes for 'TypeDefs'. You can reference TypeDefs between files.
        var scriptFilePath = Path.Combine(Application.streamingAssetsPath, "exampleScript.ygg");
        var definition = parser.BuildFromFiles<ExampleState>(scriptFilePath);

        // The errors often have useful information to debug them. In this example there should be none.
        if (definition.Errors.Count > 0) { throw new Exception("Errors while parsing script files."); }

        // You can instantiate any node defined on the parsed scripts by passing their GUID.
        // This creates an instance of that node and all its descendants.
        // This also calls Initialize() on the instantiated nodes (in a depth-first traversal, if that matters to you).
        // The script files themselves have no explicit root, so you must instantiate the tree using the desired root node's GUID.
        var root = definition.Instantiate("root");

        // The behaviour tree object serves as a manager that handles the node's execution and async continuations.
        // The behaviour tree is what keeps the "continuation state" of the tree. This means you can reuse a single "root" node instance
        // on multiple behaviour trees that get updated separately without issues. This is only valid for the node types that Yggdrasil comes
        // with by default. If you create new node types inheriting from the Node class, you must make sure they don't keep any state themselves.
        _tree = new BehaviourTree(root);

        // Some example state object passed to the tree's nodes. Kept as a field here to allocate it only once.
        _state = new ExampleState();

        // You can pre-pool some coroutines. Otherwise, the first run of the tree will allocate as necessary.
        // They get recycled automatically. The amount of coroutines needed depends on how deep an async Coroutine method stack call can get during execution.
        // Unless the nodes use multiple nested async Coroutine methods, this means only one Coroutine<Result> per node on the deepest possible branch of the tree.
        // Parallel and Interrupt nodes can complicate the math, since they create multiple simultaneously active branches.
        // Pre-pooling yourself like this is optional. It'll create as necessary during execution.
        Yggdrasil.Coroutines.Coroutine.Pool.PrePool(20);
        Yggdrasil.Coroutines.Coroutine<Result>.Pool.PrePool(20);
    }

    // Profiler should show no allocations under this method on release builds.
    public void Update()
    {
        // You can update a tree whenever you want. Note that one update does not mean one full tick of the tree, since
        // nodes can stay running across multiple tree updates. The state object should be a reference type, so that any changes
        // on its data will be present on running nodes when next they get updated.
        _tree.Update(_state);

        // When the tick count increases, it means a full tick cycle of the tree completed (root returned a value).
        Ticks = _tree.TickCount;
    }
}
