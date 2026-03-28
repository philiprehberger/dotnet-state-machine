using Xunit;
using System.Text.Json;
using Philiprehberger.StateMachine;

namespace Philiprehberger.StateMachine.Tests;

public class SerializationTests
{
    private enum State { A, B, C }
    private enum Trigger { Go, Next }

    [Fact]
    public void Serialize_CapturesCurrentStateAndHistory()
    {
        var machine = new StateMachineBuilder<State, Trigger>()
            .Configure(State.A)
                .Permit(Trigger.Go, State.B)
            .Configure(State.B)
                .Permit(Trigger.Next, State.C)
            .Build(State.A);

        machine.Fire(Trigger.Go);
        machine.Fire(Trigger.Next);

        var snapshot = machine.Serialize();

        Assert.Equal(State.C, snapshot.CurrentState);
        Assert.Equal(2, snapshot.TransitionHistory.Count);
        Assert.Equal(State.A, snapshot.TransitionHistory[0].FromState);
        Assert.Equal(State.B, snapshot.TransitionHistory[0].ToState);
    }

    [Fact]
    public void Restore_RebuildsStateMachineFromSnapshot()
    {
        var builder = new StateMachineBuilder<State, Trigger>()
            .WithMaxHistorySize(50);
        builder.Configure(State.A)
            .Permit(Trigger.Go, State.B);
        builder.Configure(State.B)
            .Permit(Trigger.Next, State.C);
        builder.Configure(State.C);

        var original = builder.Build(State.A);
        original.Fire(Trigger.Go);

        var snapshot = original.Serialize();
        var restored = StateMachine<State, Trigger>.Restore(snapshot, builder, 50);

        Assert.Equal(State.B, restored.CurrentState);
        Assert.Single(restored.TransitionHistory);
        Assert.Equal(State.A, restored.TransitionHistory[0].FromState);
    }

    [Fact]
    public void Snapshot_IsJsonSerializable()
    {
        var machine = new StateMachineBuilder<State, Trigger>()
            .Configure(State.A)
                .Permit(Trigger.Go, State.B)
            .Configure(State.B)
            .Build(State.A);

        machine.Fire(Trigger.Go);

        var snapshot = machine.Serialize();
        var json = JsonSerializer.Serialize(snapshot);
        var deserialized = JsonSerializer.Deserialize<StateMachineSnapshot<State, Trigger>>(json);

        Assert.NotNull(deserialized);
        Assert.Equal(State.B, deserialized.CurrentState);
        Assert.Single(deserialized.TransitionHistory);
    }

    [Fact]
    public void Restore_TrimsHistoryToMaxSize()
    {
        var builder = new StateMachineBuilder<State, Trigger>();
        builder.Configure(State.A)
            .Permit(Trigger.Go, State.B);
        builder.Configure(State.B)
            .Permit(Trigger.Next, State.C);
        builder.Configure(State.C)
            .Permit(Trigger.Go, State.A);

        var machine = builder.Build(State.A);
        machine.Fire(Trigger.Go);
        machine.Fire(Trigger.Next);
        machine.Fire(Trigger.Go);

        var snapshot = machine.Serialize();

        // Restore with max 2 — should trim oldest
        var restored = StateMachine<State, Trigger>.Restore(snapshot, builder, 2);

        Assert.Equal(2, restored.TransitionHistory.Count);
        Assert.Equal(State.B, restored.TransitionHistory[0].FromState);
    }
}
