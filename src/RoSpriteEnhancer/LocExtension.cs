using System;
using System.Windows.Markup;
using RoSpriteEnhancer.Core.Utils;

namespace RoSpriteEnhancer
{
    /// <summary>
    /// XAML MarkupExtension für Lokalisierung: {loc:menu.file}
    /// </summary>
    [MarkupExtensionReturnType(typeof(string))]
    public class LocExtension : MarkupExtension
    {
        public string? Key { get; set; }

        public LocExtension() { }

        public LocExtension(string? key)
        {
            Key = key;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (string.IsNullOrEmpty(Key))
                return "";

            try
            {
                return LanguageService.GetString(Key);
            }
            catch
            {
                return Key ?? "";
            }
        }
    }
}
