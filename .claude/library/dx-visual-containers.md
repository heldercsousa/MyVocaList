# DevExpress MAUI Component Patterns — ShimmerView, DXBorder, DXScrollView, CheckEdit

> Section file split from `devexpress-patterns.md` on 2026-07-14 (token-scoped reads). Index + never-miss rules: `devexpress-patterns.md`.

## ShimmerView (skeleton loading) — confirmed in codebase

```xml
<dx:ShimmerView IsLoading="{Binding IsInitialLoading}"
                WaveWidth="0.7"
                WaveOpacity="0.8">
    <dx:ShimmerView.LoadingView>
        <VerticalStackLayout Spacing="0">
            <dx:DXBorder BackgroundColor="{dx:ThemeColor SurfaceContainerHighest}"
                         CornerRadius="12" HeightRequest="52" Margin="16,4" />
        </VerticalStackLayout>
    </dx:ShimmerView.LoadingView>
    <dx:ShimmerView.Content>
        <!-- actual content shown when not loading -->
    </dx:ShimmerView.Content>
</dx:ShimmerView>
```

Note: Add `await Task.Yield()` before starting data load in `InitializeAsync()` to allow skeleton to render.

## DXBorder — confirmed in codebase

Used for cards, chips, badges, search bar wrappers:
```xml
<dx:DXBorder BackgroundColor="{StaticResource SurfaceContainerLow}"
             CornerRadius="12"
             Padding="16"
             Margin="16,4"
             HeightRequest="64">
    <!-- content -->
</dx:DXBorder>
```

Selected item variant (with border highlight):
```xml
<dx:DXBorder BackgroundColor="{StaticResource SurfaceContainerLow}"
             BorderColor="{dx:ThemeColor Primary}"
             BorderThickness="2"
             CornerRadius="12" ... />
```

## DXScrollView — confirmed in codebase

Drop-in replacement for MAUI ScrollView:
```xml
<dx:DXScrollView BackgroundColor="{StaticResource Surface}">
    <!-- content -->
</dx:DXScrollView>
```

## CheckEdit — confirmed in codebase

```xml
<dx:CheckEdit IsChecked="{Binding IsSelected, Mode=OneWay}"
              CheckedCheckBoxColor="{dx:ThemeColor Primary}"
              InputTransparent="True"
              VerticalOptions="Center" />
```

Note: `CheckedCheckBoxColor` requires `{dx:ThemeColor X}` — `{StaticResource X}` does not work here.
