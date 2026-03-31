# Philiprehberger.StateMachine

[![CI](https://github.com/philiprehberger/dotnet-state-machine/actions/workflows/ci.yml/badge.svg)](https://github.com/philiprehberger/dotnet-state-machine/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Philiprehberger.StateMachine.svg)](https://www.nuget.org/packages/Philiprehberger.StateMachine)
[![Last updated](https://img.shields.io/github/last-commit/philiprehberger/dotnet-state-machine)](https://github.com/philiprehberger/dotnet-state-machine/commits/main)

Lightweight finite state machine with fluent configuration, guard conditions, and async transition support.

## Installation

```bash
dotnet add package Philiprehberger.StateMachine
```

## Usage

```csharp
using Philiprehberger.StateMachine;

enum State { Locked, Unlocked }
enum Trigger { Coin, Push }

var machine = new StateMachineBuilder<State, Trigger>()
    .Configure(State.Locked)
        .Permit(Trigger.Coin, State.Unlocked)
    .Configure(State.Unlocked)
        .Permit(Trigger.Push, State.Locked)
    .Build(State.Locked);

machine.Fire(Trigger.Coin);
// machine.CurrentState == State.Unlocked
```

### Guard Conditions

```csharp
var machine = new StateMachineBuilder<State, Trigger>()
    .Configure(State.Locked)
        .PermitIf(Trigger.Coin, State.Unlocked, () => HasValidCoin())
    .Build(State.Locked);

bool canFire = machine.CanFire(Trigger.Coin);
var permitted = machine.GetPermittedTriggers();
```

### Entry and Exit Actions

```csharp
var machine = new StateMachineBuilder<State, Trigger>()
    .Configure(State.Unlocked)
        .OnEntry(() => Console.WriteLine("Gate opened"))
        .OnExit(() => Console.WriteLine("Gate closing"))
        .Permit(Trigger.Push, State.Locked)
    .Build(State.Unlocked);
```

### Async Actions

```csharp
var machine = new StateMachineBuilder<State, Trigger>()
    .Configure(State.Locked)
        .OnEntryAsync(async () => await NotifyAsync("Locked"))
        .OnExitAsync(async () => await LogAsync("Leaving locked"))
        .Permit(Trigger.Coin, State.Unlocked)
    .Build(State.Locked);

await machine.FireAsync(Trigger.Coin);
```

### Transition History

```csharp
var machine = new StateMachineBuilder<State, Trigger>()
    .WithMaxHistorySize(50)
    .Configure(State.Locked)
        .Permit(Trigger.Coin, State.Unlocked)
    .Configure(State.Unlocked)
        .Permit(Trigger.Push, State.Locked)
    .Build(State.Locked);

machine.Fire(Trigger.Coin);

foreach (var record in machine.TransitionHistory)
{
    Console.WriteLine($"{record.FromState} -> {record.ToState} via {record.Trigger} at {record.Timestamp}");
}
```

### Hierarchical Substates

```csharp
enum State { Active, Running, Paused, Inactive }
enum Trigger { Start, Pause, Resume, Stop }

var machine = new StateMachineBuilder<State, Trigger>()
    .Configure(State.Active)
        .Permit(Trigger.Stop, State.Inactive)
    .Configure(State.Running)
        .SubstateOf(State.Active)
        .Permit(Trigger.Pause, State.Paused)
    .Configure(State.Paused)
        .SubstateOf(State.Active)
        .Permit(Trigger.Resume, State.Running)
    .Build(State.Running);

machine.IsInState(State.Active);  // true (Running is a substate of Active)
machine.CanFire(Trigger.Stop);    // true (inherited from Active)
```

### Serialization and Restoration

```csharp
// Capture a snapshot
var snapshot = machine.Serialize();
string json = JsonSerializer.Serialize(snapshot);

// Restore from snapshot
var deserialized = JsonSerializer.Deserialize<StateMachineSnapshot<State, Trigger>>(json);
var restored = StateMachine<State, Trigger>.Restore(deserialized, builder);
```

## API

### `StateMachineBuilder<TState, TTrigger>`

| Method | Description |
|--------|-------------|
| `Configure(TState state)` | Begin configuring a state, returns `StateConfiguration` |
| `WithMaxHistorySize(int maxSize)` | Set the maximum number of transition history entries (default 100) |
| `Build(TState initialState)` | Create the state machine with the specified initial state |

### `StateConfiguration<TState, TTrigger>`

| Method | Description |
|--------|-------------|
| `Permit(TTrigger, TState)` | Allow a transition on the given trigger |
| `PermitIf(TTrigger, TState, Func<bool>)` | Allow a transition with a guard condition |
| `OnEntry(Action)` | Execute action when entering this state |
| `OnExit(Action)` | Execute action when exiting this state |
| `OnEntryAsync(Func<Task>)` | Execute async action when entering this state |
| `OnExitAsync(Func<Task>)` | Execute async action when exiting this state |
| `SubstateOf(TState parentState)` | Declare this state as a substate of the given parent |

### `StateMachine<TState, TTrigger>`

| Member | Description |
|--------|-------------|
| `CurrentState` | The current state of the machine |
| `TransitionHistory` | Read-only list of recorded transitions |
| `MaxHistorySize` | Maximum number of history entries retained |
| `Fire(TTrigger)` | Fire a trigger synchronously |
| `FireAsync(TTrigger)` | Fire a trigger asynchronously |
| `CanFire(TTrigger)` | Check if a trigger can be fired from the current state |
| `GetPermittedTriggers()` | Get all triggers permitted from the current state |
| `IsInState(TState)` | Check if the machine is in the given state or a substate thereof |
| `Serialize()` | Create a JSON-serializable snapshot of the machine |
| `Restore(snapshot, builder)` | Static method to rebuild a machine from a snapshot |

### `TransitionRecord<TState, TTrigger>`

| Property | Type | Description |
|----------|------|-------------|
| `FromState` | `TState` | The state before the transition |
| `ToState` | `TState` | The state after the transition |
| `Trigger` | `TTrigger` | The trigger that caused the transition |
| `Timestamp` | `DateTimeOffset` | When the transition occurred |

### `InvalidTransitionException`

Thrown when attempting to fire a trigger that is not permitted from the current state.

## Development

```bash
dotnet build src/Philiprehberger.StateMachine.csproj --configuration Release
```

## Support

If you find this project useful:

⭐ [Star the repo](https://github.com/philiprehberger/dotnet-state-machine)

🐛 [Report issues](https://github.com/philiprehberger/dotnet-state-machine/issues?q=is%3Aissue+is%3Aopen+label%3Abug)

💡 [Suggest features](https://github.com/philiprehberger/dotnet-state-machine/issues?q=is%3Aissue+is%3Aopen+label%3Aenhancement)

❤️ [Sponsor development](https://github.com/sponsors/philiprehberger)

🌐 [All Open Source Projects](https://philiprehberger.com/open-source-packages)

💻 [GitHub Profile](https://github.com/philiprehberger)

🔗 [LinkedIn Profile](https://www.linkedin.com/in/philiprehberger)

## License

[MIT](LICENSE)
