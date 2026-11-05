# Plugin System — RoSpriteEnhancer v1.21.0

## Overview

RoSpriteEnhancer supports external upscaler plugins via the `IUpscalerPlugin` interface. This allows developers to add custom AI upscaling algorithms without modifying the core application.

## Plugin Interface

### IUpscalerPlugin

Located in `RoSpriteEnhancer.Core.Processing.UpscalerPluginManager`.

```csharp
public interface IUpscalerPlugin
{
    string PluginId { get; }           // Unique identifier
    string DisplayName { get; }        // Human-readable name
    string Description { get; }        // Plugin description
    string Version { get; }            // Plugin version
    string Author { get; }             # Author name
    
    bool IsAvailable();                # Check if plugin dependencies are met
    Bitmap? Upscale(Bitmap src, UpscalerPluginOptions opts);  # Main upscale method
}
```

### UpscalerPluginOptions

```csharp
public class UpscalerPluginOptions
{
    public int ScaleFactor { get; set; } = 2;
    public Dictionary<string, string> CustomParameters { get; set; } = new();
}
```

## Creating a Plugin

### 1. Create Class Library

```bash
dotnet new classlib -n MyUpscalerPlugin
```

### 2. Add Reference

Reference `RoSpriteEnhancer.Core.dll` in your plugin project.

### 3. Implement IUpscalerPlugin

```csharp
using RoSpriteEnhancer.Core.Processing;
using System.Drawing;

namespace MyUpscalerPlugin
{
    public class MyUpscaler : IUpscalerPlugin
    {
        public string PluginId => "my.upscaler.v1";
        public string DisplayName => "My Custom Upscaler";
        public string Description => "A custom AI upscaling algorithm";
        public string Version => "1.0.0";
        public string Author => "Your Name";
        
        public bool IsAvailable()
        {
            // Check if dependencies (models, executables) exist
            return true;
        }
        
        public Bitmap? Upscale(Bitmap src, UpscalerPluginOptions opts)
        {
            // Your upscaling logic here
            var result = new Bitmap(src.Width * opts.ScaleFactor, src.Height * opts.ScaleFactor);
            // ... process ...
            return result;
        }
    }
}
```

### 4. Build and Deploy

1. Build the plugin DLL
2. Place `MyUpscalerPlugin.dll` in the `plugins/` folder next to `RoSpriteEnhancer.exe`
3. Restart RoSpriteEnhancer
4. The plugin appears in the Plugin Marketplace window

## Plugin Discovery

Plugins are automatically discovered on startup:

1. `UpscalerPluginManager.Initialize(pluginsDir)` scans the `plugins/` folder
2. Each DLL is loaded and checked for `IUpscalerPlugin` implementations
3. Available plugins are listed in the Plugin Marketplace UI
4. Users can enable/disable plugins

## Plugin Marketplace

The built-in Plugin Marketplace window (`PluginMarketplaceWindow`) allows:
- Viewing installed plugins
- Enabling/disabling plugins
- Viewing plugin details (name, version, author, description)
- Checking plugin availability

## Best Practices

| Practice | Reason |
|----------|--------|
| Unique `PluginId` | Avoid conflicts with other plugins |
| Check `IsAvailable()` | Graceful degradation if dependencies missing |
| Handle exceptions | Don't crash the host application |
| Dispose `Bitmap` objects | Prevent memory leaks |
| Document `CustomParameters` | Help users configure your plugin |

---

## Example: Complete Plugin

```csharp
using System.Drawing;
using RoSpriteEnhancer.Core.Processing;

namespace ExamplePlugin
{
    public class ExampleUpscaler : IUpscalerPlugin
    {
        public string PluginId => "example.upscaler.v1";
        public string DisplayName => "Example Upscaler";
        public string Description => "Example plugin for RoSpriteEnhancer";
        public string Version => "1.0.0";
        public string Author => "Developer";
        
        public bool IsAvailable() => true;
        
        public Bitmap? Upscale(Bitmap src, UpscalerPluginOptions opts)
        {
            try
            {
                var result = new Bitmap(src.Width * opts.ScaleFactor, src.Height * opts.ScaleFactor);
                using (var g = Graphics.FromImage(result))
                {
                    g.DrawImage(src, 0, 0, result.Width, result.Height);
                }
                return result;
            }
            catch
            {
                return null;
            }
        }
    }
}
```

---

## See Also

- `API.md` — `UpscalerPluginManager` class reference
- `ARCHITECTURE.md` — Plugin system design
