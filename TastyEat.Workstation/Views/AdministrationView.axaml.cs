using Avalonia.ReactiveUI;
using TastyEat.Workstation.ViewModels;

namespace TastyEat.Workstation.Views;

public partial class AdministrationView : ReactiveUserControl<AdministrationViewModel>
{
    public AdministrationView()
    {
        InitializeComponent();
    }
}
