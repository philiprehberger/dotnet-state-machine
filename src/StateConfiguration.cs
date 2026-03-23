namespace Philiprehberger.StateMachine;

/// <summary>
/// Fluent builder for configuring transitions and actions for a single state.
/// </summary>
/// <typeparam name="TState">The state type.</typeparam>
/// <typeparam name="TTrigger">The trigger type.</typeparam>
public sealed class StateConfiguration<TState, TTrigger>
    where TState : notnull
    where TTrigger : notnull
{
    private readonly StateMachineBuilder<TState, TTrigger> _builder;
    private readonly TState _state;
    internal readonly List<TransitionRule<TState, TTrigger>> Transitions = [];
    internal Action? EntryAction;
    internal Action? ExitAction;
    internal Func<Task>? EntryAsyncAction;
    internal Func<Task>? ExitAsyncAction;

    internal StateConfiguration(StateMachineBuilder<TState, TTrigger> builder, TState state)
    {
        _builder = builder;
        _state = state;
    }

    /// <summary>
    /// Permits a transition to the target state when the specified trigger is fired.
    /// </summary>
    /// <param name="trigger">The trigger that causes the transition.</param>
    /// <param name="targetState">The state to transition to.</param>
    /// <returns>This configuration for chaining.</returns>
    public StateConfiguration<TState, TTrigger> Permit(TTrigger trigger, TState targetState)
    {
        Transitions.Add(new TransitionRule<TState, TTrigger>(trigger, targetState, guard: null));
        return this;
    }

    /// <summary>
    /// Permits a transition to the target state when the specified trigger is fired
    /// and the guard condition returns <c>true</c>.
    /// </summary>
    /// <param name="trigger">The trigger that causes the transition.</param>
    /// <param name="targetState">The state to transition to.</param>
    /// <param name="guard">A function that must return <c>true</c> for the transition to occur.</param>
    /// <returns>This configuration for chaining.</returns>
    public StateConfiguration<TState, TTrigger> PermitIf(TTrigger trigger, TState targetState, Func<bool> guard)
    {
        Transitions.Add(new TransitionRule<TState, TTrigger>(trigger, targetState, guard));
        return this;
    }

    /// <summary>
    /// Registers an action to execute when entering this state.
    /// </summary>
    /// <param name="action">The action to execute on entry.</param>
    /// <returns>This configuration for chaining.</returns>
    public StateConfiguration<TState, TTrigger> OnEntry(Action action)
    {
        EntryAction = action;
        return this;
    }

    /// <summary>
    /// Registers an action to execute when exiting this state.
    /// </summary>
    /// <param name="action">The action to execute on exit.</param>
    /// <returns>This configuration for chaining.</returns>
    public StateConfiguration<TState, TTrigger> OnExit(Action action)
    {
        ExitAction = action;
        return this;
    }

    /// <summary>
    /// Registers an async action to execute when entering this state.
    /// </summary>
    /// <param name="func">The async function to execute on entry.</param>
    /// <returns>This configuration for chaining.</returns>
    public StateConfiguration<TState, TTrigger> OnEntryAsync(Func<Task> func)
    {
        EntryAsyncAction = func;
        return this;
    }

    /// <summary>
    /// Registers an async action to execute when exiting this state.
    /// </summary>
    /// <param name="func">The async function to execute on exit.</param>
    /// <returns>This configuration for chaining.</returns>
    public StateConfiguration<TState, TTrigger> OnExitAsync(Func<Task> func)
    {
        ExitAsyncAction = func;
        return this;
    }

    /// <summary>
    /// Begins configuring another state. Delegates to the parent builder.
    /// </summary>
    /// <param name="state">The state to configure.</param>
    /// <returns>A new state configuration for the specified state.</returns>
    public StateConfiguration<TState, TTrigger> Configure(TState state)
    {
        return _builder.Configure(state);
    }

    /// <summary>
    /// Builds the state machine with the specified initial state. Delegates to the parent builder.
    /// </summary>
    /// <param name="initialState">The initial state of the machine.</param>
    /// <returns>A configured state machine.</returns>
    public StateMachine<TState, TTrigger> Build(TState initialState)
    {
        return _builder.Build(initialState);
    }
}

/// <summary>
/// Represents a single transition rule from a state.
/// </summary>
/// <typeparam name="TState">The state type.</typeparam>
/// <typeparam name="TTrigger">The trigger type.</typeparam>
internal sealed class TransitionRule<TState, TTrigger>
    where TState : notnull
    where TTrigger : notnull
{
    /// <summary>
    /// Initializes a new transition rule.
    /// </summary>
    internal TransitionRule(TTrigger trigger, TState targetState, Func<bool>? guard)
    {
        Trigger = trigger;
        TargetState = targetState;
        Guard = guard;
    }

    /// <summary>
    /// Gets the trigger that activates this transition.
    /// </summary>
    internal TTrigger Trigger { get; }

    /// <summary>
    /// Gets the target state of this transition.
    /// </summary>
    internal TState TargetState { get; }

    /// <summary>
    /// Gets the optional guard condition for this transition.
    /// </summary>
    internal Func<bool>? Guard { get; }

    /// <summary>
    /// Returns <c>true</c> if the guard condition is met or no guard is set.
    /// </summary>
    internal bool IsPermitted => Guard is null || Guard();
}
