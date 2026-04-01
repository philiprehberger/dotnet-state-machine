using Xunit;
using Philiprehberger.StateMachine;

namespace Philiprehberger.StateMachine.Tests;

public class ParameterizedTriggerTests
{
    private enum State { Idle, Processing, Done }
    private enum Trigger { Start, Complete }

    [Fact]
    public void Fire_WithArgument_TransitionsToTargetState()
    {
        var machine = new StateMachineBuilder<State, Trigger>()
            .Configure(State.Idle)
                .Permit(Trigger.Start, State.Processing)
            .Configure(State.Processing)
                .Permit(Trigger.Complete, State.Done)
            .Build(State.Idle);

        machine.Fire<string>(Trigger.Start, "payload");

        Assert.Equal(State.Processing, machine.CurrentState);
    }

    [Fact]
    public void Fire_WithArgument_PassesArgToParameterizedEntryAction()
    {
        string? received = null;

        var machine = new StateMachineBuilder<State, Trigger>()
            .Configure(State.Idle)
                .Permit(Trigger.Start, State.Processing)
            .Configure(State.Processing)
                .OnEntry<string>(Trigger.Start, arg => received = arg)
                .Permit(Trigger.Complete, State.Done)
            .Build(State.Idle);

        machine.Fire<string>(Trigger.Start, "hello");

        Assert.Equal("hello", received);
    }

    [Fact]
    public void Fire_WithArgument_InvokesEntryActionAndParameterizedAction()
    {
        var actions = new List<string>();

        var machine = new StateMachineBuilder<State, Trigger>()
            .Configure(State.Idle)
                .Permit(Trigger.Start, State.Processing)
            .Configure(State.Processing)
                .OnEntry(() => actions.Add("entry"))
                .OnEntry<int>(Trigger.Start, arg => actions.Add($"param-{arg}"))
                .Permit(Trigger.Complete, State.Done)
            .Build(State.Idle);

        machine.Fire<int>(Trigger.Start, 42);

        Assert.Contains("param-42", actions);
        Assert.Contains("entry", actions);
    }

    [Fact]
    public void Fire_WithArgument_RecordsTransitionHistory()
    {
        var machine = new StateMachineBuilder<State, Trigger>()
            .Configure(State.Idle)
                .Permit(Trigger.Start, State.Processing)
            .Configure(State.Processing)
            .Build(State.Idle);

        machine.Fire<string>(Trigger.Start, "data");

        Assert.Single(machine.TransitionHistory);
        Assert.Equal(State.Idle, machine.TransitionHistory[0].FromState);
        Assert.Equal(State.Processing, machine.TransitionHistory[0].ToState);
        Assert.Equal(Trigger.Start, machine.TransitionHistory[0].Trigger);
    }

    [Fact]
    public void Fire_WithArgument_ThrowsForInvalidTransition()
    {
        var machine = new StateMachineBuilder<State, Trigger>()
            .Configure(State.Idle)
                .Permit(Trigger.Start, State.Processing)
            .Build(State.Idle);

        Assert.Throws<InvalidTransitionException>(() =>
            machine.Fire<string>(Trigger.Complete, "data"));
    }

    [Fact]
    public async Task FireAsync_WithArgument_TransitionsToTargetState()
    {
        var machine = new StateMachineBuilder<State, Trigger>()
            .Configure(State.Idle)
                .Permit(Trigger.Start, State.Processing)
            .Configure(State.Processing)
            .Build(State.Idle);

        await machine.FireAsync<string>(Trigger.Start, "data");

        Assert.Equal(State.Processing, machine.CurrentState);
    }

    [Fact]
    public async Task FireAsync_WithArgument_PassesArgToParameterizedEntryAction()
    {
        int received = 0;

        var machine = new StateMachineBuilder<State, Trigger>()
            .Configure(State.Idle)
                .Permit(Trigger.Start, State.Processing)
            .Configure(State.Processing)
                .OnEntry<int>(Trigger.Start, arg => received = arg)
            .Build(State.Idle);

        await machine.FireAsync<int>(Trigger.Start, 99);

        Assert.Equal(99, received);
    }

    [Fact]
    public void Fire_WithArgument_ExecutesExitActionOnSourceState()
    {
        var exited = false;

        var machine = new StateMachineBuilder<State, Trigger>()
            .Configure(State.Idle)
                .OnExit(() => exited = true)
                .Permit(Trigger.Start, State.Processing)
            .Configure(State.Processing)
            .Build(State.Idle);

        machine.Fire<string>(Trigger.Start, "go");

        Assert.True(exited);
    }

    [Fact]
    public void OnEntry_WithDifferentTriggers_DispatchesCorrectly()
    {
        string? result = null;

        var machine = new StateMachineBuilder<State, Trigger>()
            .Configure(State.Idle)
                .Permit(Trigger.Start, State.Processing)
                .Permit(Trigger.Complete, State.Processing)
            .Configure(State.Processing)
                .OnEntry<string>(Trigger.Start, arg => result = $"start-{arg}")
                .OnEntry<string>(Trigger.Complete, arg => result = $"complete-{arg}")
            .Build(State.Idle);

        machine.Fire<string>(Trigger.Start, "test");

        Assert.Equal("start-test", result);
    }
}
