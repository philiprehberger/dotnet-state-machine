namespace Philiprehberger.StateMachine;

/// <summary>
/// Represents a transition that fires automatically after a specified timeout
/// if the machine remains in the configured state.
/// </summary>
/// <typeparam name="TState">The state type.</typeparam>
/// <typeparam name="TTrigger">The trigger type.</typeparam>
internal sealed class TimeoutTransition<TState, TTrigger>
    where TState : notnull
    where TTrigger : notnull
{
    /// <summary>
    /// Initializes a new timeout transition.
    /// </summary>
    internal TimeoutTransition(TTrigger trigger, TState targetState, TimeSpan timeout)
    {
        Trigger = trigger;
        TargetState = targetState;
        Timeout = timeout;
    }

    /// <summary>
    /// Gets the trigger that will be auto-fired.
    /// </summary>
    internal TTrigger Trigger { get; }

    /// <summary>
    /// Gets the target state for the timeout transition.
    /// </summary>
    internal TState TargetState { get; }

    /// <summary>
    /// Gets the timeout duration.
    /// </summary>
    internal TimeSpan Timeout { get; }
}
