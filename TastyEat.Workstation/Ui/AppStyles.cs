using System;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Avalonia.Styling;
using static Avalonia.Styling.Selectors;
using TastyEat.Workstation.Models.Tables;

namespace TastyEat.Workstation.Ui;

public sealed class AppStyles : Styles
{
    public static readonly SolidColorBrush Accent = new(Color.Parse("#512BD4"));
    public static readonly SolidColorBrush AccentLight = new(Color.Parse("#7559E0"));
    public static readonly SolidColorBrush AccentPurple = new(Color.Parse("#E6E1F9"));

    public static AppStyles Instance { get; } = new();

    private static BrushTransition BackgroundTransition => new() { Property = Button.BackgroundProperty, Duration = TimeSpan.FromSeconds(0.15) };
    private static BrushTransition BorderBrushTransition => new() { Property = Button.BorderBrushProperty, Duration = TimeSpan.FromSeconds(0.15) };

    private AppStyles()
    {
        Add(ButtonStyles());
        Add(TabStyles());
        Add(ManagementLayoutStyles());
        Add(ControlStyles());
    }

    private static Styles ButtonStyles()
    {
        var brushTransitions = new Transitions
        {
            BackgroundTransition,
            BorderBrushTransition
        };

        return
        [
            new Style(x => x.OfType<Button>())
            {
                Setters =
                {
                    new Setter(Button.BackgroundProperty, Accent),
                    new Setter(Button.ForegroundProperty, Brushes.White),
                    new Setter(Button.BorderBrushProperty, Accent),
                    new Setter(Button.CornerRadiusProperty, new CornerRadius(8)),
                    new Setter(Button.TransitionsProperty, brushTransitions)
                }
            },
            new Style(x => x.OfType<Button>().Class(":pointerover"))
            {
                Setters =
                {
                    new Setter(Button.BackgroundProperty, AccentLight),
                    new Setter(Button.BorderBrushProperty, AccentLight)
                }
            },
            new Style(x => x.OfType<Button>().Class(":pressed"))
            {
                Setters =
                {
                    new Setter(Button.BackgroundProperty, Accent),
                    new Setter(Button.BorderBrushProperty, Accent)
                }
            },
            DisabledButtonStyle(),
            SidebarActionButtonStyle(),
            new Style(x => x.OfType<Button>().Class("sidebarAction").Class(":pointerover"))
            {
                Setters =
                {
                    new Setter(Button.BackgroundProperty, new SolidColorBrush(Color.Parse("#F5F3FF"))),
                    new Setter(Button.BorderBrushProperty, Accent)
                }
            },
            AccentButtonStyle(),
            new Style(x => x.OfType<Button>().Class("accent").Class(":pointerover"))
            {
                Setters =
                {
                    new Setter(Button.BackgroundProperty, Brushes.White),
                    new Setter(Button.BorderBrushProperty, Accent),
                    new Setter(Button.ForegroundProperty, Accent)
                }
            },
            new Style(x => x.OfType<Button>().Class("accent").Class("sidebarAction"))
            {
                Setters =
                {
                    new Setter(Button.BackgroundProperty, Accent),
                    new Setter(Button.BorderBrushProperty, Accent),
                    new Setter(Button.ForegroundProperty, Brushes.White)
                }
            },
            new Style(x => x.OfType<Button>().Class("accent").Class("sidebarAction").Class(":pointerover"))
            {
                Setters =
                {
                    new Setter(Button.BackgroundProperty, Brushes.White),
                    new Setter(Button.BorderBrushProperty, Accent),
                    new Setter(Button.ForegroundProperty, Accent)
                }
            },
            new Style(x => x.OfType<Button>().Class("accent").Class(":pressed"))
            {
                Setters =
                {
                    new Setter(Button.BackgroundProperty, AccentLight),
                    new Setter(Button.BorderBrushProperty, AccentLight),
                    new Setter(Button.ForegroundProperty, Brushes.White)
                }
            },
            DisabledButtonStyle("accent"),
            DisabledButtonStyle("accent", "sidebarAction"),
            ActionButtonStyle(),
            new Style(x => x.OfType<Button>().Class("action").Class(":pointerover"))
            {
                Setters = { new Setter(Button.BackgroundProperty, new SolidColorBrush(Color.Parse("#F0F0F0"))) }
            },
            new Style(x => x.OfType<Button>().Class("action").Class(":disabled").Template().OfType<ContentPresenter>().Name("PART_ContentPresenter"))
            {
                Setters =
                {
                    new Setter(Panel.BackgroundProperty, Brushes.Transparent),
                    new Setter(TextElement.ForegroundProperty, new SolidColorBrush(Color.Parse("#888888")))
                }
            }
        ];
    }

