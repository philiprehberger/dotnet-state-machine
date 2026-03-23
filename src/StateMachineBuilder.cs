namespace Philiprehberger.StateMachine;

/// <summary>
/// Fluent builder for constructing a <see cref="StateMachine{TState, TTrigger}"/>.
/// </summary>
/// <typeparam name="TState">The state type.</typeparam>
/// <typeparam name="TTrigger">The trigger type.</typeparam>
public sealed class StateMachineBuilder<TState, TTrigger>
    where TState : notnull
    where TTrigger : notnull
{
    internal readonly Dictionary<TState, StateConfiguration<TState, TTrigger>> Configurations = [];

    /// <summary>
    /// Begins configuring the specified state.
    /// If the state has already been configured, returns the existing configuration.
    /// </summary>
    /// <param name="state">The state to configure.</param>
    /// <returns>A state configuration for fluent chaining.</returns>
    public StateConfiguration<TState, TTrigger> Configure(TState state)
    {
        if (!Configurations.TryGetValue(state, out var config))
        {
            config = new StateConfiguration<TState, TTrigger>(this, state);
            Configurations[state] = config;
        }

        return config;
    }

    /// <summary>
    /// Builds the state machine with the specified initial state.
    /// </summary>
    /// <param name="initialState">The initial state of the machine.</param>
    /// <returns>A fully configured <see cref="StateMachine{TState, TTrigger}"/>.</returns>
    public StateMachine<TState, TTrigger> Build(TState initialState)
    {
        return new StateMachine<TState, TTrigger>(initialState, Configurations);
    }
}
