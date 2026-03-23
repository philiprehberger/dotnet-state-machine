namespace Philiprehberger.StateMachine;

/// <summary>
/// A lightweight finite state machine with support for guard conditions and async transitions.
/// </summary>
/// <typeparam name="TState">The state type (typically an enum).</typeparam>
/// <typeparam name="TTrigger">The trigger type (typically an enum).</typeparam>
public sealed class StateMachine<TState, TTrigger>
    where TState : notnull
    where TTrigger : notnull
{
    private readonly Dictionary<TState, StateConfiguration<TState, TTrigger>> _configurations;
    private TState _currentState;

    internal StateMachine(
        TState initialState,
        Dictionary<TState, StateConfiguration<TState, TTrigger>> configurations)
    {
        _currentState = initialState;
        _configurations = configurations;
    }

    /// <summary>
    /// Gets the current state of the state machine.
    /// </summary>
    public TState CurrentState => _currentState;

    /// <summary>
    /// Fires a trigger synchronously, transitioning the machine to a new state
    /// if a valid transition exists.
    /// </summary>
    /// <param name="trigger">The trigger to fire.</param>
    /// <exception cref="InvalidTransitionException">Thrown when no valid transition exists.</exception>
    public void Fire(TTrigger trigger)
    {
        var rule = FindTransitionRule(trigger);
        var previousState = _currentState;
        var targetState = rule.TargetState;

        ExecuteExitActions(previousState);
        _currentState = targetState;
        ExecuteEntryActions(targetState);
    }

    /// <summary>
    /// Fires a trigger asynchronously, transitioning the machine to a new state
    /// if a valid transition exists. Executes async entry/exit actions if configured.
    /// </summary>
    /// <param name="trigger">The trigger to fire.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidTransitionException">Thrown when no valid transition exists.</exception>
    public async Task FireAsync(TTrigger trigger)
    {
        var rule = FindTransitionRule(trigger);
        var previousState = _currentState;
        var targetState = rule.TargetState;

        await ExecuteExitActionsAsync(previousState).ConfigureAwait(false);
        _currentState = targetState;
        await ExecuteEntryActionsAsync(targetState).ConfigureAwait(false);
    }

    /// <summary>
    /// Checks whether the specified trigger can be fired from the current state.
    /// </summary>
    /// <param name="trigger">The trigger to check.</param>
    /// <returns><c>true</c> if the trigger can be fired; otherwise <c>false</c>.</returns>
    public bool CanFire(TTrigger trigger)
    {
        if (!_configurations.TryGetValue(_currentState, out var config))
        {
            return false;
        }

        return config.Transitions.Any(t =>
            EqualityComparer<TTrigger>.Default.Equals(t.Trigger, trigger) && t.IsPermitted);
    }

    /// <summary>
    /// Gets all triggers that are currently permitted from the current state.
    /// </summary>
    /// <returns>A read-only list of permitted triggers.</returns>
    public IReadOnlyList<TTrigger> GetPermittedTriggers()
    {
        if (!_configurations.TryGetValue(_currentState, out var config))
        {
            return [];
        }

        return config.Transitions
            .Where(t => t.IsPermitted)
            .Select(t => t.Trigger)
            .Distinct()
            .ToList()
            .AsReadOnly();
    }

    private TransitionRule<TState, TTrigger> FindTransitionRule(TTrigger trigger)
    {
        if (!_configurations.TryGetValue(_currentState, out var config))
        {
            throw new InvalidTransitionException(_currentState, trigger);
        }

        var rule = config.Transitions.FirstOrDefault(t =>
            EqualityComparer<TTrigger>.Default.Equals(t.Trigger, trigger) && t.IsPermitted);

        if (rule is null)
        {
            throw new InvalidTransitionException(_currentState, trigger);
        }

        return rule;
    }

    private void ExecuteExitActions(TState state)
    {
        if (_configurations.TryGetValue(state, out var config))
        {
            config.ExitAction?.Invoke();
        }
    }

    private void ExecuteEntryActions(TState state)
    {
        if (_configurations.TryGetValue(state, out var config))
        {
            config.EntryAction?.Invoke();
        }
    }

    private async Task ExecuteExitActionsAsync(TState state)
    {
        if (_configurations.TryGetValue(state, out var config))
        {
            config.ExitAction?.Invoke();
            if (config.ExitAsyncAction is not null)
            {
                await config.ExitAsyncAction().ConfigureAwait(false);
            }
        }
    }

    private async Task ExecuteEntryActionsAsync(TState state)
    {
        if (_configurations.TryGetValue(state, out var config))
        {
            config.EntryAction?.Invoke();
            if (config.EntryAsyncAction is not null)
            {
                await config.EntryAsyncAction().ConfigureAwait(false);
            }
        }
    }
}
