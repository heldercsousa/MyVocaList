using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using MyVocaList.Services;

namespace MyVocaList.Extensions;

[ContentProperty(nameof(Key))]
public class TranslateExtension : IMarkupExtension
{
    public string Key { get; set; }

    public object ProvideValue(IServiceProvider serviceProvider)
    {
        return LocalizationResourceManager.Instance.GetString(Key);
    }
}
