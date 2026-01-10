# Material Symbols Test Implementation Guide

## Quick Test Setup

This guide shows you how to add Material Symbols to your project and create a comparison test page.

---

## Step 1: Install Material Symbols Package

Run this command in your project directory:

```bash
cd MyVocaList
dotnet add package UraniumUI.Icons.MaterialSymbols
```

---

## Step 2: Update MauiProgram.cs

Open `MauiProgram.cs` and add the Material Symbols font registration:

**Find this section (around line 20-26):**
```csharp
.ConfigureFonts(fonts =>
{
    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
    fonts.AddFontAwesomeIconFonts();
});
```

**Change to:**
```csharp
.ConfigureFonts(fonts =>
{
    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
    fonts.AddFontAwesomeIconFonts();
    fonts.AddMaterialSymbolsFonts(); // ← Add this line
});
```

---

## Step 3: Create Test Page

### Option A: Add to Existing DesignSystemPage

Add a new section to `UI/Pages/DesignSystem/DesignSystemPage.xaml`:

```xml
<!-- Add after xmlns:x declaration -->
xmlns:m="clr-namespace:UraniumUI.Icons.MaterialSymbols;assembly=UraniumUI.Icons.MaterialSymbols"

<!-- Add this section to your ScrollView content -->
<VerticalStackLayout Padding="20" Spacing="16">
    <Label Text="Icon Comparison Test"
           Style="{StaticResource HeadlineSmall}"/>

    <!-- Material Symbols Examples -->
    <Label Text="Material Symbols Icons:"
           Style="{StaticResource TitleMedium}"/>

    <HorizontalStackLayout Spacing="20">
        <Button Text="Home (Outlined)"
                material:ButtonIcon.Icon="{x:Static m:MaterialOutlined.Home}"/>

        <Button Text="Home (Filled)"
                material:ButtonIcon.Icon="{x:Static m:MaterialFilled.Home}"/>

        <Button Text="Home (Rounded)"
                material:ButtonIcon.Icon="{x:Static m:MaterialRound.Home}"/>
    </HorizontalStackLayout>

    <HorizontalStackLayout Spacing="20">
        <Button Text="Tune (Outlined)"
                material:ButtonIcon.Icon="{x:Static m:MaterialOutlined.Tune}"/>

        <Button Text="Tune (Filled)"
                material:ButtonIcon.Icon="{x:Static m:MaterialFilled.Tune}"/>

        <Button Text="Tune (Rounded)"
                material:ButtonIcon.Icon="{x:Static m:MaterialRound.Tune}"/>
    </HorizontalStackLayout>

    <!-- SVG Reference -->
    <Label Text="Current SVG Icons (for comparison):"
           Style="{StaticResource TitleMedium}"
           Margin="0,20,0,0"/>

    <HorizontalStackLayout Spacing="20">
        <Image Source="home"
               WidthRequest="24"
               HeightRequest="24"/>
        <Label Text="home.svg (outline)"
               VerticalOptions="Center"/>
    </HorizontalStackLayout>

    <HorizontalStackLayout Spacing="20">
        <Image Source="home_fill"
               WidthRequest="24"
               HeightRequest="24"/>
        <Label Text="home_fill.svg (filled)"
               VerticalOptions="Center"/>
    </HorizontalStackLayout>

    <HorizontalStackLayout Spacing="20">
        <Image Source="tune"
               WidthRequest="24"
               HeightRequest="24"/>
        <Label Text="tune.svg (outline)"
               VerticalOptions="Center"/>
    </HorizontalStackLayout>
</VerticalStackLayout>
```

### Option B: Create Dedicated IconTestPage

