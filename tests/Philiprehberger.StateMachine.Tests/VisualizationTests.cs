using Xunit;
using Philiprehberger.StateMachine;

namespace Philiprehberger.StateMachine.Tests;

public class VisualizationTests
{
    private enum State { Idle, Active, Done }
    private enum Trigger { Start, Finish }

    [Fact]
    public void ToDot_ContainsDigraphHeader()
    {
        var machine = new StateMachineBuilder<State, Trigger>()
            .Configure(State.Idle)
                .Permit(Trigger.Start, State.Active)
            .Configure(State.Active)
                .Permit(Trigger.Finish, State.Done)
            .Build(State.Idle);

        var dot = machine.ToDot();

        Assert.Contains("digraph StateMachine {", dot);
        Assert.Contains("rankdir=LR;", dot);
    }

    [Fact]
    public void ToDot_HighlightsCurrentState()
    {
        var machine = new StateMachineBuilder<State, Trigger>()
            .Configure(State.Idle)
                .Permit(Trigger.Start, State.Active)
            .Build(State.Idle);

        var dot = machine.ToDot();

        Assert.Contains("\"Idle\" [style=\"rounded,filled\", fillcolor=lightblue]", dot);
    }

    [Fact]
    public void ToDot_ContainsTransitionEdges()
    {
        var machine = new StateMachineBuilder<State, Trigger>()
            .Configure(State.Idle)
                .Permit(Trigger.Start, State.Active)
            .Configure(State.Active)
                .Permit(Trigger.Finish, State.Done)
            .Build(State.Idle);

        var dot = machine.ToDot();

        Assert.Contains("\"Idle\" -> \"Active\" [label=\"Start\"]", dot);
        Assert.Contains("\"Active\" -> \"Done\" [label=\"Finish\"]", dot);
    }

    [Fact]
    public void ToDot_ShowsGuardedTransitions()
    {
        var machine = new StateMachineBuilder<State, Trigger>()
            .Configure(State.Idle)
                .PermitIf(Trigger.Start, State.Active, () => true)
            .Build(State.Idle);

        var dot = machine.ToDot();

        Assert.Contains("[guarded]", dot);
    }

    [Fact]
    public void ToDot_ShowsSubstateRelationships()
    {
        var machine = new StateMachineBuilder<State, Trigger>()
            .Configure(State.Idle)
                .Permit(Trigger.Start, State.Active)
            .Configure(State.Active)
                .SubstateOf(State.Idle)
                .Permit(Trigger.Finish, State.Done)
            .Build(State.Idle);

        var dot = machine.ToDot();

        Assert.Contains("\"Active\" -> \"Idle\" [style=dashed, label=\"substate of\"]", dot);
    }

    [Fact]
    public void ToMermaid_ContainsStateDiagramHeader()
    {
        var machine = new StateMachineBuilder<State, Trigger>()
            .Configure(State.Idle)
                .Permit(Trigger.Start, State.Active)
            .Build(State.Idle);

        var mermaid = machine.ToMermaid();

        Assert.Contains("stateDiagram-v2", mermaid);
    }

    [Fact]
    public void ToMermaid_ContainsTransitions()
    {
        var machine = new StateMachineBuilder<State, Trigger>()
            .Configure(State.Idle)
                .Permit(Trigger.Start, State.Active)
            .Configure(State.Active)
                .Permit(Trigger.Finish, State.Done)
            .Build(State.Idle);

        var mermaid = machine.ToMermaid();

        Assert.Contains("Idle --> Active : Start", mermaid);
        Assert.Contains("Active --> Done : Finish", mermaid);
    }

    [Fact]
    public void ToMermaid_ShowsCurrentStateNote()
    {
        var machine = new StateMachineBuilder<State, Trigger>()
            .Configure(State.Idle)
                .Permit(Trigger.Start, State.Active)
            .Build(State.Idle);

        var mermaid = machine.ToMermaid();

        Assert.Contains("note right of Idle : Current state", mermaid);
    }

    [Fact]
    public void ToMermaid_ShowsGuardedTransitions()
    {
        var machine = new StateMachineBuilder<State, Trigger>()
            .Configure(State.Idle)
                .PermitIf(Trigger.Start, State.Active, () => true)
            .Build(State.Idle);

        var mermaid = machine.ToMermaid();

        Assert.Contains("[guarded]", mermaid);
    }

    [Fact]
    public void ToDot_ShowsTimeoutTransitions()
    {
        var machine = new StateMachineBuilder<State, Trigger>()
            .Configure(State.Idle)
                .PermitAfter(Trigger.Start, State.Active, TimeSpan.FromSeconds(30))
            .Build(State.Idle);

        var dot = machine.ToDot();

        Assert.Contains("(after 30s)", dot);
    }

    [Fact]
    public void ToMermaid_ShowsTimeoutTransitions()
    {
        var machine = new StateMachineBuilder<State, Trigger>()
            .Configure(State.Idle)
                .PermitAfter(Trigger.Start, State.Active, TimeSpan.FromSeconds(30))
            .Build(State.Idle);

        var mermaid = machine.ToMermaid();

        Assert.Contains("(after 30s)", mermaid);
    }
}
