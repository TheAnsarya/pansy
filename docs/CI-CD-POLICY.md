# 🌼 Pansy - CI/CD and Build Policy

## Hybrid Policy

Pansy uses a hybrid policy:

- Keep most development workflows local-first (build/test/benchmark)
- Allow focused GitHub automation for package publication only

### Automation Scope

- `publish-pansy-core.yml` is approved for NuGet publication
- Other workflows remain optional and local-first
- Local validation remains mandatory before release publication

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
# Pack Pansy.Core
dotnet pack src/Pansy.Core/Pansy.Core.csproj -c Release -o artifacts/nupkg

# Publish to NuGet.org (requires API key)
dotnet nuget push artifacts/nupkg/*.nupkg --source https://api.nuget.org/v3/index.json --api-key <NUGET_API_KEY>

# Publish CLI tool
dotnet publish src/Pansy.Cli/Pansy.Cli.csproj -c Release -o publish/

# Publish UI
dotnet publish src/Pansy.UI/Pansy.UI.csproj -c Release -o publish-ui/
```

## NuGet Publication

### Required Secret

- `NUGET_API_KEY` - NuGet.org API key with permission to publish `Pansy.Core`

### Recommended Publication Flow

1. Update `src/Pansy.Core/Pansy.Core.csproj` `<Version>` when needed.
2. Verify local quality gates:

```bash
dotnet build Pansy.sln -c Release
dotnet test tests/Pansy.Core.Tests -c Release
```

3. Publish package:
	- Run `Publish Pansy.Core NuGet Package` via workflow dispatch, or
	- Publish a GitHub release tag like `v1.0.1` to trigger release-based publish.

4. Verify package availability on NuGet.org.

### Optional Future Expansion

- Automated CI test/build pipelines for PR validation
- Benchmark automation
- Release-note generation

## Related Policies

- All code must build locally before committing
- All tests must pass locally before pushing
- Use descriptive commit messages
- Tag releases manually with semantic versioning
