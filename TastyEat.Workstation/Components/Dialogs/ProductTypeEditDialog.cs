using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TastyEat.Workstation.Models.Dto;
using TastyEat.Workstation.Models.Tables;
using TastyEat.Workstation.Options;
using TastyEat.Workstation.Services.Interfaces;
using TastyEat.Workstation.Ui;
using TastyEat.Workstation.Views.Utils;

namespace TastyEat.Workstation.Components.Dialogs;

public sealed partial class ProductTypeEditDialog : Window
{
    public sealed partial class State(StringLengthOptions stringLengthOptions) : ObservableObject
    {
        [ObservableProperty]
        public partial string Name { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string? ServerError { get; set; }

        [ObservableProperty]
        public partial string WindowTitle { get; set; } = "Добавить тип";

        public int Id { get; set; }

        public bool IsNew => Id == 0;

        public string? NameError =>
            string.IsNullOrWhiteSpace(Name)
                ? IsNew ? "Название типа обязательно" : null
                : Name.Trim().Length > stringLengthOptions.ProductTypeNameMaxLength
                    ? $"Название типа не должно превышать {stringLengthOptions.ProductTypeNameMaxLength} символов"
                    : ServerError;

        public bool CanSave => NameError is null;

        partial void OnNameChanged(string value)
        {
            ServerError = null;
            RaiseValidation();
        }

        partial void OnServerErrorChanged(string? value) => RaiseValidation();

        private void RaiseValidation()
        {
            OnPropertyChanged(nameof(NameError));
            OnPropertyChanged(nameof(CanSave));
        }
    }

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly State _state;

    public ProductTypeEditDialog(
        IServiceScopeFactory scopeFactory,
        IOptions<StringLengthOptions> stringLengthOptions)
    {
        _scopeFactory = scopeFactory;
        _state = new State(stringLengthOptions.Value);

        Width = 400;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Title = "Тип продукта";

        Content = new Grid { Margin = new Thickness(24) }.Rows("Auto, *, Auto")
            .Children(
                new TextBlock { FontSize = 20, FontWeight = Avalonia.Media.FontWeight.Bold }
                    .Text(_state, x => x.WindowTitle),
                new StackPanel { Margin = new Thickness(0, 20, 0, 0), Spacing = 4 }.Grid_Row(1)
                    .Children(
                        new TextBlock { Text = "Название типа" },
                        new TextBox { PlaceholderText = "Введите название типа" }
                            .Text(_state, x => x.Name, Avalonia.Data.BindingMode.TwoWay),
                        UiFactory.ErrorText(_state, x => x.NameError)),
                new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, HorizontalAlignment = HorizontalAlignment.Right }.Grid_Row(2)
                    .Children(
                        new Button { Content = "Сохранить", IsDefault = true }
                            .IsEnabled(_state, x => x.CanSave)
                            .OnClick(async _ => await SaveAsync()),
                        new Button { Content = "Отмена", IsCancel = true }
                            .OnClick(_ => Close(null))));
    }

    public void Initialize(ProductType? type = null)
    {
        _state.Id = type?.Id ?? 0;
        _state.Name = type?.Name ?? string.Empty;
        _state.WindowTitle = _state.IsNew ? "Добавить тип" : "Изменить тип";
        Title = _state.WindowTitle;
    }

    private async Task SaveAsync()
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var productTypeService = scope.ServiceProvider.GetRequiredService<IProductTypeService>();
            var name = _state.Name.Trim();

            if (await productTypeService.ExistsByNameAsync(name, _state.Id))
            {
                _state.ServerError = "Тип с таким названием уже существует";
                return;
            }

            if (_state.IsNew)
            {
                await productTypeService.CreateAsync(name);
                Close(true);
                return;
            }

            await productTypeService.UpdateAsync(new ProductTypeEditDto { Id = _state.Id, Name = name });
            Close(true);
        }
        catch (Exception exception)
        {
            await MessageDialog.ShowInfoAsync(this.GetOwnerWindow(), $"Не удалось сохранить тип продукта:\n{exception.Message}");
        }
    }
}
