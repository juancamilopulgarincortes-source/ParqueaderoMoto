using ParqueaderoMoto.Models;
using ParqueaderoMoto.Services;
using System.Globalization;
using Microsoft.Maui.Controls.Shapes;

namespace ParqueaderoMoto;

public class MainPage : ContentPage
{
    private const int Capacity = 20;

    // Paleta oscura: azul profundo, texto claro y acentos vivos.
    private static readonly Color PageBackground = Color.FromArgb("#07121A");
    private static readonly Color CardBackground = Color.FromArgb("#0D1B27");
    private static readonly Color FieldBackground = Color.FromArgb("#142638");
    private static readonly Color MainText = Color.FromArgb("#F3F7FC");
    private static readonly Color SecondaryText = Color.FromArgb("#A8BCD2");
    private static readonly Color MutedText = Color.FromArgb("#7F96AF");
    private static readonly Color BorderColor = Color.FromArgb("#263D50");
    private static readonly Color Green = Color.FromArgb("#0AA66A");
    private static readonly Color Blue = Color.FromArgb("#0878F9");
    private static readonly Color Gold = Color.FromArgb("#FFBD32");

    private readonly ParkingDatabase _database = new();
    private readonly Entry _plate = StyledEntry("Placa (ej. ABC12D)", 10);
    private readonly Entry _customer = StyledEntry("Nombre del cliente (opcional)");
    private readonly Picker _spacePicker = StyledPicker("Selecciona un puesto");
    private readonly Entry _exitPlate = StyledEntry("Placa para registrar salida", 10);
    private readonly Entry _hourlyRate = StyledEntry("Tarifa por hora", 12);
    private readonly Picker _paymentPicker = StyledPicker("Método de pago");
    private readonly Label _summary = new() { FontSize = 17, TextColor = Color.FromArgb("#28D98A") };
    private readonly Label _income = new() { FontSize = 17, TextColor = Gold };
    private readonly Label _occupied = new() { Text = "0 ocupados", FontSize = 17, TextColor = Color.FromArgb("#FF6868") };
    private readonly VerticalStackLayout _activeList = new() { Spacing = 9 };
    private readonly Label _history = new()
    {
        FontSize = 13,
        TextColor = SecondaryText,
        LineBreakMode = LineBreakMode.WordWrap
    };

