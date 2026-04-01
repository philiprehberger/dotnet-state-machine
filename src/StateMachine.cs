using System.Text;

namespace Philiprehberger.StateMachine;

/// <summary>
/// A lightweight finite state machine with support for guard conditions, async transitions,
/// transition history, hierarchical substates, parameterized triggers, timeout transitions,
/// and serialization.
/// </summary>
/// <typeparam name="TState">The state type (typically an enum).</typeparam>
/// <typeparam name="TTrigger">The trigger type (typically an enum).</typeparam>
public sealed class StateMachine<TState, TTrigger>
    where TState : notnull
    where TTrigger : notnull
{
    private readonly Dictionary<TState, StateConfiguration<TState, TTrigger>> _configurations;
    private readonly List<TransitionRecord<TState, TTrigger>> _history = [];
    private readonly int _maxHistorySize;
    private readonly Dictionary<TState, CancellationTokenSource> _activeTimeouts = [];
    private TState _currentState;

    internal StateMachine(
        TState initialState,
        Dictionary<TState, StateConfiguration<TState, TTrigger>> configurations,
        int maxHistorySize = 100)
    {
        _currentState = initialState;
        _configurations = configurations;
        _maxHistorySize = maxHistorySize;

        StartTimeoutsForState(initialState);
    }

    /// <summary>
    /// Gets the current state of the state machine.
    /// </summary>
    public TState CurrentState => _currentState;

    /// <summary>
    /// Gets the transition history of the state machine.
    /// </summary>
    public IReadOnlyList<TransitionRecord<TState, TTrigger>> TransitionHistory => _history.AsReadOnly();

    /// <summary>
    /// Gets the maximum number of history entries retained.
    /// </summary>
    public int MaxHistorySize => _maxHistorySize;

    /// <summary>
    /// Checks whether the machine is currently in the specified state.
    /// Returns <c>true</c> if the current state equals the given state,
    /// or if the current state is a substate of the given state (at any depth).
    /// </summary>
    /// <param name="state">The state to check.</param>
    /// <returns><c>true</c> if the machine is in the given state or a substate thereof.</returns>
    public bool IsInState(TState state)
    {
        var comparer = EqualityComparer<TState>.Default;

        if (comparer.Equals(_currentState, state))
        {
            return true;
        }

        // Walk up the parent chain from the current state
        var current = _currentState;
        while (_configurations.TryGetValue(current, out var config) && config.HasParent)
        {
            if (comparer.Equals(config.ParentState!, state))
            {
                return true;
            }

            current = config.ParentState!;
        }

        return false;
    }

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

        CancelTimeoutsForState(previousState);
        ExecuteExitActions(previousState);
        _currentState = targetState;
        ExecuteEntryActions(targetState);
        StartTimeoutsForState(targetState);

        RecordTransition(previousState, targetState, trigger);
    }

    /// <summary>
    /// Fires a trigger with an argument, transitioning the machine to a new state.
    /// If the target state has a parameterized entry action registered for this trigger,
    /// it receives the argument.
    /// </summary>
    /// <typeparam name="TArg">The type of the argument.</typeparam>
    /// <param name="trigger">The trigger to fire.</param>
    /// <param name="arg">The argument to pass to the parameterized entry action.</param>
    /// <exception cref="InvalidTransitionException">Thrown when no valid transition exists.</exception>
    public void Fire<TArg>(TTrigger trigger, TArg arg)
    {
        var rule = FindTransitionRule(trigger);
        var previousState = _currentState;
        var targetState = rule.TargetState;

        CancelTimeoutsForState(previousState);
        ExecuteExitActions(previousState);
        _currentState = targetState;
        ExecuteEntryActions(targetState, trigger, arg);
        StartTimeoutsForState(targetState);

        RecordTransition(previousState, targetState, trigger);
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

        CancelTimeoutsForState(previousState);
        await ExecuteExitActionsAsync(previousState).ConfigureAwait(false);
        _currentState = targetState;
        await ExecuteEntryActionsAsync(targetState).ConfigureAwait(false);
        StartTimeoutsForState(targetState);

        RecordTransition(previousState, targetState, trigger);
    }

    /// <summary>
    /// Fires a trigger with an argument asynchronously, transitioning the machine to a new state.
    /// If the target state has a parameterized entry action registered for this trigger,
    /// it receives the argument.
    /// </summary>
    /// <typeparam name="TArg">The type of the argument.</typeparam>
    /// <param name="trigger">The trigger to fire.</param>
    /// <param name="arg">The argument to pass to the parameterized entry action.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidTransitionException">Thrown when no valid transition exists.</exception>
    public async Task FireAsync<TArg>(TTrigger trigger, TArg arg)
    {
        var rule = FindTransitionRule(trigger);
        var previousState = _currentState;
        var targetState = rule.TargetState;

        CancelTimeoutsForState(previousState);
        await ExecuteExitActionsAsync(previousState).ConfigureAwait(false);
        _currentState = targetState;
        ExecuteParameterizedEntryAction(targetState, trigger, arg);
        if (_configurations.TryGetValue(targetState, out var config))
        {
            config.EntryAction?.Invoke();
            if (config.EntryAsyncAction is not null)
            {
                await config.EntryAsyncAction().ConfigureAwait(false);
            }
        }
        StartTimeoutsForState(targetState);

        RecordTransition(previousState, targetState, trigger);
    }

    /// <summary>
    /// Checks whether the specified trigger can be fired from the current state.
    /// Also considers transitions inherited from parent states.
    /// </summary>
    /// <param name="trigger">The trigger to check.</param>
    /// <returns><c>true</c> if the trigger can be fired; otherwise <c>false</c>.</returns>
    public bool CanFire(TTrigger trigger)
    {
        var transitions = GetEffectiveTransitions(_currentState);
        return transitions.Any(t =>
            EqualityComparer<TTrigger>.Default.Equals(t.Trigger, trigger) && t.IsPermitted);
    }

    /// <summary>
    /// Gets all triggers that are currently permitted from the current state.
    /// Includes triggers inherited from parent states.
    /// </summary>
    /// <returns>A read-only list of permitted triggers.</returns>
    public IReadOnlyList<TTrigger> GetPermittedTriggers()
    {
        var transitions = GetEffectiveTransitions(_currentState);
        return transitions
            .Where(t => t.IsPermitted)
            .Select(t => t.Trigger)
            .Distinct()
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Creates a serializable snapshot of the current state and transition history.
    /// </summary>
    /// <returns>A <see cref="StateMachineSnapshot{TState, TTrigger}"/> that can be serialized to JSON.</returns>
    public StateMachineSnapshot<TState, TTrigger> Serialize()
    {
        return new StateMachineSnapshot<TState, TTrigger>
        {
            CurrentState = _currentState,
            TransitionHistory = _history.Select(r => new TransitionHistoryEntry<TState, TTrigger>
            {
                FromState = r.FromState,
                ToState = r.ToState,
                Trigger = r.Trigger,
                Timestamp = r.Timestamp
            }).ToList()
        };
    }

    /// <summary>
    /// Restores a state machine from a snapshot and a builder that defines the state machine configuration.
    /// </summary>
    /// <param name="snapshot">The snapshot to restore from.</param>
    /// <param name="builder">The builder containing the state machine configuration.</param>
    /// <param name="maxHistorySize">The maximum number of history entries to retain. Defaults to 100.</param>
    /// <returns>A restored <see cref="StateMachine{TState, TTrigger}"/>.</returns>
    public static StateMachine<TState, TTrigger> Restore(
        StateMachineSnapshot<TState, TTrigger> snapshot,
        StateMachineBuilder<TState, TTrigger> builder,
        int maxHistorySize = 100)
    {
        var machine = new StateMachine<TState, TTrigger>(
            snapshot.CurrentState,
            builder.Configurations,
            maxHistorySize);

        foreach (var entry in snapshot.TransitionHistory)
        {
            machine._history.Add(new TransitionRecord<TState, TTrigger>(
                entry.FromState,
                entry.ToState,
                entry.Trigger,
                entry.Timestamp));
        }

        // Trim history if it exceeds the configured max
        while (machine._history.Count > maxHistorySize)
        {
            machine._history.RemoveAt(0);
        }

        return machine;
    }

    /// <summary>
    /// Generates a DOT (Graphviz) representation of the state machine graph,
    /// showing all configured states, transitions, and guard conditions.
    /// </summary>
    /// <returns>A string containing the DOT graph definition.</returns>
    public string ToDot()
    {
        var sb = new StringBuilder();
        sb.AppendLine("digraph StateMachine {");
        sb.AppendLine("    rankdir=LR;");
        sb.AppendLine("    node [shape=rectangle, style=rounded];");

        // Highlight current state
        sb.AppendLine($"    \"{_currentState}\" [style=\"rounded,filled\", fillcolor=lightblue];");

        foreach (var (state, config) in _configurations)
        {
            // Ensure the state node exists
            if (!EqualityComparer<TState>.Default.Equals(state, _currentState))
            {
                sb.AppendLine($"    \"{state}\";");
            }

            // Draw substate relationship
            if (config.HasParent)
            {
                sb.AppendLine($"    \"{state}\" -> \"{config.ParentState}\" [style=dashed, label=\"substate of\"];");
            }

            // Draw transitions
            foreach (var transition in config.Transitions)
            {
                var label = transition.Guard is not null
                    ? $"{transition.Trigger} [guarded]"
                    : $"{transition.Trigger}";

                // Check if this is a timeout transition
                var timeoutTrans = config.TimeoutTransitions
                    .FirstOrDefault(t => EqualityComparer<TTrigger>.Default.Equals(t.Trigger, transition.Trigger));
                if (timeoutTrans is not null)
                {
                    label += $" (after {timeoutTrans.Timeout.TotalSeconds}s)";
                }

                sb.AppendLine($"    \"{state}\" -> \"{transition.TargetState}\" [label=\"{label}\"];");
            }
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    /// <summary>
    /// Generates a Mermaid diagram representation of the state machine graph,
    /// showing all configured states, transitions, and guard conditions.
    /// </summary>
    /// <returns>A string containing the Mermaid diagram definition.</returns>
    public string ToMermaid()
    {
        var sb = new StringBuilder();
        sb.AppendLine("stateDiagram-v2");

        foreach (var (state, config) in _configurations)
        {
            // Draw substate relationship
            if (config.HasParent)
            {
                sb.AppendLine($"    state \"{config.ParentState}\" {{");
                sb.AppendLine($"        {state}");
                sb.AppendLine("    }");
            }

            // Draw transitions
            foreach (var transition in config.Transitions)
            {
                var label = transition.Guard is not null
                    ? $"{transition.Trigger} [guarded]"
                    : $"{transition.Trigger}";

                // Check if this is a timeout transition
                var timeoutTrans = config.TimeoutTransitions
                    .FirstOrDefault(t => EqualityComparer<TTrigger>.Default.Equals(t.Trigger, transition.Trigger));
                if (timeoutTrans is not null)
                {
                    label += $" (after {timeoutTrans.Timeout.TotalSeconds}s)";
                }

                sb.AppendLine($"    {state} --> {transition.TargetState} : {label}");
            }
        }

        // Highlight current state with a note
        sb.AppendLine($"    note right of {_currentState} : Current state");

        return sb.ToString();
    }

    private void RecordTransition(TState fromState, TState toState, TTrigger trigger)
    {
        _history.Add(new TransitionRecord<TState, TTrigger>(
            fromState,
            toState,
            trigger,
            DateTimeOffset.UtcNow));

        while (_history.Count > _maxHistorySize)
        {
            _history.RemoveAt(0);
        }
    }

    private List<TransitionRule<TState, TTrigger>> GetEffectiveTransitions(TState state)
    {
        var transitions = new List<TransitionRule<TState, TTrigger>>();
        var visited = new HashSet<TState>();
        var current = state;

        while (current is not null && visited.Add(current))
        {
            if (_configurations.TryGetValue(current, out var config))
            {
                transitions.AddRange(config.Transitions);

                if (config.HasParent)
                {
                    current = config.ParentState!;
                }
                else
                {
                    break;
                }
            }
            else
            {
                break;
            }
        }

        return transitions;
    }

    private TransitionRule<TState, TTrigger> FindTransitionRule(TTrigger trigger)
    {
        var transitions = GetEffectiveTransitions(_currentState);

        var rule = transitions.FirstOrDefault(t =>
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

    private void ExecuteEntryActions<TArg>(TState state, TTrigger trigger, TArg? arg)
    {
        if (_configurations.TryGetValue(state, out var config))
        {
            ExecuteParameterizedEntryAction(state, trigger, arg);
            config.EntryAction?.Invoke();
        }
    }

    private void ExecuteParameterizedEntryAction<TArg>(TState state, TTrigger trigger, TArg? arg)
    {
        if (_configurations.TryGetValue(state, out var config) &&
            config.ParameterizedEntryActions.TryGetValue(trigger, out var action) &&
            arg is not null)
        {
            action(arg);
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

    private void StartTimeoutsForState(TState state)
    {
        if (!_configurations.TryGetValue(state, out var config))
        {
            return;
        }

        foreach (var timeout in config.TimeoutTransitions)
        {
            var cts = new CancellationTokenSource();
            _activeTimeouts[state] = cts;
            var trigger = timeout.Trigger;
            var delay = timeout.Timeout;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(delay, cts.Token).ConfigureAwait(false);

                    // Only fire if we're still in the same state
                    if (EqualityComparer<TState>.Default.Equals(_currentState, state))
                    {
                        Fire(trigger);
                    }
                }
                catch (TaskCanceledException)
                {
                    // Timeout was cancelled because state changed
                }
            });
        }
    }

    private void CancelTimeoutsForState(TState state)
    {
        if (_activeTimeouts.TryGetValue(state, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
            _activeTimeouts.Remove(state);
        }
    }
}