Create a new page: `UI/Pages/DesignSystem/IconTestPage.xaml`

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:material="http://schemas.enisn-projects.io/dotnet/maui/uraniumui/material"
             xmlns:m="clr-namespace:UraniumUI.Icons.MaterialSymbols;assembly=UraniumUI.Icons.MaterialSymbols"
             x:Class="MyVocaList.UI.Pages.DesignSystem.IconTestPage"
             Title="Icon Test">

    <ScrollView>
        <VerticalStackLayout Padding="20" Spacing="24">

            <!-- Header -->
            <Label Text="Icon Implementation Comparison"
                   Style="{StaticResource HeadlineMedium}"/>

            <BoxView HeightRequest="1"
                     Color="{StaticResource OutlineVariant}"/>

            <!-- Material Symbols Section -->
            <Label Text="Material Symbols (UraniumUI)"
                   Style="{StaticResource TitleLarge}"/>

            <Label Text="Outlined Variant:"
                   Style="{StaticResource TitleSmall}"/>
            <Grid ColumnDefinitions="*,*"
                  RowDefinitions="Auto,Auto,Auto"
                  ColumnSpacing="10"
                  RowSpacing="10">
                <Button Grid.Column="0" Grid.Row="0"
                        Text="Home"
                        material:ButtonIcon.Icon="{x:Static m:MaterialOutlined.Home}"/>
                <Button Grid.Column="1" Grid.Row="0"
                        Text="Tune"
                        material:ButtonIcon.Icon="{x:Static m:MaterialOutlined.Tune}"/>
                <Button Grid.Column="0" Grid.Row="1"
                        Text="Favorite"
                        material:ButtonIcon.Icon="{x:Static m:MaterialOutlined.Favorite}"/>
                <Button Grid.Column="1" Grid.Row="1"
                        Text="Settings"
                        material:ButtonIcon.Icon="{x:Static m:MaterialOutlined.Settings}"/>
                <Button Grid.Column="0" Grid.Row="2"
                        Text="Add"
                        material:ButtonIcon.Icon="{x:Static m:MaterialOutlined.Add}"/>
                <Button Grid.Column="1" Grid.Row="2"
                        Text="Search"
                        material:ButtonIcon.Icon="{x:Static m:MaterialOutlined.Search}"/>
            </Grid>

            <Label Text="Filled Variant:"
                   Style="{StaticResource TitleSmall}"
                   Margin="0,10,0,0"/>
            <Grid ColumnDefinitions="*,*"
                  RowDefinitions="Auto,Auto"
                  ColumnSpacing="10"
                  RowSpacing="10">
                <Button Grid.Column="0" Grid.Row="0"
                        Text="Home"
                        material:ButtonIcon.Icon="{x:Static m:MaterialFilled.Home}"/>
                <Button Grid.Column="1" Grid.Row="0"
                        Text="Tune"
                        material:ButtonIcon.Icon="{x:Static m:MaterialFilled.Tune}"/>
                <Button Grid.Column="0" Grid.Row="1"
                        Text="Favorite"
                        material:ButtonIcon.Icon="{x:Static m:MaterialFilled.Favorite}"/>
                <Button Grid.Column="1" Grid.Row="1"
                        Text="Settings"
                        material:ButtonIcon.Icon="{x:Static m:MaterialFilled.Settings}"/>
            </Grid>

            <Label Text="Rounded Variant:"
                   Style="{StaticResource TitleSmall}"
                   Margin="0,10,0,0"/>
            <Grid ColumnDefinitions="*,*"
                  RowDefinitions="Auto,Auto"
                  ColumnSpacing="10"
                  RowSpacing="10">
                <Button Grid.Column="0" Grid.Row="0"
                        Text="Home"
                        material:ButtonIcon.Icon="{x:Static m:MaterialRound.Home}"/>
                <Button Grid.Column="1" Grid.Row="0"
                        Text="Tune"
                        material:ButtonIcon.Icon="{x:Static m:MaterialRound.Tune}"/>
                <Button Grid.Column="0" Grid.Row="1"
                        Text="Favorite"
                        material:ButtonIcon.Icon="{x:Static m:MaterialRound.Favorite}"/>
                <Button Grid.Column="1" Grid.Row="1"
                        Text="Settings"
                        material:ButtonIcon.Icon="{x:Static m:MaterialRound.Settings}"/>
            </Grid>

            <!-- Divider -->
            <BoxView HeightRequest="1"
                     Color="{StaticResource OutlineVariant}"
                     Margin="0,20,0,0"/>

            <!-- SVG Section -->
            <Label Text="Custom SVG Files (Current)"
                   Style="{StaticResource TitleLarge}"/>

            <Label Text="These are your current TabBar icons:"
                   Style="{StaticResource BodyMedium}"/>

            <Grid ColumnDefinitions="Auto,*"
                  RowDefinitions="Auto,Auto,Auto,Auto,Auto,Auto"
                  ColumnSpacing="15"
                  RowSpacing="15">

                <Image Grid.Column="0" Grid.Row="0"
                       Source="home"
                       WidthRequest="32"
                       HeightRequest="32"/>
                <Label Grid.Column="1" Grid.Row="0"
                       Text="home.svg (outline)"
                       VerticalOptions="Center"/>

                <Image Grid.Column="0" Grid.Row="1"
                       Source="home_fill"
                       WidthRequest="32"
                       HeightRequest="32"/>
                <Label Grid.Column="1" Grid.Row="1"
                       Text="home_fill.svg (filled)"
                       VerticalOptions="Center"/>

                <Image Grid.Column="0" Grid.Row="2"
                       Source="home_round"
                       WidthRequest="32"
                       HeightRequest="32"/>
                <Label Grid.Column="1" Grid.Row="2"
                       Text="home_round.svg (rounded)"
                       VerticalOptions="Center"/>

                <Image Grid.Column="0" Grid.Row="3"
                       Source="tune"
                       WidthRequest="32"
                       HeightRequest="32"/>
                <Label Grid.Column="1" Grid.Row="3"
                       Text="tune.svg (outline)"
                       VerticalOptions="Center"/>

                <Image Grid.Column="0" Grid.Row="4"
                       Source="tune_fill"
                       WidthRequest="32"
                       HeightRequest="32"/>
                <Label Grid.Column="1" Grid.Row="4"
                       Text="tune_fill.svg (filled)"
                       VerticalOptions="Center"/>

                <Image Grid.Column="0" Grid.Row="5"
                       Source="tune_round"
                       WidthRequest="32"
                       HeightRequest="32"/>
                <Label Grid.Column="1" Grid.Row="5"
                       Text="tune_round.svg (rounded)"
                       VerticalOptions="Center"/>
            </Grid>

            <!-- Comparison Notes -->
            <BoxView HeightRequest="1"
                     Color="{StaticResource OutlineVariant}"
                     Margin="0,20,0,0"/>

            <Label Text="Visual Comparison Notes"
                   Style="{StaticResource TitleMedium}"/>

            <Label Style="{StaticResource BodyMedium}">
                <Label.FormattedText>
                    <FormattedString>
                        <Span Text="✓ Material Symbols:" FontAttributes="Bold"/>
                        <Span Text="&#x0a;• Consistent with Google's Material Design&#x0a;• Automatic theming&#x0a;• Large icon library (2,500+)&#x0a;• Great for buttons, cards, lists&#x0a;&#x0a;"/>
                        <Span Text="✓ Custom SVG Files:" FontAttributes="Bold"/>
                        <Span Text="&#x0a;• Perfect for TabBar (Shell Icon property)&#x0a;• Full control over design&#x0a;• Smaller bundle size&#x0a;• Custom brand icons&#x0a;"/>
                    </FormattedString>
                </Label.FormattedText>
            </Label>

        </VerticalStackLayout>
    </ScrollView>
