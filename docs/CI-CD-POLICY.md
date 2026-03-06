# 🌼 Pansy - CI/CD and Build Policy

## No Automated CI/CD on GitHub

**Policy:** We do **NOT** enable automated CI/CD workflows on GitHub due to cost constraints.

### Rationale

- GitHub Actions minutes are expensive at scale
- Multiple repositories would compound costs
- Manual builds are sufficient for our development pace
- Local development and testing is preferred

### What This Means

- **No `.github/workflows/` automation** - Do not create GitHub Actions files
- **Manual builds only** - Build locally with `dotnet build`
- **Manual testing** - Run `dotnet test` locally before committing
- **Manual releases** - Package and publish manually when needed

### Local Development Workflow

#### Building

```bash
# Build entire solution
dotnet build Pansy.sln

# Build specific project
dotnet build src/Pansy.Core/Pansy.Core.csproj

# Clean build
dotnet clean && dotnet build
```

#### Testing

```bash
# Run all tests
dotnet test Pansy.sln

# Run with verbosity
dotnet test Pansy.sln -v detailed

# Run specific test project
dotnet test tests/Pansy.Core.Tests/Pansy.Core.Tests.csproj
```

#### Publishing

```bash
# Publish CLI tool
dotnet publish src/Pansy.Cli/Pansy.Cli.csproj -c Release -o publish/

# Publish UI
dotnet publish src/Pansy.UI/Pansy.UI.csproj -c Release -o publish-ui/
```

### When We Can Afford CI/CD
In the future, when budget allows, we can add:

- Automated builds on push
- Automated testing
- Release automation
- Package publishing to NuGet

Until then, **local is king**. 👑

## Related Policies

- All code must build locally before committing
- All tests must pass locally before pushing
- Use descriptive commit messages
- Tag releases manually with semantic versioning
