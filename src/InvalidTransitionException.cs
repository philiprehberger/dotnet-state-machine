namespace Philiprehberger.StateMachine;

/// <summary>
/// Exception thrown when attempting to fire a trigger that is not permitted
/// from the current state of a state machine.
/// </summary>
public sealed class InvalidTransitionException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of <see cref="InvalidTransitionException"/>.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="trigger">The trigger that was fired.</param>
    public InvalidTransitionException(object state, object trigger)
        : base($"No valid transition from state '{state}' for trigger '{trigger}'.")
    {
        State = state;
        Trigger = trigger;
    }

    /// <summary>
    /// Gets the state from which the invalid transition was attempted.
    /// </summary>
    public object State { get; }

    /// <summary>
    /// Gets the trigger that caused the invalid transition.
    /// </summary>
    public object Trigger { get; }
}