</ContentPage>
```

**Code-behind (`IconTestPage.xaml.cs`):**
```csharp
namespace MyVocaList.UI.Pages.DesignSystem;

public partial class IconTestPage : ContentPage
{
    public IconTestPage()
    {
        InitializeComponent();
    }
}
```

**Add to AppShell.xaml (for testing):**
```xml
<Tab Title="Icon Test" Icon="tune_round">
    <ShellContent ContentTemplate="{DataTemplate ds:IconTestPage}" Route="IconTestPage" />
</Tab>
```

---

## Step 4: Test and Compare

1. **Run the app**: `dotnet build && dotnet run`
2. **Navigate to the test page**
3. **Compare visual appearance:**
   - Sharpness and clarity
   - Theming and color application
   - Size consistency
   - Overall aesthetic fit with MD3

---

## Material Symbols Available Variants

UraniumUI Material Symbols provides three style variants:

- `MaterialOutlined` - Thin stroke outline (default Material style)
- `MaterialFilled` - Solid filled icons
- `MaterialRound` - Rounded corners variant

Each variant has 2,500+ icons including:
- Navigation: Home, Menu, ArrowBack, ArrowForward
- Actions: Add, Remove, Edit, Delete, Save, Search
- Content: Favorite, Star, Flag, Bookmark
- Communication: Email, Chat, Call, Notifications
- Media: Play, Pause, VolumeUp, MusicNote
- And many more...

---

## Performance Considerations

### Bundle Size Impact:

**Current SVG approach:**
- ~6 SVG files = ~12 KB total

**Material Symbols package:**
- Font file with 2,500+ icons = ~200-300 KB

**Recommendation:**
- TabBar icons → Keep SVG (you only need 2-6 icons)
- In-app UI → Material Symbols (access to full library)

---

## Final Decision Checklist

After testing, ask yourself:

1. **Visual Quality:**
   - [ ] Which icons look sharper?
   - [ ] Which theme better with your color system?
   - [ ] Which feel more "Material Design 3"?

2. **Development Experience:**
   - [ ] Is it easier to use Material Symbols for new icons?
   - [ ] Do you need custom icons not in Material library?
   - [ ] How important is bundle size?

3. **Use Case:**
   - [ ] TabBar navigation only? → **Keep SVG**
   - [ ] Lots of UI icons throughout app? → **Add Material Symbols**
   - [ ] Need custom brand icons? → **Hybrid approach**

---

## My Recommendation

**Hybrid Approach:**

1. **TabBar (Shell)** → Custom SVG files ✅
   - You already have them
   - Perfect theming with `currentColor`
   - Outline/fill variants for states
   - Zero setup needed

2. **In-App UI** → Material Symbols (optional)
   - Install for buttons, cards, lists
   - Quick access to consistent icons
   - Use when you don't need custom design

This gives you maximum flexibility and performance!
