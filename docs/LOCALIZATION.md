# Localization — RoSpriteEnhancer v1.21.0

## Overview

RoSpriteEnhancer supports **48 languages** with English as the default. All UI text is stored in JSON files and loaded at runtime.

## Architecture

### LanguageService

Located in `RoSpriteEnhancer.Core.Utils.LanguageService`.

| Method | Description |
|--------|-------------|
| `LoadLanguage(string code)` | Loads a language file and sets it as active |
| `GetString(string key)` | Returns translated text for a key |
| `GetString(string key, params object[] args)` | Returns translated text with format arguments |
| `GetAvailableLanguages()` | Returns all available language codes |
| `CurrentLanguageCode` | Currently active language code |
| `CurrentLanguageName` | Currently active language name |
| `LanguageChanged` | Event fired when language changes |

### File Structure

```
Locales/
├── en.json          # English (default)
├── de.json          # German
├── fr.json          # French
├── es.json          # Spanish
├── ...              # 48 languages total
└── zh.json          # Chinese (simplified)
```

Each JSON file contains key-value pairs:

```json
{
  "app.title": "RO Sprite Enhancer v1.21 — Liquid Glass 2026",
  "btn.settings": "Settings",
  "btn.process": "Start Processing",
  "chk.magenta": "Magenta Colorkey Handling",
  "msg.processingComplete": "Processing complete",
  "tooltip.btn.ai_comparison": "Waifu2x vs RealESRGAN Side-by-Side"
}
```

### Key Format

Keys use dot notation:
- `btn.*` — Button labels
- `chk.*` — Checkbox labels
- `cmb.*` — ComboBox items
- `label.*` — Label text
- `menu.*` — Menu items
- `msg.*` — Messages/dialogs
- `section.*` — Section headers
- `slider.*` — Slider labels
- `status.*` — Status bar text
- `tab.*` — Tab names
- `tooltip.*` — Tooltips
- `window.*` — Window titles

### Fallback Behavior

If a key is missing in the current language:
1. Returns the key itself (e.g., `btn.settings`)
2. Falls back to English if available

### Adding a New Language

1. Copy `en.json` to `Locales/<langcode>.json`
2. Translate all values
3. Add to `FirstRunWizard.xaml.cs` language combo (optional)
4. Update `en.json` if new keys are added

### Supported Languages (48)

| Code | Language |
|------|----------|
| en | English |
| de | Deutsch |
| fr | Français |
| es | Español |
| it | Italiano |
| pt | Português |
| nl | Nederlands |
| sv | Svenska |
| da | Dansk |
| no | Norsk |
| fi | Suomi |
| pl | Polski |
| cs | Čeština |
| sk | Slovenčina |
| hu | Magyar |
| ro | Română |
| bg | Български |
| ru | Русский |
| uk | Українська |
| el | Ελληνικά |
| tr | Türkçe |
| ar | العربية |
| fa | فارسی |
| he | עברית |
| hi | हिन्दी |
| bn | বাংলা |
| gu | ગુજરાતી |
| pa | ਪੰਜਾਬੀ |
| ta | தமிழ் |
| te | తెలుగు |
| kn | ಕನ್ನಡ |
| ml | മലയാളം |
| mr | मराठी |
| or | ଓଡ଼ିଆ |
| th | ไทย |
| vi | Tiếng Việt |
| id | Bahasa Indonesia |
| ms | Bahasa Melayu |
| tl | Filipino |
| zh | 中文 (简体) |
| zh-cn | 中文 (繁體) |
| ja | 日本語 |
| ko | 한국어 |

### RTL Support

Right-to-left languages (Arabic, Hebrew, Persian) are supported via `RtlSupport` converter which sets `FlowDirection` on UI elements.

---

## Adding New Keys

1. Add key-value pair to `en.json`
2. Run translation script or manually translate to all 48 languages
3. Use `LanguageService.GetString("key")` in code
4. Use `{loc:key}` in XAML (legacy) or code-behind for new UI

---

## See Also

- `ARCHITECTURE.md` — System architecture
- `API.md` — API documentation (LanguageService class)
- `FILE_FORMATS.md` — JSON format details
