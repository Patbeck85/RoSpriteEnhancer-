# Contributing — RoSpriteEnhancer v1.21.0

## Table of Contents

1. [Getting Started](#getting-started)
2. [Development Workflow](#development-workflow)
3. [Code Style](#code-style)
4. [Pull Request Process](#pull-request-process)
5. [Testing](#testing)
6. [Documentation](#documentation)
7. [License](#license)

---

## Getting Started

### Prerequisites

- .NET 8 SDK
- Git
- Visual Studio 2022 or VS Code

### Clone

```bash
git clone https://github.com/Patbeck85/RoSpriteEnhancer-.git
cd RoSpriteEnhancer
```

### Build

```bash
dotnet build RoSpriteEnhancer.sln --configuration Release
```

### Run Tests

```bash
dotnet test --configuration Release
```

---

## Development Workflow

### 1. Create Feature Branch

```bash
git checkout -b feature/your-feature-name
```

### 2. Make Changes

- Follow the [Code Style](#code-style)
- Add unit tests for new functionality
- Update documentation if needed

### 3. Verify

```bash
dotnet build --configuration Release
dotnet test --configuration Release
```

### 4. Commit

Use conventional commit messages:

```
feat: add new upscaling algorithm
fix: resolve memory leak in GFPGANRunner
docs: update API documentation
test: add PathHelper unit tests
refactor: simplify BatchProcessor logic
```

### 5. Push & Pull Request

```bash
git push origin feature/your-feature-name
```

Open a PR on GitHub and request review.

---

## Code Style

### Naming Conventions

| Element | Convention | Example |
|---------|------------|---------|
| Classes | PascalCase | `BatchProcessor` |
| Methods | PascalCase | `ProcessBitmapPreview()` |
| Properties | PascalCase | `OutputFormat` |
| Fields | camelCase | `_pauseEvent` |
| Local variables | camelCase | `filePath` |
| Constants | PascalCase | `MaxConcurrentJobs` |
| Interfaces | PascalCase + I | `IUpscalerPlugin` |
| Enums | PascalCase | `OutputFormat` |
| Namespaces | PascalCase | `RoSpriteEnhancer.Core.Processing` |

### File Headers

All new classes should include XML documentation:

```csharp
/// <summary>
/// Brief description of the class.
/// </summary>
/// <typeparam name="T">Type parameter description.</typeparam>
public class MyClass<T>
{
    /// <summary>
    /// Brief description of the method.
    /// </summary>
    /// <param name="param">Parameter description.</param>
    /// <returns>Return value description.</returns>
    public string MyMethod(string param) { ... }
}
```

### Error Handling

- **No empty `catch {}` blocks** — always log with `Debug.WriteLine`
- **No silent errors** — every error requires logging
- **Use `using`/`try-finally`** for all `IDisposable` resources
- **Validate inputs** before processing

### Thread Safety

- UI updates via `Dispatcher.InvokeAsync` only
- Background processing on separate threads
- Shared state protected with `lock` or `SemaphoreSlim`

### Resource Management

- Every `Bitmap`, `Graphics`, `BitmapData` disposed via `using`/`finally`
- `LockBits`/`UnlockBits` pairs mandatory
- File handles closed via `using`

---

## Pull Request Process

1. Fork the repository
2. Create feature branch from `main`
3. Make changes following [Code Style](#code-style)
4. Add/update unit tests
5. Update documentation if needed
6. Ensure all tests pass
7. Submit PR with description of changes

### PR Checklist

- [ ] Build passes (0 warnings, 0 errors)
- [ ] All tests pass
- [ ] New code has unit tests
- [ ] Documentation updated
- [ ] No `catch {}` blocks
- [ ] All resources properly disposed

---

## Testing

### Running Tests

```bash
dotnet test --configuration Release
```

### Adding Tests

- Test files go in `src/RoSpriteEnhancer.Tests/Unit/`
- Naming: `ClassNameTests.cs`
- Use `xUnit` framework
- Test both valid and invalid inputs
- Mock external dependencies (AI tools, file system)

### Coverage

Current coverage: **~53%** (measured by Coverlet)
Target: **80%+**

---

## Documentation

### Where to Document

| Document | Location | Purpose |
|----------|----------|---------|
| API Reference | `docs/API.md` | Public classes and methods |
| Architecture | `ARCHITECTURE.md` | System design |
| Build Guide | `docs/BUILD.md` | How to build |
| Deployment | `docs/DEPLOYMENT.md` | Release process |
| File Formats | `docs/FILE_FORMATS.md` | SPR, ACT, GRF, etc. |
| Localization | `docs/LOCALIZATION.md` | Language system |
| Plugins | `docs/PLUGINS.md` | Plugin development |
| Presets | `docs/PRESETS.md` | Preset system |
| RO Data | `docs/RO_DATA_FORMATS.md` | RO-specific formats |
| Decisions | `docs/DECISIONS.md` | Architecture decisions (ADR) |

### Documentation Standards

- Write in English
- Use Markdown
- Include code examples
- Keep up-to-date with code changes

---

## License

Part of the Yggdrasil.Studio ecosystem — Open Source for the RO community.
