using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Yggdrasil.Scripting;
using Yggdrasil.ScriptTypes;

public class ExampleCompilationEntry : MonoBehaviour
{
    public bool ConditionalResult;

    private BaseConditional _conditional;
    private ExampleCompilationState _state;

    public void Start()
    {
        var netstandard = AppDomain.CurrentDomain.GetAssemblies().First(n => n.GetName().Name == "netstandard");
        var config = new YggParserConfig {ReferenceAssemblyPaths = new List<string> {netstandard.Location}};
        var parser = new YggParser(config);

        var (conditional, errors) = parser.CompileConditional<ExampleCompilationState>(@"state.A > state.B || state.C < state.D");
        foreach (var error in errors) { Debug.Log(error.ToString()); }

        _conditional = conditional;
        _state = new ExampleCompilationState {A = 1, B = 2, C = 3, D = 4};
    }

    public void Update()
    {
        ConditionalResult = _conditional.Execute(_state);
        if (!ConditionalResult) { throw new Exception(); }
    }
}
