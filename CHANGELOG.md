# Changelog

## 0.2.1 (2026-03-31)

- Standardize README to 3-badge format with emoji Support section
- Update CI actions to v5 for Node.js 24 compatibility

## 0.2.0 (2026-03-27)

- Add entry and exit actions on states (sync and async)
- Add transition history tracking with configurable depth
- Add hierarchical substates with inherited transitions
- Add state machine serialization and restoration from snapshots

## 0.1.0 (2026-03-22)

- Initial release
- `StateMachine<TState, TTrigger>` with `Fire`, `FireAsync`, `CanFire`, `GetPermittedTriggers`
- Fluent `StateConfiguration` with `Permit`, `PermitIf`, `OnEntry`, `OnExit`
- Async entry/exit actions via `OnEntryAsync`, `OnExitAsync`
- `StateMachineBuilder` for fluent configuration and construction
- `InvalidTransitionException` for illegal state transitions
