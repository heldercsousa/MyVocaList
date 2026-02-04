using MyVocaList.UI.Pages.Samples.BottomSheet;
using MyVocaList.UI.Pages.Samples.CollectionView;
using MyVocaList.UI.Pages.Samples.DataForm;
using MyVocaList.UI.Pages.Samples.Editors;
using MyVocaList.UI.Pages.Samples.Popup;
using MyVocaList.UI.Pages.Samples.TabView;

namespace MyVocaList;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

#if DEBUG
        // DevExpress Sample Routes - Debug Only
        Routing.RegisterRoute("sample/swipe", typeof(SwipeContainerSample));
        Routing.RegisterRoute("sample/dragdrop", typeof(DragDropSample));
        Routing.RegisterRoute("sample/filterchips", typeof(FilterChipsSample));
        Routing.RegisterRoute("sample/editform", typeof(EditFormSample));
        Routing.RegisterRoute("sample/combobox", typeof(ComboBoxEditorSample));
        Routing.RegisterRoute("sample/editors", typeof(EditorsSample));
        Routing.RegisterRoute("sample/tabview", typeof(BottomTabViewSample));
        Routing.RegisterRoute("sample/popup", typeof(PopupServiceSample));
        Routing.RegisterRoute("sample/bottomsheet", typeof(BottomSheetSample));
#endif
    }
}