    public MainPage()
    {
        Title = "Parqueadero";
        BackgroundColor = PageBackground;
        Shell.SetBackgroundColor(this, PageBackground);
        Shell.SetForegroundColor(this, MainText);

        _hourlyRate.Text = "3000";
        _hourlyRate.Keyboard = Keyboard.Numeric;

        for (int i = 1; i <= Capacity; i++)
            _spacePicker.Items.Add($"Puesto {i}");

        _paymentPicker.Items.Add("Efectivo");
        _paymentPicker.Items.Add("Transferencia");
        _paymentPicker.Items.Add("Tarjeta");
        _paymentPicker.SelectedIndex = 0;

        var content = new VerticalStackLayout
        {
            Padding = new Thickness(18, 22, 18, 28),
            Spacing = 17,
            Children =
            {
                new HorizontalStackLayout
                {
                    Spacing = 12,
                    VerticalOptions = LayoutOptions.Center,
                    Children =
                    {
                        new Label
                        {
                            Text = "🏍️",
                            FontSize = 34,
                            VerticalOptions = LayoutOptions.Center
                        },
                        new VerticalStackLayout
                        {
                            Spacing = 2,
                            VerticalOptions = LayoutOptions.Center,
                            Children =
                            {
                                new Label
                                {
                                    Text = "PARQUEADERO",
                                    FontSize = 27,
                                    FontAttributes = FontAttributes.Bold,
                                    TextColor = MainText
                                },
                                new Label
                                {
                                    Text = "Control de entradas, puestos y cobros",
                                    FontSize = 13,
                                    TextColor = SecondaryText
                                }
                            }
                        }
                    }
                },

                Card(new VerticalStackLayout
                {
                    Spacing = 12,
                    Children =
                    {
                        new Label
                        {
                            Text = "ESTADO DEL PARQUEADERO",
                            FontAttributes = FontAttributes.Bold,
                            FontSize = 13,
                            TextColor = SecondaryText
                        },
                        new Grid
                        {
                            ColumnDefinitions =
                            {
                                new ColumnDefinition(GridLength.Star),
                                new ColumnDefinition(GridLength.Star)
                            },
                            ColumnSpacing = 10,
                            Children =
                            {
                                StatTile("🅿️", _summary, "#0E2A25").Column(0),
                                StatTile("🏍️", _occupied, "#2A1C25").Column(1)
                            }
                        },
                        new BoxView { HeightRequest = 1, Color = BorderColor },
                        new HorizontalStackLayout
                        {
                            Spacing = 10,
                            Children =
                            {
                                new Label { Text = "💰", FontSize = 23, VerticalOptions = LayoutOptions.Center },
                                new Label { Text = "Ingresos de hoy:", FontSize = 15, TextColor = SecondaryText, VerticalOptions = LayoutOptions.Center },
                                _income
                            }
                        }
                    }
                }, "#0B211F", Color.FromArgb("#11634D")),

                SectionHeader("🟢", "Registrar entrada"),
                Card(new VerticalStackLayout
                {
                    Spacing = 10,
                    Children =
                    {
                        FieldLabel("Placa de la moto"), _plate,
                        FieldLabel("Nombre del cliente (opcional)"), _customer,
                        FieldLabel("Selecciona un puesto"), _spacePicker,
                        ActionButton("➜   REGISTRAR ENTRADA", RegisterEntryClicked, Green)
                    }
                }),

                SectionHeader("🔵", "Registrar salida y cobrar"),
                Card(new VerticalStackLayout
                {
                    Spacing = 10,
                    Children =
                    {
                        FieldLabel("Placa de la moto"), _exitPlate,
                        FieldLabel("Tarifa por hora (COP)"), _hourlyRate,
                        FieldLabel("Método de pago"), _paymentPicker,
                        ActionButton("▤   REGISTRAR SALIDA Y COBRAR", RegisterExitClicked, Blue)
                    }
                }),

                SectionHeader("🟣", "Motos estacionadas"),
                Card(_activeList),
                SectionHeader("📋", "Últimos movimientos"),
                Card(_history)
            }
        };

        Content = new ScrollView
        {
            Content = content,
            BackgroundColor = PageBackground
        };

        Loaded += async (_, _) => await RefreshAsync();
    }

    private static Entry StyledEntry(string placeholder, int maxLength = 100) => new()
    {
        Placeholder = placeholder,
        MaxLength = maxLength,
        TextColor = MainText,
        PlaceholderColor = MutedText,
        BackgroundColor = FieldBackground,
        HeightRequest = 52,
        Margin = new Thickness(0, 0, 0, 2),
        ClearButtonVisibility = ClearButtonVisibility.WhileEditing
    };

    private static Picker StyledPicker(string title) => new()
    {
        Title = title,
        TitleColor = MutedText,
        TextColor = MainText,
        BackgroundColor = FieldBackground,
        HeightRequest = 52
    };

    private static View FieldLabel(string text) => new Label
    {
        Text = text,
        FontSize = 14,
        TextColor = SecondaryText,
        Margin = new Thickness(1, 3, 0, -5)
    };

