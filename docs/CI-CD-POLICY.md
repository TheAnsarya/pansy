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

- `NUGET_USER` - nuget.org profile name for Trusted Publishing login
- `NUGET_API_KEY` - optional fallback key for manual/API-key-based publishing

### Trusted Publishing (Recommended)

Use trusted publishing to avoid long-lived API keys.

1. Sign in to nuget.org.
2. Open user menu -> Trusted Publishing.
3. Add a GitHub policy with:
	- Repository owner: `TheAnsarya`
	- Repository: `pansy`
	- Workflow file: `publish-pansy-core.yml`
	- Environment: leave blank unless workflow uses a GitHub environment
4. In GitHub repo secrets, set:
	- `NUGET_USER` = your nuget.org username/profile name (not email)
5. Run the publish workflow.

The workflow will request an OIDC token and exchange it for a short-lived NuGet API key.

### Recommended Publication Flow

1. Update `src/Pansy.Core/Pansy.Core.csproj` `<Version>` when needed.
2. Verify local quality gates:

```bash
dotnet build Pansy.sln -c Release
dotnet test tests/Pansy.Core.Tests -c Release
```

3. Publish package:
	- Preferred: Trusted Publishing with `NUGET_USER` configured
	- Fallback: API key with `NUGET_API_KEY` configured
	- Trigger via workflow dispatch or publish a release tag like `v1.0.1`

4. Verify package availability on NuGet.org.

### Manual Recovery (If Publish Fails)

- `403 invalid/expired/unauthorized API key`:
	- Rotate `NUGET_API_KEY` on nuget.org and update GitHub secret, or switch to Trusted Publishing.
- Trusted Publishing not available in UI:
	- Continue with `NUGET_API_KEY` fallback until rollout reaches your account.
- Package exists but not searchable yet:
	- Wait for indexing/validation on nuget.org (usually minutes).

### Optional Future Expansion

- Automated CI test/build pipelines for PR validation
- Benchmark automation
- Release-note generation

## Related Policies

- All code must build locally before committing
- All tests must pass locally before pushing
- Use descriptive commit messages
- Tag releases manually with semantic versioning
