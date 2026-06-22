using System.Collections.ObjectModel;
using System.Reactive;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace TastyEat.Workstation.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel(ClientsViewModel clients, ProductsViewModel products, ProductionsViewModel productions, AnalyticsViewModel analytics)
    {
        TabItems =
        [
            clients,
            products,
            productions,
            analytics
        ];
    }

    [Reactive]
    private int _selectedTabIndex;

    public ObservableCollection<ViewModelBase> TabItems { get; }

    public Interaction<string, Unit> ShowError { get; } = new();

    public override string Title => "TastyEat Workstation";
    public override string IconName => "ApplicationOutline";
}
