using Xunit;
using Philiprehberger.StateMachine;

namespace Philiprehberger.StateMachine.Tests;

public class SubstateTests
{
    private enum State { Active, Running, Paused, Inactive }
    private enum Trigger { Start, Pause, Resume, Stop }

    [Fact]
    public void IsInState_ReturnsTrueForCurrentState()
    {
        var machine = new StateMachineBuilder<State, Trigger>()
            .Configure(State.Active)
            .Build(State.Active);

        Assert.True(machine.IsInState(State.Active));
        Assert.False(machine.IsInState(State.Inactive));
    }

    [Fact]
    public void IsInState_ReturnsTrueForParentOfCurrentSubstate()
    {
        var machine = new StateMachineBuilder<State, Trigger>()
            .Configure(State.Active)
                .Permit(Trigger.Start, State.Running)
            .Configure(State.Running)
                .SubstateOf(State.Active)
                .Permit(Trigger.Pause, State.Paused)
            .Configure(State.Paused)
                .SubstateOf(State.Active)
                .Permit(Trigger.Resume, State.Running)
            .Build(State.Running);

        Assert.True(machine.IsInState(State.Running));
        Assert.True(machine.IsInState(State.Active));
        Assert.False(machine.IsInState(State.Inactive));
    }

    [Fact]
    public void Substate_InheritsParentTransitions()
    {
        var machine = new StateMachineBuilder<State, Trigger>()
            .Configure(State.Active)
                .Permit(Trigger.Stop, State.Inactive)
            .Configure(State.Running)
                .SubstateOf(State.Active)
                .Permit(Trigger.Pause, State.Paused)
            .Configure(State.Paused)
                .SubstateOf(State.Active)
            .Configure(State.Inactive)
            .Build(State.Running);

        // Running inherits Stop -> Inactive from Active
        Assert.True(machine.CanFire(Trigger.Stop));

        machine.Fire(Trigger.Stop);
        Assert.Equal(State.Inactive, machine.CurrentState);
    }

    [Fact]
    public void GetPermittedTriggers_IncludesInheritedTriggers()
    {
        var machine = new StateMachineBuilder<State, Trigger>()
            .Configure(State.Active)
                .Permit(Trigger.Stop, State.Inactive)
            .Configure(State.Running)
                .SubstateOf(State.Active)
                .Permit(Trigger.Pause, State.Paused)
            .Configure(State.Paused)
                .SubstateOf(State.Active)
            .Configure(State.Inactive)
            .Build(State.Running);

        var triggers = machine.GetPermittedTriggers();

        Assert.Contains(Trigger.Pause, triggers);
        Assert.Contains(Trigger.Stop, triggers);
    }

    [Fact]
    public void Substate_OwnTransitionTakesPrecedence()
    {
        // Running defines its own Pause transition; it should work even though
        // the parent Active does not define Pause
        var machine = new StateMachineBuilder<State, Trigger>()
            .Configure(State.Active)
                .Permit(Trigger.Stop, State.Inactive)
            .Configure(State.Running)
                .SubstateOf(State.Active)
                .Permit(Trigger.Pause, State.Paused)
            .Configure(State.Paused)
                .SubstateOf(State.Active)
                .Permit(Trigger.Resume, State.Running)
            .Configure(State.Inactive)
            .Build(State.Running);

        machine.Fire(Trigger.Pause);
        Assert.Equal(State.Paused, machine.CurrentState);
        Assert.True(machine.IsInState(State.Active));

        machine.Fire(Trigger.Resume);
        Assert.Equal(State.Running, machine.CurrentState);
    }
}
