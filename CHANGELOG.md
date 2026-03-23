# Changelog

## 0.1.0 (2026-03-22)

- Initial release
- `StateMachine<TState, TTrigger>` with `Fire`, `FireAsync`, `CanFire`, `GetPermittedTriggers`
- Fluent `StateConfiguration` with `Permit`, `PermitIf`, `OnEntry`, `OnExit`
- Async entry/exit actions via `OnEntryAsync`, `OnExitAsync`
- `StateMachineBuilder` for fluent configuration and construction
- `InvalidTransitionException` for illegal state transitions