    private static Style DisabledButtonStyle(params string[] classes) =>
        new(type =>
        {
            var selector = type.OfType<Button>().Class(":disabled").Template().OfType<ContentPresenter>().Name("PART_ContentPresenter");
            foreach (var className in classes)
                selector = selector.Class(className);
            return selector;
        })
        {
            Setters =
            {
                new Setter(Panel.BackgroundProperty, new SolidColorBrush(Color.Parse("#CCCCCC"))),
                new Setter(Border.BorderBrushProperty, new SolidColorBrush(Color.Parse("#CCCCCC"))),
                new Setter(TextElement.ForegroundProperty, new SolidColorBrush(Color.Parse("#888888")))
            }
        };

    private static Style SidebarActionButtonStyle() =>
        new(x => x.OfType<Button>().Class("sidebarAction"))
        {
            Setters =
            {
                new Setter(Button.HorizontalAlignmentProperty, HorizontalAlignment.Stretch),
                new Setter(Button.HorizontalContentAlignmentProperty, HorizontalAlignment.Left),
                new Setter(Button.BackgroundProperty, Brushes.White),
                new Setter(Button.ForegroundProperty, new SolidColorBrush(Color.Parse("#333333"))),
                new Setter(Button.BorderBrushProperty, new SolidColorBrush(Color.Parse("#E0E0E0"))),
                new Setter(Button.BorderThicknessProperty, new Thickness(1)),
                new Setter(Button.CornerRadiusProperty, new CornerRadius(10)),
                new Setter(Button.PaddingProperty, new Thickness(14, 12)),
                new Setter(Button.FontWeightProperty, FontWeight.Medium),
                new Setter(Button.TransitionsProperty, new Transitions
                {
                    BackgroundTransition,
                    BorderBrushTransition,
                    new TransformOperationsTransition { Property = Visual.RenderTransformProperty, Duration = TimeSpan.FromSeconds(0.1) }
                })
            }
        };