    private static View SectionHeader(string emoji, string title) => new HorizontalStackLayout
    {
        Spacing = 10,
        VerticalOptions = LayoutOptions.Center,
        Children =
        {
            new Border
            {
                BackgroundColor = title.Contains("entrada")
                    ? Color.FromArgb("#075A42")
                    : title.Contains("salida")
                        ? Color.FromArgb("#123D79")
                        : Color.FromArgb("#38215F"),
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 24 },
                Padding = new Thickness(10),
                Content = new Label { Text = emoji, FontSize = 18 }
            },
            new Label
            {
                Text = title,
                FontSize = 20,
                FontAttributes = FontAttributes.Bold,
                TextColor = MainText,
                VerticalOptions = LayoutOptions.Center
            }
        }
    };

    private static View StatTile(string emoji, Label value, string background) => new Border
    {
        BackgroundColor = Color.FromArgb(background),
        Stroke = BorderColor,
        StrokeThickness = 1,
        Padding = 10,
        StrokeShape = new RoundRectangle { CornerRadius = 13 },
        Content = new VerticalStackLayout
        {
            Spacing = 5,
            Children =
            {
                new Label { Text = emoji, FontSize = 23 },
                value
            }
        }
    };

    private static View Card(View child, string? background = null, Color? border = null) => new Border
    {
        BackgroundColor = Color.FromArgb(background ?? "#0D1B27"),
        Stroke = border ?? BorderColor,
        StrokeThickness = 1,
        Padding = 14,
        StrokeShape = new RoundRectangle { CornerRadius = 19 },
        Content = child
    };

    private static Button ActionButton(string text, EventHandler handler, Color color) => new()
    {
        Text = text,
        BackgroundColor = color,
        TextColor = Colors.White,
        CornerRadius = 13,
        HeightRequest = 56,
        FontSize = 14,
        FontAttributes = FontAttributes.Bold,
        Padding = new Thickness(10, 0),
        Command = new Command(() => handler.Invoke(null, EventArgs.Empty))
    };

    private async void RegisterEntryClicked(object? sender, EventArgs e)
    {
        if (_spacePicker.SelectedIndex < 0)
        {
            await DisplayAlertAsync("Falta el puesto", "Selecciona un puesto.", "OK");
            return;
        }

        var result = await _database.RegisterEntryAsync(
            _plate.Text ?? "",
            _customer.Text ?? "",
            _spacePicker.SelectedIndex + 1);

        await DisplayAlertAsync(result.Success ? "Listo" : "No se pudo registrar", result.Message, "OK");
        if (result.Success)
        {
            _plate.Text = "";
            _customer.Text = "";
            _spacePicker.SelectedIndex = -1;
            await RefreshAsync();
        }
    }

    private async void RegisterExitClicked(object? sender, EventArgs e)
    {
        if (!long.TryParse(_hourlyRate.Text, out var rate) || rate <= 0)
        {
            await DisplayAlertAsync("Tarifa inválida", "Escribe una tarifa por hora mayor que cero.", "OK");
            return;
        }

        var method = _paymentPicker.SelectedItem?.ToString() ?? "Efectivo";
        var result = await _database.RegisterExitAsync(_exitPlate.Text ?? "", rate, method);
        await DisplayAlertAsync(result.Success ? "Cobro registrado" : "No se pudo cobrar", result.Message, "OK");

        if (result.Success)
        {
            _exitPlate.Text = "";
            await RefreshAsync();
        }
    }

    private async Task RefreshAsync()
    {
        var active = await _database.GetActiveAsync();
        _summary.Text = $"{Capacity - active.Count} puestos disponibles";
        _occupied.Text = $"{active.Count} ocupados";

        var income = await _database.GetTodayIncomeAsync();
        _income.Text = income.ToString("C0", CultureInfo.GetCultureInfo("es-CO"));

        _activeList.Children.Clear();
        if (active.Count == 0)
        {
            _activeList.Children.Add(new Label
            {
                Text = "🏍️  No hay motos estacionadas.",
                TextColor = SecondaryText,
                Padding = new Thickness(4, 8)
            });
        }
        else
        {
            foreach (VehicleVisit visit in active)
            {
                _activeList.Children.Add(Card(new VerticalStackLayout
                {
                    Spacing = 4,
                    Children =
                    {
                        new Label
                        {
                            Text = $"🏍️  {visit.Plate} · Puesto {visit.ParkingSpace}",
                            FontAttributes = FontAttributes.Bold,
                            TextColor = MainText,
                            FontSize = 15
                        },
                        new Label
                        {
                            Text = $"{(string.IsNullOrWhiteSpace(visit.CustomerName) ? "Cliente sin nombre" : visit.CustomerName)}  |  Entrada: {visit.EntryAt:dd/MM/yyyy HH:mm}",
                            FontSize = 12,
                            TextColor = SecondaryText
                        }
                    }
                }, "#102331"));
            }
        }

        var history = await _database.GetHistoryAsync();
        _history.Text = history.Count == 0
            ? "Todavía no hay movimientos."
            : string.Join("\n\n", history.Take(5).Select(v =>
                $"{v.Plate} · {v.EntryAt:dd/MM HH:mm} · {(v.ExitAt is null ? "Dentro" : $"Cobrado {v.AmountPaid.ToString("C0", CultureInfo.GetCultureInfo("es-CO"))}")}"));
    }
}

internal static class GridExtensions
{
    public static T Column<T>(this T view, int column) where T : View
    {
        Grid.SetColumn(view, column);
        return view;
    }
}
