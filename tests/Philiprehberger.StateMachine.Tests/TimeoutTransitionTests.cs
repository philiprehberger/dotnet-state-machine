using Xunit;
using Philiprehberger.StateMachine;

namespace Philiprehberger.StateMachine.Tests;

public class TimeoutTransitionTests
{
    private enum State { Idle, Active, TimedOut }
    private enum Trigger { Start, Timeout, Reset }

    [Fact]
    public void PermitAfter_RegistersTransitionAndTimeout()
    {
        var machine = new StateMachineBuilder<State, Trigger>()
            .Configure(State.Idle)
                .PermitAfter(Trigger.Timeout, State.TimedOut, TimeSpan.FromMinutes(5))
            .Configure(State.TimedOut)
            .Build(State.Idle);

        // The trigger should be manually fireable as well
        Assert.True(machine.CanFire(Trigger.Timeout));
    }

    [Fact]
    public void PermitAfter_ManualFireStillWorks()
    {
        var machine = new StateMachineBuilder<State, Trigger>()
            .Configure(State.Idle)
                .PermitAfter(Trigger.Timeout, State.TimedOut, TimeSpan.FromMinutes(5))
            .Configure(State.TimedOut)
            .Build(State.Idle);

        machine.Fire(Trigger.Timeout);

        Assert.Equal(State.TimedOut, machine.CurrentState);
    }

    [Fact]
    public void PermitAfter_AutoFiresAfterTimeout()
    {
        var machine = new StateMachineBuilder<State, Trigger>()
            .Configure(State.Idle)
                .PermitAfter(Trigger.Timeout, State.TimedOut, TimeSpan.FromMilliseconds(50))
            .Configure(State.TimedOut)
            .Build(State.Idle);

        // Wait for the timeout to fire
        Thread.Sleep(200);

        Assert.Equal(State.TimedOut, machine.CurrentState);
    }

    [Fact]
    public void PermitAfter_CancelsTimeoutWhenStateChanges()
    {
        var machine = new StateMachineBuilder<State, Trigger>()
            .Configure(State.Idle)
                .Permit(Trigger.Start, State.Active)
                .PermitAfter(Trigger.Timeout, State.TimedOut, TimeSpan.FromMilliseconds(100))
            .Configure(State.Active)
            .Configure(State.TimedOut)
            .Build(State.Idle);

        // Leave the state before timeout
        machine.Fire(Trigger.Start);

        // Wait past the timeout duration
        Thread.Sleep(200);

        // Should still be Active, not TimedOut
        Assert.Equal(State.Active, machine.CurrentState);
    }

    [Fact]
    public void PermitAfter_RecordsTransitionInHistory()
    {
        var machine = new StateMachineBuilder<State, Trigger>()
            .Configure(State.Idle)
                .PermitAfter(Trigger.Timeout, State.TimedOut, TimeSpan.FromMilliseconds(50))
            .Configure(State.TimedOut)
            .Build(State.Idle);

        // Wait for timeout
        Thread.Sleep(200);

        Assert.Single(machine.TransitionHistory);
        Assert.Equal(State.Idle, machine.TransitionHistory[0].FromState);
        Assert.Equal(State.TimedOut, machine.TransitionHistory[0].ToState);
        Assert.Equal(Trigger.Timeout, machine.TransitionHistory[0].Trigger);
    }

    [Fact]
    public void PermitAfter_AppearsInPermittedTriggers()
    {
        var machine = new StateMachineBuilder<State, Trigger>()
            .Configure(State.Idle)
                .Permit(Trigger.Start, State.Active)
                .PermitAfter(Trigger.Timeout, State.TimedOut, TimeSpan.FromMinutes(5))
            .Configure(State.Active)
            .Configure(State.TimedOut)
            .Build(State.Idle);

        var permitted = machine.GetPermittedTriggers();

        Assert.Contains(Trigger.Start, permitted);
        Assert.Contains(Trigger.Timeout, permitted);
    }

    [Fact]
    public void PermitAfter_ShowsInDotVisualization()
    {
        var machine = new StateMachineBuilder<State, Trigger>()
            .Configure(State.Idle)
                .PermitAfter(Trigger.Timeout, State.TimedOut, TimeSpan.FromSeconds(10))
            .Configure(State.TimedOut)
            .Build(State.Idle);

        var dot = machine.ToDot();

        Assert.Contains("(after 10s)", dot);
        Assert.Contains("\"Idle\" -> \"TimedOut\"", dot);
    }

    [Fact]
    public void PermitAfter_ShowsInMermaidVisualization()
    {
        var machine = new StateMachineBuilder<State, Trigger>()
            .Configure(State.Idle)
                .PermitAfter(Trigger.Timeout, State.TimedOut, TimeSpan.FromSeconds(10))
            .Configure(State.TimedOut)
            .Build(State.Idle);

        var mermaid = machine.ToMermaid();

        Assert.Contains("(after 10s)", mermaid);
        Assert.Contains("Idle --> TimedOut", mermaid);
    }
}
