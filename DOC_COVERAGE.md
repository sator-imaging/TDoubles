# Documentation Coverage Report - TDoubles

## Overall Check Result

The documentation for `TDoubles` provides a solid foundation for core features, but there is a noticeable gap between the current implementation state and what is described in the `README.md` and supplementary documentation.

**Summary:**
- **Core Mocking:** Well-documented and extensively tested.
- **Advanced Features:** Several recently added features (Parameter Defaults, Inner Classes, Partial Overrides) are tested but missing from the main documentation.
- **Static Class Mocking:** This is documented as a major feature in `docs/advanced-usage.md`, but the implementation is currently missing or incomplete.
- **TODO Alignment:** Some items marked as completed in the `README.md` TODO list (specifically `MockCallCounts`) are not yet present in the codebase.

---

## Detailed Result Follows

### 1. Implemented and Well-Documented
These features are both present in the codebase (with tests) and clearly explained in the `README.md` or `docs/`.

- **Basic Mocking:** Interfaces, Abstract Classes, and Concrete Classes.
- **Value Types:** `record`, `record struct`, and `struct` mocking.
- **Generics:** Support for unbound and closed generics, including complex type constraints.
- **Customization:** `IncludeInternals` flag and Member Exclusion (via `MockAttribute` constructor).
- **Callbacks:** Unified `OnWillMockCall` for spying and custom behavior.
- **Member Types:** Properties, Events, Indexers, and Method Overloading.
- **Language Features:** `ref`, `out`, and `in` parameter handling.
- **Standard Overrides:** Proper handling of `ToString()`, `Equals()`, and `GetHashCode()`.

### 2. Implemented but Missing from Documentation
These features have dedicated test projects (Test31-Test39) but are not explicitly mentioned in the main `README.md`.

- **Method Parameter Defaults:** Preservation of default values in generated mock methods (Test37).
- **Inner Class Mocking:** Ability to mock nested/inner types (Test38).
- **Partial Mock Overrides:** The `MockOverrideContainer` is generated as `sealed partial`, allowing users to extend it with custom logic or interfaces (Test39).
- **Auto-Exclusion of Declaring Members:** The generator automatically avoids mocking members that the user has already manually implemented in the partial mock class (Test36).
- **Multiple Indexers:** Support for types with multiple overloaded indexers (Test34).

### 3. Documented but Not Fully Implemented
These features are described in the documentation but lack full implementation or are currently non-functional.

- **Static Class Mocking:** Described as a "Key Benefit" and has a large section in `docs/advanced-usage.md`. However, the generator does not yet implement the instance-wrapper pattern for static classes described there.
- **MockCallCounts:** This item is struck through in the `README.md` TODO list, implying completion, but the code for recording call counts is not yet implemented.

### 4. Known Limitations (Verified)
The following are correctly documented as unsupported in the `README.md`:

- `ref` return types.
- Attribute preservation on generated members.
- `__arglist` (Variable arguments).
- Advanced type constraints like `allows ref struct`.

### 5. TODO Status Accuracy
- **Completed (Struck-through):**
    - [x] Explicit interface implementations with name conflicts (Verified: Test11).
    - [x] Properties with complex getter/setter accessibility (Verified: Test33).
    - [ ] `MockCallCounts` ( **INCORRECTLY MARKED AS COMPLETED** ).
- **Remaining:**
    - [ ] `async` tests.
    - [ ] `event` getter/setter specific tests.
    - [ ] `readonly` struct/record tests.
    - [ ] `inheritdoc` support.
    - [ ] Attribute preservation.
