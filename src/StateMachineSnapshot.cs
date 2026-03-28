using System.Text.Json.Serialization;

namespace Philiprehberger.StateMachine;

/// <summary>
/// A JSON-serializable snapshot of a state machine's current state and transition history.
/// Used for persistence and restoration of state machines.
/// </summary>
/// <typeparam name="TState">The state type.</typeparam>
/// <typeparam name="TTrigger">The trigger type.</typeparam>
public sealed class StateMachineSnapshot<TState, TTrigger>
    where TState : notnull
    where TTrigger : notnull
{
    /// <summary>
    /// Gets or sets the current state at the time of the snapshot.
    /// </summary>
    [JsonPropertyName("currentState")]
    public TState CurrentState { get; set; } = default!;

    /// <summary>
    /// Gets or sets the transition history at the time of the snapshot.
    /// </summary>
    [JsonPropertyName("transitionHistory")]
    public List<TransitionHistoryEntry<TState, TTrigger>> TransitionHistory { get; set; } = [];
}

/// <summary>
/// A single entry in the serialized transition history.
/// </summary>
/// <typeparam name="TState">The state type.</typeparam>
/// <typeparam name="TTrigger">The trigger type.</typeparam>
public sealed class TransitionHistoryEntry<TState, TTrigger>
    where TState : notnull
    where TTrigger : notnull
{
    /// <summary>
    /// Gets or sets the state before the transition.
    /// </summary>
    [JsonPropertyName("fromState")]
    public TState FromState { get; set; } = default!;

    /// <summary>
    /// Gets or sets the state after the transition.
    /// </summary>
    [JsonPropertyName("toState")]
    public TState ToState { get; set; } = default!;

    /// <summary>
    /// Gets or sets the trigger that caused the transition.
    /// </summary>
    [JsonPropertyName("trigger")]
    public TTrigger Trigger { get; set; } = default!;

    /// <summary>
    /// Gets or sets the timestamp of the transition.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; }
}
