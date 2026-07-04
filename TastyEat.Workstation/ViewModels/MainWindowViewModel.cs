using System.Collections.ObjectModel;
using System.Reactive;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace TastyEat.Workstation.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel(
        ClientsViewModel clients,
        ProductsViewModel products,
        ProductionsViewModel productions,
        OrderCollectionViewModel orderCollection,
        AnalyticsViewModel analytics,
        AdministrationViewModel administration)
    {
        TabItems =
        [
            clients,
            products,
            productions,
            orderCollection,
            analytics,
            administration
        ];
    }

    [Reactive]
    private int _selectedTabIndex;

    public ObservableCollection<ViewModelBase> TabItems { get; }

    public Interaction<string, Unit> ShowError { get; } = new();

    public override string Title => "TastyEat Workstation";
    public override string IconName => "ApplicationOutline";
}
