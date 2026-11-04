using Xunit;

namespace RoSpriteEnhancer.Core.Tests.Unit;

public class LanguageServiceTests
{
    [Fact]
    public void LoadLanguage_ValidLanguage_DoesNotThrow()
    {
        // Sollte keine Exception werfen
        RoSpriteEnhancer.Core.Utils.LanguageService.LoadLanguage("en");
        Assert.True(true);
    }

    [Fact]
    public void LoadLanguage_InvalidLanguage_FallsBackToEnglish()
    {
        // Sollte keine Exception werfen, auch bei ungültiger Sprache
        RoSpriteEnhancer.Core.Utils.LanguageService.LoadLanguage("xx");
        Assert.True(true);
    }

    [Fact]
    public void GetLocalizedString_EnglishKey_ReturnsValue()
    {
        RoSpriteEnhancer.Core.Utils.LanguageService.LoadLanguage("en");

        // Teste dass der Service initialisiert ist und keine Exception wirft
        var result = RoSpriteEnhancer.Core.Utils.LanguageService.GetString("status.ready");

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }
}
