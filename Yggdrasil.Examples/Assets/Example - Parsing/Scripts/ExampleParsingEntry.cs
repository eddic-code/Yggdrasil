using System;
using UnityEngine;
using Yggdrasil.ScriptTypes;

public class ExampleParsingEntry : MonoBehaviour
{
    private BaseConditional _conditional;
    private ExampleParsingState _state;

    public void Start()
    {
        var parser = new YggParser();
        var (conditional, errors) = parser.CompileConditional<ExampleParsingState>(@"state.A > state.B || state.C < state.D");
        foreach (var error in errors) { Debug.Log(error.ToString()); }

        _conditional = conditional;
        _state = new ExampleParsingState {A = 1, B = 2, C = 3, D = 4};
    }

    public void Update()
    {
        if (!_conditional.Execute(_state)) { throw new Exception(); }
    }
}
