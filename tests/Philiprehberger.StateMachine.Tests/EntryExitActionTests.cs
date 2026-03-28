using Xunit;
using Philiprehberger.StateMachine;

namespace Philiprehberger.StateMachine.Tests;

public class EntryExitActionTests
{
    private enum State { A, B, C }
    private enum Trigger { Go, Back }

    [Fact]
    public void Fire_ExecutesExitActionOnSourceState()
    {
        var exited = false;

        var machine = new StateMachineBuilder<State, Trigger>()
            .Configure(State.A)
                .OnExit(() => exited = true)
                .Permit(Trigger.Go, State.B)
            .Configure(State.B)
            .Build(State.A);

        machine.Fire(Trigger.Go);

        Assert.True(exited);
    }

    [Fact]
    public void Fire_ExecutesEntryActionOnTargetState()
    {
        var entered = false;

        var machine = new StateMachineBuilder<State, Trigger>()
            .Configure(State.A)
                .Permit(Trigger.Go, State.B)
            .Configure(State.B)
                .OnEntry(() => entered = true)
            .Build(State.A);

        machine.Fire(Trigger.Go);

        Assert.True(entered);
    }

    [Fact]
    public void Fire_ExecutesExitBeforeEntry()
    {
        var order = new List<string>();

        var machine = new StateMachineBuilder<State, Trigger>()
            .Configure(State.A)
                .OnExit(() => order.Add("exit-A"))
                .Permit(Trigger.Go, State.B)
            .Configure(State.B)
                .OnEntry(() => order.Add("entry-B"))
            .Build(State.A);

        machine.Fire(Trigger.Go);

        Assert.Equal(["exit-A", "entry-B"], order);
    }

    [Fact]
    public async Task FireAsync_ExecutesAsyncEntryAndExitActions()
    {
        var order = new List<string>();

        var machine = new StateMachineBuilder<State, Trigger>()
            .Configure(State.A)
                .OnExitAsync(async () =>
                {
                    await Task.Delay(1);
                    order.Add("async-exit-A");
                })
                .Permit(Trigger.Go, State.B)
            .Configure(State.B)
                .OnEntryAsync(async () =>
                {
                    await Task.Delay(1);
                    order.Add("async-entry-B");
                })
            .Build(State.A);

        await machine.FireAsync(Trigger.Go);

        Assert.Equal(["async-exit-A", "async-entry-B"], order);
    }

    [Fact]
    public async Task FireAsync_ExecutesSyncAndAsyncActions()
    {
        var order = new List<string>();

        var machine = new StateMachineBuilder<State, Trigger>()
            .Configure(State.A)
                .OnExit(() => order.Add("sync-exit-A"))
                .OnExitAsync(async () =>
                {
                    await Task.Delay(1);
                    order.Add("async-exit-A");
                })
                .Permit(Trigger.Go, State.B)
            .Configure(State.B)
                .OnEntry(() => order.Add("sync-entry-B"))
                .OnEntryAsync(async () =>
                {
                    await Task.Delay(1);
                    order.Add("async-entry-B");
                })
            .Build(State.A);

        await machine.FireAsync(Trigger.Go);

        Assert.Equal(["sync-exit-A", "async-exit-A", "sync-entry-B", "async-entry-B"], order);
    }
}
