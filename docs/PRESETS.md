# Presets — RoSpriteEnhancer v1.21.0

## Overview

Presets allow saving and loading enhancement settings for quick reuse. Presets are stored as JSON files.

## Preset Structure

**File:** `%AppData%\RoSpriteEnhancer\Presets\<name>.preset.json`

```json
{
  "Name": "My Monster Preset",
  "Category": "Monster",
  "ScaleAlgorithm": "RealESRGAN4x",
  "ScalePasses": 1,
  "OutputFormat": "Png",
  "OutputQuality": 9,
  "IncludeBmp": true,
  "IncludePng": true,
  "IncludeTga": true,
  "IncludeSpr": true,
  "ScaleActAnchors": false,
  "Subfolders": true,
  "HandleMagentaKey": true,
  "AlphaThreshold": 128,
  "Enhancer": {
    "Outline": true,
    "OutlineStrength": 0.5,
    "Shadow": true,
    "ShadowStrength": 1.0,
    "Highlight": true,
    "HighlightStrength": 1.0,
    "Sharpen": false,
    "SharpenStrength": 0.3,
    "Volumetric3D": false,
    "Volumetric3DStrength": 0.5,
    "OutlineMode": "Full",
    "ExclusionColor": "#FF00FF",
    "HueShift": 0,
    "SaturationScale": 1.0,
    "LightnessScale": 1.0
  },
  "SuperSampler": {
    "Enabled": false,
    "Filter": "Lanczos3",
    "TargetSize": 0
  },
  "Esrgan": { ... },
  "Waifu2x": { ... },
  "RealCugan": { ... },
  "AiFirst": { ... },
  "RealESRGAN": { ... },
  "GFPGAN": { ... },
  "IncrementalMode": false,
  "AutoSelectEngine": true,
  "CategoryOverrides": { ... }
}
```

## Preset Categories

| Category | Description |
|----------|-------------|
| `Classic` | Classic RO sprites (Pre-Renewal) |
| `Renewal` | Renewal RO sprites |
| `Item` | Item sprites |
| `Monster` | Monster sprites |
| `NPC` | NPC sprites |
| `Custom` | User-defined category |

## Preset Manager

Located in `RoSpriteEnhancer.Core.Services.PresetManager`.

| Method | Description |
|--------|-------------|
| `Save(string name, EnhancementPreset preset, string category)` | Save preset to file |
| `Load(string name)` | Load preset from file |
| `Delete(string name)` | Delete preset file |
| `List()` | List all available presets |
| `Category` | Property on `EnhancementPreset` |

## Preset Categories Enum

```csharp
public enum PresetCategory
{
    Classic,
    Renewal,
    Item,
    Monster,
    NPC,
    Custom
}
```

## Using Presets

### Save

1. Configure all enhancement settings
2. Enter preset name
3. Select category
4. Click **Save Preset**

### Load

1. Select preset from ComboBox
2. All settings applied automatically
3. Preview updates immediately

### Export/Import

- **Export:** Select preset → click **📤** → choose file location
- **Import:** Click **📥** → select `.json` preset file

### Cloud Sharing

Presets can be shared via GitHub Gists:

- **Upload:** Click **☁↑** → preset uploaded to Gist
- **Download:** Click **☁↓** → enter Gist ID → preset downloaded

**Service:** `RoSpriteEnhancer.Core.Services.PresetSharingService`

---

## Preset File Location

```
%AppData%\RoSpriteEnhancer\Presets\
├── My Monster Preset.preset.json
├── My Item Preset.preset.json
└── ...
```

---

## See Also

- `API.md` — `PresetManager`, `PresetSharingService` class reference
- `FILE_FORMATS.md` — JSON format details
