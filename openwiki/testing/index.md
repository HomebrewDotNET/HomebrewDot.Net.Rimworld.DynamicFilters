# Files

- [Integration Tests](integration-tests.md) - The tests/Integration xUnit suite that stands up the Toolkit indexing pipeline and the mod's registries in pure .NET — preset def collection, policy/template registry lifecycle, and ThingFilter indexing persistence.
- [Testing Overview](overview.md) - The two xUnit test layers of the mod — tests/Unit (pure logic, no game assemblies) and tests/Integration (indexing pipeline and registries against a stubbed Toolkit environment) — and when to run which.
- [Unit Tests](unit-tests.md) - The tests/Unit xUnit suite covering delegate filter/policy mechanics, ActivatedPolicies, StateStore, template validation matrices, BlocksWindmillPolicy, MapPolicyManager smoke tests, and preset condition structure/behavior.
