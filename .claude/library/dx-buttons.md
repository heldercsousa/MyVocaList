# DevExpress MAUI Component Patterns — DXButton

> Section file split from `devexpress-patterns.md` on 2026-07-14 (token-scoped reads). Index + never-miss rules: `devexpress-patterns.md`.

## DXButton — confirmed in codebase

Five named styles in `MaterialStyles.xaml`:
- `FilledButton` — primary action, `BackgroundColor=Primary`, `CornerRadius=20`
- `FilledTonalButton` — secondary action, `BackgroundColor=SecondaryContainer`
- `OutlinedButton` — cancel/secondary, `BorderColor=Outline`
- `TextButton` — low-emphasis, transparent
- `FlyoutMenuButton` — navigation drawer items, `HorizontalContentAlignment=Start`

Icon-only button pattern (FAB, toolbar icons):
```xml
<dx:DXButton Icon="add_outlined"
             IconColor="{StaticResource OnPrimary}"
             BackgroundColor="{StaticResource Primary}"
             PressedBackgroundColor="{StaticResource PrimaryContainer}"
             WidthRequest="56" HeightRequest="56"
             CornerRadius="16"
             HorizontalOptions="End" VerticalOptions="End"
             Margin="0,0,16,16"
             Command="{Binding AddCommand}" />
```

Icon-only display button (no tap, just icon):
```xml
<dx:DXButton Icon="nightlife_outlined"
             IconColor="{dx:ThemeColor OnSurfaceVariant}"
             IconWidth="64" IconHeight="64"
             BackgroundColor="Transparent"
             InputTransparent="True"
             WidthRequest="80" HeightRequest="80" />
```