    private static Style AccentButtonStyle() =>
        new(x => x.OfType<Button>().Class("accent"))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Accent),
                new Setter(Button.ForegroundProperty, Brushes.White),
                new Setter(Button.BorderBrushProperty, Accent),
                new Setter(Button.BorderThicknessProperty, new Thickness(1)),
                new Setter(Button.CornerRadiusProperty, new CornerRadius(10)),
                new Setter(Button.PaddingProperty, new Thickness(14, 12)),
                new Setter(Button.FontWeightProperty, FontWeight.SemiBold),
                new Setter(Button.TransitionsProperty, new Transitions { BackgroundTransition })
            }
        };

    private static Style ActionButtonStyle() =>
        new(x => x.OfType<Button>().Class("action"))
        {
            Setters =
            {
                new Setter(Button.ForegroundProperty, Accent),
                new Setter(Button.BackgroundProperty, Brushes.Transparent),
                new Setter(Button.BorderBrushProperty, Brushes.Transparent),
                new Setter(Button.PaddingProperty, new Thickness(6)),
                new Setter(Button.WidthProperty, 36.0),
                new Setter(Button.HeightProperty, 36.0),
                new Setter(Button.CornerRadiusProperty, new CornerRadius(8)),
                new Setter(Button.TransitionsProperty, new Transitions { BackgroundTransition })
            }
        };

    private static Styles TabStyles() =>
    [
        new Style(x => x.OfType<TabItem>())
        {
            Setters =
            {
                new Setter(TabItem.MinWidthProperty, 160.0),
                new Setter(TabItem.MaxWidthProperty, 320.0),
                new Setter(TabItem.HeightProperty, 56.0),
                new Setter(TabItem.PaddingProperty, new Thickness(16, 0, 6, 0)),
                new Setter(TabItem.FontSizeProperty, 15.0),
                new Setter(TabItem.FontWeightProperty, FontWeight.Medium),
                new Setter(TabItem.VerticalContentAlignmentProperty, VerticalAlignment.Center),
                new Setter(TabItem.BackgroundProperty, Brushes.Transparent),
                new Setter(TabItem.ForegroundProperty, new SolidColorBrush(Color.Parse("#333333")))
            }
        },
        new Style(x => x.OfType<TabItem>().Class(":selected"))
        {
            Setters =
            {
                new Setter(TabItem.BackgroundProperty, Accent),
                new Setter(TabItem.ForegroundProperty, Brushes.White)
            }
        },
        new Style(x => x.OfType<TabItem>().Class(":selected").Template().OfType<ContentPresenter>().Name("PART_ContentPresenter"))
        {
            Setters = { new Setter(TextElement.ForegroundProperty, Brushes.White) }
        },
        new Style(x => x.OfType<TabItem>().Class(":selected").Template().OfType<Material.Icons.Avalonia.MaterialIcon>())
        {
            Setters = { new Setter(TextElement.ForegroundProperty, Brushes.White) }
        },
        new Style(x => x.OfType<TabItem>().Class(":pointerover").Template().OfType<ContentPresenter>().Name("PART_ContentPresenter"))
        {
            Setters =
            {
                new Setter(Panel.BackgroundProperty, AccentLight),
                new Setter(TextElement.ForegroundProperty, Brushes.White)
            }
        }
    ];

    private static Styles ManagementLayoutStyles() =>
    [
        new Style(x => x.OfType<Grid>().Class("managementLayout")) { Setters = { new Setter(Grid.MarginProperty, new Thickness(24)) } },
        new Style(x => x.OfType<StackPanel>().Class("topbar"))
        {
            Setters =
            {
                new Setter(StackPanel.OrientationProperty, Orientation.Horizontal),
                new Setter(StackPanel.SpacingProperty, 12.0)
            }
        },
        new Style(x => x.OfType<Grid>().Class("topbar")) { Setters = { new Setter(Grid.MarginProperty, new Thickness(0, 24, 0, 0)) } },
        new Style(x => x.OfType<Border>().Class("dataGridHost"))
        {
            Setters =
            {
                new Setter(Border.BackgroundProperty, Brushes.White),
                new Setter(Border.BorderBrushProperty, new SolidColorBrush(Color.Parse("#E8E8E8"))),
                new Setter(Border.BorderThicknessProperty, new Thickness(1)),
                new Setter(Border.CornerRadiusProperty, new CornerRadius(12)),
                new Setter(Border.MarginProperty, new Thickness(0, 24, 0, 0)),
                new Setter(Border.BoxShadowProperty, BoxShadows.Parse("0 2 12 0 #15000000")),
                new Setter(Border.PaddingProperty, new Thickness(1))
            }
        }
    ];

    private static Styles ControlStyles() =>
    [
        new Style(x => x.OfType<TextBox>().Template().OfType<Border>().Name("PART_ContentPresenterBorder"))
        {
            Setters = { new Setter(Border.BorderBrushProperty, Accent) }
        },
        new Style(x => x.OfType<TextBox>().Class(":pointerover").Template().OfType<Border>().Name("PART_ContentPresenterBorder"))
        {
            Setters = { new Setter(Border.BorderBrushProperty, AccentLight) }
        },
        new Style(x => x.OfType<TextBox>().Class(":focus").Template().OfType<Border>().Name("PART_ContentPresenterBorder"))
        {
            Setters = { new Setter(Border.BorderBrushProperty, AccentLight) }
        },
        new Style(x => x.OfType<TextBox>().Class(":disabled").Template().OfType<Border>().Name("PART_ContentPresenterBorder"))
        {
            Setters = { new Setter(Border.BorderBrushProperty, new SolidColorBrush(Color.Parse("#CCCCCC"))) }
        },
        new Style(x => x.OfType<CheckBox>().Template().OfType<Border>().Name("NormalRectangle"))
        {
            Setters = { new Setter(Border.BorderBrushProperty, Accent) }
        },
        new Style(x => x.OfType<CheckBox>().Class(":pointerover").Template().OfType<Border>().Name("NormalRectangle"))
        {
            Setters = { new Setter(Border.BorderBrushProperty, AccentLight) }
        },
        new Style(x => x.OfType<CheckBox>().Class(":checked").Template().OfType<Border>().Name("NormalRectangle"))
        {
            Setters =
            {
                new Setter(Panel.BackgroundProperty, Accent),
                new Setter(Border.BorderBrushProperty, Accent)
            }
        },
        new Style(x => x.OfType<CheckBox>().Class(":indeterminate").Template().OfType<Border>().Name("NormalRectangle"))
        {
            Setters =
            {
                new Setter(Panel.BackgroundProperty, Accent),
                new Setter(Border.BorderBrushProperty, Accent)
            }
        },
        new Style(x => x.OfType<ComboBox>().Template().OfType<Border>().Name("Background"))
        {
            Setters = { new Setter(Border.BorderBrushProperty, Accent) }
        },
        new Style(x => x.OfType<ComboBox>().Class(":pointerover").Template().OfType<Border>().Name("Background"))
        {
            Setters = { new Setter(Border.BorderBrushProperty, AccentLight) }
        },
        new Style(x => x.OfType<ComboBox>().Class(":focus").Template().OfType<Border>().Name("Background"))
        {
            Setters = { new Setter(Border.BorderBrushProperty, AccentLight) }
        },
        new Style(x => x.OfType<ComboBox>().Class(":dropdownopen").Template().OfType<Border>().Name("Background"))
        {
            Setters = { new Setter(Border.BorderBrushProperty, AccentLight) }
        },
        new Style(x => x.OfType<ComboBoxItem>().Class(":pointerover").Template().OfType<ContentPresenter>().Name("PART_ContentPresenter"))
        {
            Setters = { new Setter(Panel.BackgroundProperty, AccentLight) }
        },
        new Style(x => x.OfType<ComboBoxItem>().Class(":selected").Template().OfType<ContentPresenter>().Name("PART_ContentPresenter"))
        {
            Setters = { new Setter(Panel.BackgroundProperty, Accent) }
        },
        new Style(x => x.OfType<AutoCompleteBox>())
        {
            Setters =
            {
                new Setter(AutoCompleteBox.ForegroundProperty, new SolidColorBrush(Color.Parse("#333333"))),
                new Setter(AutoCompleteBox.PlaceholderForegroundProperty, new SolidColorBrush(Color.Parse("#666666"))),
                new Setter(AutoCompleteBox.IsTextCompletionEnabledProperty, false),
                new Setter(AutoCompleteBox.ItemTemplateProperty, new FuncDataTemplate<Client>(
                    (client, _) => new TextBlock { Foreground = new SolidColorBrush(Color.Parse("#333333")), Text = client?.FullName ?? string.Empty }))
            }
        },
        new Style(x => x.OfType<NumericUpDown>().Template().OfType<TextBox>().Name("PART_TextBox"))
        {
            Setters = { new Setter(TextBox.IsReadOnlyProperty, true) }
        },
        new Style(x => x.OfType<NumericUpDown>().Template().OfType<ButtonSpinner>().Name("PART_Spinner"))
        {
            Setters = { new Setter(Border.BorderBrushProperty, Accent) }
        },
        new Style(x => x.OfType<NumericUpDown>().Class(":pointerover").Template().OfType<ButtonSpinner>().Name("PART_Spinner"))
        {
            Setters = { new Setter(Border.BorderBrushProperty, AccentLight) }
        },
        new Style(x => x.OfType<NumericUpDown>().Class(":focus").Template().OfType<ButtonSpinner>().Name("PART_Spinner"))
        {
            Setters = { new Setter(Border.BorderBrushProperty, AccentLight) }
        },
        new Style(x => x.OfType<DatePicker>())
        {
            Setters = { new Setter(Border.BorderBrushProperty, Accent) }
        },
        new Style(x => x.OfType<Calendar>())
        {
            Setters =
            {
                new Setter(Panel.BackgroundProperty, Brushes.White),
                new Setter(TemplatedControl.ForegroundProperty, Accent)
            }
        },
        new Style(x => x.OfType<CalendarDayButton>().Class(":selected"))
        {
            Setters =
            {
                new Setter(Panel.BackgroundProperty, Accent),
                new Setter(TemplatedControl.ForegroundProperty, Brushes.White)
            }
        },
        new Style(x => x.OfType<CalendarDayButton>().Class(":pointerover"))
        {
            Setters =
            {
                new Setter(Panel.BackgroundProperty, AccentLight),
                new Setter(TemplatedControl.ForegroundProperty, Brushes.White)
            }
        },
        new Style(x => x.OfType<CalendarButton>().Class(":selected"))
        {
            Setters =
            {
                new Setter(Panel.BackgroundProperty, Accent),
                new Setter(TemplatedControl.ForegroundProperty, Brushes.White)
            }
        },
        new Style(x => x.OfType<CalendarButton>().Class(":pointerover"))
        {
            Setters =
            {
                new Setter(Panel.BackgroundProperty, AccentLight),
                new Setter(TemplatedControl.ForegroundProperty, Brushes.White)
            }
        }
    ];
}
