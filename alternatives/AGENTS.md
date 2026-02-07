# Agent Instructions for .NET Version

## Project summary
`discogs-xml2db` converts Discogs XML dumps into CSV for database import. The primary implementation is Python; there is an experimental .NET parser under `alternatives/dotnet`.

## Key paths
- `discogsxml2db/`: core Python exporter code
- `run.py`: CLI entrypoint (docopt)
- `tests/`: pytest suite and fixtures
- `mysql/`, `postgresql/`: import scripts
- `alternatives/dotnet/`: experimental C# parser/exporter
- `tmp/`: typical location for downloaded dump files (do not commit large dumps)

## Common commands
- TBD
- Tests can be run from the command line with `dotnet test`.

## .NET Coding guidelines and instructions

- Focus on readability and consistency with existing code.
- Be cautious with performance-sensitive paths; these dumps are large in real usage.
- Use `logger.BeginScope` when a method logs multiple lines or chained calls.
- Unit tests:
  - Use a Given/When/Then style for tests; label each section of the test with `// Given`, `// When`, and `// Then`.
  - Use `NSubstitute` for mocking dependencies and `AwesomeAssertions` (fork of `FluentAssertions`) for assertions.
  - Tests can be run from the command line with `dotnet test`.
- Follow `.editorconfig` style rules.
- Additional style guidance:
    - Avoid nested ternary operators.
    - Favor primary constructors and early returns.
    - Favor pattern matching over multiple boolean checks (e.g., `if(x is { Length: > 0 })` over `if(x != null && x.Length > 0)`)
    - Prefer new collection initializer style `List<T> items = []`.
    - Require target-typed `new` (e.g., `SomeType x = new(...)`).
    - Prefer explicit variable types for method return values (especially when used multiple times).
    - Avoid reimplementing framework helpers (e.g., use `Trim()`).
    - String comparisons must use `StringComparison.OrdinalIgnoreCase` (or similar).
