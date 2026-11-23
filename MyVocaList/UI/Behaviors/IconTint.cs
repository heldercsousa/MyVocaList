using CommunityToolkit.Maui.Behaviors;

namespace MyVocaList.UI.Behaviors;

/// <summary>
/// Attached property that tints an icon through <see cref="IconTintColorBehavior"/>. It exists so a <see cref="Style"/> setter can tint
/// an icon, because a behavior cannot be set from a style.
/// </summary>
public static class IconTint
{
    /// <summary>Icon tint color. Null removes the tint.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.CreateAttached(
        "Color", typeof(Color), typeof(IconTint), null, propertyChanged: OnColorChanged);

    /// <summary>Gets the icon tint color of <paramref name="view"/>.</summary>
    public static Color GetColor(BindableObject view) => (Color)view.GetValue(ColorProperty);

    /// <summary>Sets the icon tint color of <paramref name="view"/>.</summary>
    public static void SetColor(BindableObject view, Color value) => view.SetValue(ColorProperty, value);

    private static void OnColorChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not View view)
            return;

        var existing = view.Behaviors.OfType<IconTintColorBehavior>().FirstOrDefault();

        if (newValue is null)
        {
            if (existing is not null)
                view.Behaviors.Remove(existing);
            return;
        }

        if (existing is null)
        {
            existing = new IconTintColorBehavior();
            view.Behaviors.Add(existing);
        }

        existing.TintColor = (Color)newValue;
    }
}
