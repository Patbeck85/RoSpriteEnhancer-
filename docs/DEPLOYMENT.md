# Deployment Guide — RoSpriteEnhancer v1.21.0

## Overview

RoSpriteEnhancer is distributed as a **self-contained ZIP package** via GitHub Releases. No installer, no code signing.

## Release Process

### 1. Prepare Release

```bash
# Ensure on main branch
git checkout main
git pull origin main

# Verify build
dotnet build RoSpriteEnhancer.sln --configuration Release

# Verify tests
dotnet test --configuration Release
```

### 2. Create Release Package

```bash
# Publish self-contained
dotnet publish src/RoSpriteEnhancer/RoSpriteEnhancer.csproj --configuration Release --self-contained false -r win-x64 -o release_final/app/

# Create ZIP
# (Windows: right-click → Send to → Compressed folder)
# Or use PowerShell:
Compress-Archive -Path release_final/app/* -DestinationPath release_final/ReleasePackage.zip
```

### 3. Create GitHub Release

1. Go to https://github.com/Patbeck85/RoSpriteEnhancer-/releases
2. Click "Draft a new release"
3. Tag: `v1.21.0` (or next version)
4. Title: `RoSpriteEnhancer v1.21.0 — Liquid Glass 2026 Release`
5. Describe changes (see CHANGELOG.md)
6. Upload `release_final/ReleasePackage.zip` as asset
7. Publish

### 4. Verify Release

- Download the ZIP from GitHub
- Extract to test folder
- Run `RoSpriteEnhancer.exe`
- Verify main features work

---

## Release Package Contents

| File | Description |
|------|-------------|
| `RoSpriteEnhancer.exe` | Standalone application (~152 KB) |
| `RoSpriteEnhancer.dll` | Main assembly |
| `RoSpriteEnhancer.Core.dll` | Core library |
| `RoSpriteEnhancer.deps.json` | Dependencies |
| `RoSpriteEnhancer.runtimeconfig.json` | Runtime config |
| `Locales/*.json` | 48 language files |
| `Microsoft.Extensions.DependencyInjection.dll` | DI container |
| `WPF DLLs` | Presentation framework |
| `download_ai_tools.ps1` | AI tool download script |
| `download_ai_tools.bat` | Batch wrapper |

---

## AI Tools (Not Included)

AI upscaling tools are **not bundled** due to size. Users must download separately:

| Tool | Size | Download |
|------|------|----------|
| Waifu2x | ~100 MB | `scripts/download_ai_tools.ps1` |
| RealCUGAN | ~100 MB | `scripts/download_ai_tools.ps1` |
| RealESRGAN | ~100 MB | `scripts/download_ai_tools.ps1` |
| GFPGAN | ~100 MB | `scripts/download_ai_tools.ps1` |

---

## Versioning

SemVer `MAJOR.MINOR.PATCH`:
- **MAJOR**: Breaking changes
- **MINOR**: New features
- **PATCH**: Bug fixes

Current: `1.21.0`

---

## CI/CD

GitHub Actions workflow (`.github/workflows/ci.yml`):
- Builds on every push to `main`
- Runs tests with code coverage
- Uploads artifacts
- Coverage gate: 30% minimum

---

## Rollback

If a release has issues:
1. Create GitHub Release "v1.20.0" with previous ZIP
2. Pin as latest if needed
3. Create issue for fix

---

## See Also

- `BUILD.md` — Build instructions
- `CHANGELOG.md` — Version history
- `API.md` — API documentation
