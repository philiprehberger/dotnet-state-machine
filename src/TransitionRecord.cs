namespace Philiprehberger.StateMachine;

/// <summary>
/// Represents a single recorded transition in the state machine history.
/// </summary>
/// <typeparam name="TState">The state type.</typeparam>
/// <typeparam name="TTrigger">The trigger type.</typeparam>
public sealed record TransitionRecord<TState, TTrigger>(
    TState FromState,
    TState ToState,
    TTrigger Trigger,
    DateTimeOffset Timestamp)
    where TState : notnull
    where TTrigger : notnull;
