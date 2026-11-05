using Xunit;

namespace RoSpriteEnhancer.Tests.Unit;

public class LoggerTests
{
    [Fact]
    public void IsEnabled_DefaultIsTrue()
    {
        Assert.True(RoSpriteEnhancer.Core.Utils.Logger.IsEnabled);
    }

    [Fact]
    public void IsEnabled_CanBeSetToFalse()
    {
        RoSpriteEnhancer.Core.Utils.Logger.IsEnabled = false;
        try
        {
            // Sollte keine Ausnahme werfen, auch wenn Logging deaktiviert ist
            RoSpriteEnhancer.Core.Utils.Logger.Info("Test message");
            RoSpriteEnhancer.Core.Utils.Logger.Warn("Test warning");
            RoSpriteEnhancer.Core.Utils.Logger.Error("Test error");
            RoSpriteEnhancer.Core.Utils.Logger.Fatal("Test fatal");
        }
        finally
        {
            RoSpriteEnhancer.Core.Utils.Logger.IsEnabled = true;
        }
    }

    [Fact]
    public void Info_ValidMessage_DoesNotThrow()
    {
        RoSpriteEnhancer.Core.Utils.Logger.Info("Test info message");
        RoSpriteEnhancer.Core.Utils.Logger.Info("Test info message with category", "TestCategory");
    }

    [Fact]
    public void Warn_ValidMessage_DoesNotThrow()
    {
        RoSpriteEnhancer.Core.Utils.Logger.Warn("Test warning message");
        RoSpriteEnhancer.Core.Utils.Logger.Warn("Test warning message with category", "TestCategory");
    }

    [Fact]
    public void Error_ValidMessage_DoesNotThrow()
    {
        RoSpriteEnhancer.Core.Utils.Logger.Error("Test error message");
        RoSpriteEnhancer.Core.Utils.Logger.Error("Test error message with exception", new InvalidOperationException("Test exception"));
        RoSpriteEnhancer.Core.Utils.Logger.Error("Test error message with category", null, "TestCategory");
    }

    [Fact]
    public void Fatal_ValidMessage_DoesNotThrow()
    {
        RoSpriteEnhancer.Core.Utils.Logger.Fatal("Test fatal message");
        RoSpriteEnhancer.Core.Utils.Logger.Fatal("Test fatal message with exception", new InvalidOperationException("Test exception"));
        RoSpriteEnhancer.Core.Utils.Logger.Fatal("Test fatal message with category", null, "TestCategory");
    }

    [Fact]
    public void Debug_ValidMessage_DoesNotThrow()
    {
        // Debug wird nur im DEBUG-Build geschrieben, ruft Write nur auf wenn DEBUG definiert
        RoSpriteEnhancer.Core.Utils.Logger.Debug("Test debug message");
        RoSpriteEnhancer.Core.Utils.Logger.Debug("Test debug message with category", "TestCategory");
    }

    [Fact]
    public void CleanupOldLogs_DefaultMaxDays_DoesNotThrow()
    {
        RoSpriteEnhancer.Core.Utils.Logger.CleanupOldLogs(30);
    }

    [Fact]
    public void CleanupOldLogs_ZeroDays_DoesNotThrow()
    {
        RoSpriteEnhancer.Core.Utils.Logger.CleanupOldLogs(0);
    }

    [Fact]
    public void LogLevel_EnumHasExpectedValues()
    {
        Assert.Equal(0, (int)RoSpriteEnhancer.Core.Utils.LogLevel.Debug);
        Assert.Equal(1, (int)RoSpriteEnhancer.Core.Utils.LogLevel.Info);
        Assert.Equal(2, (int)RoSpriteEnhancer.Core.Utils.LogLevel.Warn);
        Assert.Equal(3, (int)RoSpriteEnhancer.Core.Utils.LogLevel.Error);
        Assert.Equal(4, (int)RoSpriteEnhancer.Core.Utils.LogLevel.Fatal);
    }
}
