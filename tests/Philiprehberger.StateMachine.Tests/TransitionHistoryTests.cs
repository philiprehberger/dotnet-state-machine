using Xunit;
using Philiprehberger.StateMachine;

namespace Philiprehberger.StateMachine.Tests;

public class TransitionHistoryTests
{
    private enum State { A, B, C }
    private enum Trigger { Go, Next }

    [Fact]
    public void TransitionHistory_IsEmptyInitially()
    {
        var machine = new StateMachineBuilder<State, Trigger>()
            .Configure(State.A)
                .Permit(Trigger.Go, State.B)
            .Build(State.A);

        Assert.Empty(machine.TransitionHistory);
    }

    [Fact]
    public void Fire_RecordsTransitionInHistory()
    {
        var machine = new StateMachineBuilder<State, Trigger>()
            .Configure(State.A)
                .Permit(Trigger.Go, State.B)
            .Configure(State.B)
            .Build(State.A);

        machine.Fire(Trigger.Go);

        Assert.Single(machine.TransitionHistory);
        var record = machine.TransitionHistory[0];
        Assert.Equal(State.A, record.FromState);
        Assert.Equal(State.B, record.ToState);
        Assert.Equal(Trigger.Go, record.Trigger);
        Assert.True(record.Timestamp <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Fire_RecordsMultipleTransitions()
    {
        var machine = new StateMachineBuilder<State, Trigger>()
            .Configure(State.A)
                .Permit(Trigger.Go, State.B)
            .Configure(State.B)
                .Permit(Trigger.Next, State.C)
            .Build(State.A);

        machine.Fire(Trigger.Go);
        machine.Fire(Trigger.Next);

        Assert.Equal(2, machine.TransitionHistory.Count);
        Assert.Equal(State.A, machine.TransitionHistory[0].FromState);
        Assert.Equal(State.B, machine.TransitionHistory[1].FromState);
    }

    [Fact]
    public void MaxHistorySize_DropsOldestEntries()
    {
        var machine = new StateMachineBuilder<State, Trigger>()
            .WithMaxHistorySize(2)
            .Configure(State.A)
                .Permit(Trigger.Go, State.B)
            .Configure(State.B)
                .Permit(Trigger.Next, State.C)
            .Configure(State.C)
                .Permit(Trigger.Go, State.A)
            .Build(State.A);

        machine.Fire(Trigger.Go);   // A -> B
        machine.Fire(Trigger.Next);  // B -> C
        machine.Fire(Trigger.Go);   // C -> A

        Assert.Equal(2, machine.TransitionHistory.Count);
        Assert.Equal(State.B, machine.TransitionHistory[0].FromState);
        Assert.Equal(State.C, machine.TransitionHistory[1].FromState);
    }

    [Fact]
    public async Task FireAsync_AlsoRecordsHistory()
    {
        var machine = new StateMachineBuilder<State, Trigger>()
            .Configure(State.A)
                .Permit(Trigger.Go, State.B)
            .Configure(State.B)
            .Build(State.A);

        await machine.FireAsync(Trigger.Go);

        Assert.Single(machine.TransitionHistory);
        Assert.Equal(State.A, machine.TransitionHistory[0].FromState);
    }
}
