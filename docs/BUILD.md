# Build Documentation — RoSpriteEnhancer v1.21.0

## Prerequisites

| Component | Version | Download |
|-----------|---------|----------|
| .NET 8 SDK | 8.0.x | https://dotnet.microsoft.com/download/dotnet/8.0 |
| Windows | 7/8/10/11 | — |
| Git | 2.x+ | https://git-scm.com/ |

> **.NET 8 Runtime is NOT required** — the published EXE is self-contained.

---

## Project Structure

```
RoSpriteEnhancer/
├── RoSpriteEnhancer.sln              # Solution file
├── RoSpriteEnhancer/                 # WPF UI project (net8.0-windows)
│   ├── RoSpriteEnhancer.csproj       # Project file
│   ├── App.xaml/.cs                  # Application resources
│   ├── Views/Windows/                # UI windows
│   └── Assets/Icon.ico               # Application icon
├── RoSpriteEnhancer.Core/            # Core library (net8.0)
│   ├── RoSpriteEnhancer.Core.csproj  # Project file
│   └── Processing/Upscaling/...      # Core domain logic
├── RoSpriteEnhancer.Tests/           # xUnit test project
│   └── Unit/*.cs                     # 193 unit tests
├── scripts/                          # Build & utility scripts
│   ├── build.bat                     # Build script
│   ├── download_ai_tools.ps1         # AI tool downloader
│   ├── download_ai_tools.bat         # Batch wrapper for PS1
│   └── update_hd_pipeline.ps1        # HD pipeline updater
├── Locales/*.json                    # 48 language files
├── .github/workflows/ci.yml          # GitHub Actions CI/CD
└── .gitignore                        # Git ignore rules
```

---

## Build Methods

### Method 1: build.bat (Recommended)

```batch
# 1. Extract/cloned repository
cd RoSpriteEnhancer

# 2. Double-click build.bat

# 3. Wait until "BUILD SUCCESSFUL" appears

# 4. Output is in: release_final/app/
```

`build.bat` performs:
- .NET SDK version check
- Clean previous build
- Compile as `win-x64` Release
- Create output in `release_final/app/`

### Method 2: dotnet CLI (Manual)

```batch
cd RoSpriteEnhancer
dotnet restore RoSpriteEnhancer.sln
dotnet build RoSpriteEnhancer.sln --configuration Release --no-restore
dotnet publish src/RoSpriteEnhancer/RoSpriteEnhancer.csproj --configuration Release --self-contained false -r win-x64 -o release_final/app/
```

### Method 3: Visual Studio 2022

1. Open `RoSpriteEnhancer.sln`
2. Select **Release** configuration
3. Select **x64** platform
4. Build → Build Solution
5. Publish: Right-click project → Publish → Folder

---

## Project References

| Project | Target Framework | References |
|---------|-----------------|------------|
| `RoSpriteEnhancer.Core` | net8.0 | `System.Drawing.Common`, `Microsoft.Extensions.DependencyInjection` |
| `RoSpriteEnhancer` (UI) | net8.0-windows | `RoSpriteEnhancer.Core`, `System.Drawing.Common` |
| `RoSpriteEnhancer.Tests` | net8.0-windows | `RoSpriteEnhancer.Core`, `xunit`, `coverlet.collector` |

---

## Build Configuration

| Setting | Value |
|---------|-------|
| Target Framework (Core) | net8.0 |
| Target Framework (UI) | net8.0-windows |
| Nullable | enabled |
| TreatWarningsAsErrors | true |
| AllowUnsafeBlocks | true |
| EnableDefaultCompileItems | false (all .cs files must be explicitly listed) |
| RuntimeIdentifier | win-x64 |
| Publish | SingleFile, self-contained |

---

## CI/CD (GitHub Actions)

| Workflow | File | Trigger |
|----------|------|---------|
| CI | `.github/workflows/ci.yml` | push, pull_request |

The CI pipeline:
1. Checks out repository
2. Sets up .NET 8
3. Restores dependencies
4. Builds solution (0 warnings, 0 errors)
5. Runs tests with code coverage
6. Checks coverage gate (30% minimum)
7. Uploads test results and coverage report

---

## AI Tools

The application supports external AI upscaling tools. These are **not** included in the repository and must be downloaded separately:

| Tool | Executable | Download |
|------|------------|----------|
| Waifu2x | `waifu2x-ncnn-vulkan.exe` | `scripts/download_ai_tools.ps1` |
| RealCUGAN | `realcugan-ncnn-vulkan.exe` | `scripts/download_ai_tools.ps1` |
| RealESRGAN | `realesrgan-ncnn-vulkan.exe` | `scripts/download_ai_tools.ps1` |
| GFPGAN | `gfpgan-ncnn-vulkan.exe` | `scripts/download_ai_tools.ps1` |

Run `scripts/download_ai_tools.ps1` to download all AI tools automatically.

---

## Testing

```bash
cd RoSpriteEnhancer
dotnet test src/RoSpriteEnhancer.Tests/RoSpriteEnhancer.Tests.csproj --configuration Release
```

Current test count: **193 tests** covering all core components.

---

## Code Quality

- 0 build warnings
- 0 empty `catch {}` blocks
- All `IDisposable` resources use `using`/`try-finally`
- Nullable reference types enabled
- SOLID, KISS, DRY, YAGNI principles applied

---

## See Also

- `ARCHITECTURE.md` — System architecture
- `API.md` — API documentation
- `DEPLOYMENT.md` — Release/deployment guide
