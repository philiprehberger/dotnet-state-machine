# Philiprehberger.StateMachine

[![CI](https://github.com/philiprehberger/dotnet-state-machine/actions/workflows/ci.yml/badge.svg)](https://github.com/philiprehberger/dotnet-state-machine/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Philiprehberger.StateMachine.svg)](https://www.nuget.org/packages/Philiprehberger.StateMachine)
[![License](https://img.shields.io/github/license/philiprehberger/dotnet-state-machine)](LICENSE)
[![Sponsor](https://img.shields.io/badge/sponsor-GitHub%20Sponsors-ec6cb9)](https://github.com/sponsors/philiprehberger)

Lightweight finite state machine with fluent configuration, guard conditions, and async transition support.

## Installation

```bash
dotnet add package Philiprehberger.StateMachine
```

## Usage

### Basic State Machine

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

machine.Fire(Trigger.Push);
// machine.CurrentState == State.Locked
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

### Async Support

```csharp
var machine = new StateMachineBuilder<State, Trigger>()
    .Configure(State.Locked)
        .OnEntryAsync(async () => await NotifyAsync("Locked"))
        .OnExitAsync(async () => await LogAsync("Leaving locked"))
        .Permit(Trigger.Coin, State.Unlocked)
    .Build(State.Locked);

await machine.FireAsync(Trigger.Coin);
```

## API

### `StateMachineBuilder<TState, TTrigger>`

| Method | Description |
|--------|-------------|
| `Configure(TState state)` | Begin configuring a state, returns `StateConfiguration` |
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

### `StateMachine<TState, TTrigger>`

| Member | Description |
|--------|-------------|
| `CurrentState` | The current state of the machine |
| `Fire(TTrigger)` | Fire a trigger synchronously |
| `FireAsync(TTrigger)` | Fire a trigger asynchronously |
| `CanFire(TTrigger)` | Check if a trigger can be fired from the current state |
| `GetPermittedTriggers()` | Get all triggers permitted from the current state |

### `InvalidTransitionException`

Thrown when attempting to fire a trigger that is not permitted from the current state.

## Development

```bash
dotnet build src/Philiprehberger.StateMachine.csproj --configuration Release
```

## License

MIT
