# Contributing to Pansy

Thank you for your interest in contributing to **🌼 Pansy**!

## Getting Started

```bash
git clone https://github.com/TheAnsarya/pansy.git
cd pansy
dotnet build src/Pansy.Core
dotnet test tests/Pansy.Core.Tests -c Release
```

## Coding Standards

### Formatting

- **Tabs for indentation** — never spaces
- **K&R brace style** — opening braces on the same line
- **Lowercase hex** — `0xff00`, not `0xFF00`; `:x4`, not `:X4`
- **UTF-8 with BOM** — all source files
- **CRLF** line endings

### C# Conventions

- .NET 10 / C# 14
- File-scoped namespaces
- Nullable reference types enabled
- Modern pattern matching

## Testing

All changes must pass the full test suite:

```bash
dotnet test tests/Pansy.Core.Tests -c Release --nologo -v m
```

- Add tests for any new functionality
- Add regression tests for bug fixes
- Never commit with failing tests

## Benchmarks

For performance-related changes, run benchmarks before and after:

```bash
dotnet run --project tests/Pansy.Core.Benchmarks -c Release -- --filter "*"
```

Include before/after results in your pull request.

## Commit Messages

Use conventional commit prefixes and reference issues:

```
feat: Add batch symbol import API - #52
fix: Fix header version parsing - #47
test: Add cross-ref query edge case tests - #50
docs: Update platform table in README - #49
perf: Replace LINQ with manual loops in hot path - #52
```

## Pull Request Process

1. Create a feature branch: `feature/description` or `fix/description`
2. Make your changes with tests
3. Ensure all tests pass and no new warnings
4. Submit a pull request referencing relevant issues

## License

By contributing, you agree that your contributions will be released under [The Unlicense](LICENSE) — public domain.
