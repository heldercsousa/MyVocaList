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
