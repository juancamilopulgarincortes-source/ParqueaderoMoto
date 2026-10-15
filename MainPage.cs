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

    private readonly CarouselView _carousel = new()
    {
        IsSwipeEnabled = true,
        Loop = false,
        HorizontalScrollBarVisibility = ScrollBarVisibility.Never,
        VerticalOptions = LayoutOptions.FillAndExpand
    };
    private readonly Label _pageIndicator = new()
    {
        Text = "1 de 4 · Inicio",
        FontSize = 12,
        TextColor = SecondaryText,
        HorizontalTextAlignment = TextAlignment.Center,
        VerticalTextAlignment = TextAlignment.Center
    };

    public MainPage()
    {
        Title = "Parqueadero Moto";
        NavigationPage.SetHasNavigationBar(this, false);
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

        var pages = new List<View>
        {
            BuildDashboardPage(),
            BuildEntryPage(),
            BuildExitPage(),
            BuildManagementPage()
        };

        _carousel.ItemsSource = pages;
        _carousel.ItemTemplate = new DataTemplate(() =>
        {
            var holder = new ContentView();
            holder.SetBinding(ContentView.ContentProperty, ".");
            return holder;
        });
        _carousel.PositionChanged += (_, args) => UpdatePageIndicator(args.CurrentPosition);

        Grid.SetColumnSpan(_pageIndicator, 2);
        var header = new Grid
        {
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto) },
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            ColumnSpacing = 8,
            Padding = new Thickness(18, 16, 18, 8),
            Children =
            {
                new HorizontalStackLayout
                {
                    Spacing = 10,
                    VerticalOptions = LayoutOptions.Center,
                    Children =
                    {
                        new Border
                        {
                            BackgroundColor = Color.FromArgb("#12382F"),
                            StrokeThickness = 0,
                            StrokeShape = new RoundRectangle { CornerRadius = 15 },
                            Padding = 9,
                            Content = new Label { Text = "🏍️", FontSize = 25 }
                        },
                        new VerticalStackLayout
                        {
                            Spacing = 1,
                            VerticalOptions = LayoutOptions.Center,
                            Children =
                            {
                                new Label { Text = "MOTO PARK", FontSize = 21, FontAttributes = FontAttributes.Bold, TextColor = MainText },
                                new Label { Text = "Gestión de parqueadero", FontSize = 12, TextColor = SecondaryText }
                            }
                        }
                    }
                }.Column(0).Row(0),
                new Button
                {
                    Text = "Salir",
                    BackgroundColor = FieldBackground,
                    TextColor = SecondaryText,
                    FontSize = 12,
                    Padding = new Thickness(12, 0),
                    HeightRequest = 40,
                    CornerRadius = 12,
                    VerticalOptions = LayoutOptions.Center,
                    Command = new Command(Logout)
                }.Column(1).Row(0),
                _pageIndicator.Row(1)
            }
        };

        var navigation = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 7,
            Padding = new Thickness(12, 8, 12, 12),
            Children =
            {
                NavigationButton("⌂", "Inicio", 0).Column(0),
                NavigationButton("＋", "Entrada", 1).Column(1),
                NavigationButton("＄", "Salida", 2).Column(2),
                NavigationButton("▤", "Control", 3).Column(3)
            }
        };

        Content = new Grid
        {
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star), new RowDefinition(GridLength.Auto) },
            BackgroundColor = PageBackground,
            Children = { header.Row(0), _carousel.Row(1), navigation.Row(2) }
        };

        Loaded += async (_, _) => await RefreshAsync();
    }

    private View BuildDashboardPage()
    {
        var content = new VerticalStackLayout
        {
            Padding = new Thickness(18, 12, 18, 24),
            Spacing = 16,
            Children =
            {
                new Label { Text = "¡Hola! 👋", FontSize = 26, FontAttributes = FontAttributes.Bold, TextColor = MainText },
                new Label { Text = "Este es el resumen de tu parqueadero.", FontSize = 14, TextColor = SecondaryText },
                Card(new VerticalStackLayout
                {
                    Spacing = 13,
                    Children =
                    {
                        new Label { Text = "RESUMEN DE HOY", FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = SecondaryText },
                        new Grid
                        {
                            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
                            ColumnSpacing = 10,
                            Children = { StatTile("🅿️", _summary, "#0E2A25").Column(0), StatTile("🏍️", _occupied, "#2A1C25").Column(1) }
                        },
                        new BoxView { HeightRequest = 1, Color = BorderColor },
                        new Label { Text = "INGRESOS REGISTRADOS", FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = SecondaryText },
                        _income,
                        new Label { Text = "Desliza hacia los lados para moverte entre las secciones, o usa los botones de abajo.", FontSize = 13, TextColor = SecondaryText }
                    }
                }, "#0B211F", Color.FromArgb("#11634D")),
                ActionButton("＋   Registrar entrada", (_, _) => GoToPage(1), Green),
                ActionButton("＄   Registrar salida y cobrar", (_, _) => GoToPage(2), Blue)
            }
        };
        return PageScroll(content);
    }

    private View BuildEntryPage()
    {
        var content = new VerticalStackLayout
        {
            Padding = new Thickness(18, 14, 18, 24),
            Spacing = 15,
            Children =
            {
                SectionHeader("🟢", "Registrar entrada"),
                new Label { Text = "Ingresa los datos de la moto y asigna un puesto disponible.", FontSize = 14, TextColor = SecondaryText },
                Card(new VerticalStackLayout
                {
                    Spacing = 11,
                    Children =
                    {
                        FieldLabel("Placa de la moto"), _plate,
                        FieldLabel("Nombre del cliente (opcional)"), _customer,
                        FieldLabel("Puesto de estacionamiento"), _spacePicker,
                        ActionButton("✓   CONFIRMAR ENTRADA", RegisterEntryClicked, Green)
                    }
                }),
                new Label { Text = "Consejo: verifica la placa antes de guardar el ingreso.", FontSize = 12, TextColor = MutedText }
            }
        };
        return PageScroll(content);
    }

    private View BuildExitPage()
    {
        var content = new VerticalStackLayout
        {
            Padding = new Thickness(18, 14, 18, 24),
            Spacing = 15,
            Children =
            {
                SectionHeader("🔵", "Salida y cobro"),
                new Label { Text = "Busca la moto por su placa y registra el pago para liberar el puesto.", FontSize = 14, TextColor = SecondaryText },
                Card(new VerticalStackLayout
                {
                    Spacing = 11,
                    Children =
                    {
                        FieldLabel("Placa de la moto"), _exitPlate,
                        FieldLabel("Tarifa por hora (COP)"), _hourlyRate,
                        FieldLabel("Método de pago"), _paymentPicker,
                        ActionButton("＄   REGISTRAR SALIDA Y COBRAR", RegisterExitClicked, Blue)
                    }
                }),
                new Label { Text = "La tarifa inicial está configurada en $3.000 por hora; puedes ajustarla.", FontSize = 12, TextColor = MutedText }
            }
        };
        return PageScroll(content);
    }

    private View BuildManagementPage()
    {
        var content = new VerticalStackLayout
        {
            Padding = new Thickness(18, 14, 18, 24),
            Spacing = 15,
            Children =
            {
                SectionHeader("🏍️", "Motos estacionadas"),
                Card(_activeList),
                SectionHeader("📋", "Últimos movimientos"),
                Card(_history),
                ActionButton("↻   Actualizar información", async (_, _) => await RefreshAsync(), Blue)
            }
        };
        return PageScroll(content);
    }

    private static View PageScroll(View content) => new ScrollView
    {
        Content = content,
        BackgroundColor = PageBackground,
        VerticalScrollBarVisibility = ScrollBarVisibility.Never
    };

    private Button NavigationButton(string symbol, string text, int position) => new()
    {
        Text = $"{symbol}  {text}",
        BackgroundColor = FieldBackground,
        TextColor = MainText,
        FontSize = 12,
        CornerRadius = 12,
        HeightRequest = 46,
        Padding = new Thickness(3, 0),
        Command = new Command(() => GoToPage(position))
    };

    private void GoToPage(int position)
    {
        _carousel.Position = position;
        UpdatePageIndicator(position);
    }

    private void UpdatePageIndicator(int position)
    {
        string[] names = { "Inicio", "Entrada", "Salida", "Control" };
        if (position >= 0 && position < names.Length)
            _pageIndicator.Text = $"{position + 1} de {names.Length} · {names[position]}";
    }

    private void Logout()
    {
        if (Application.Current is not null && Application.Current.Windows.Count > 0)
            Application.Current.Windows[0].Page = new NavigationPage(new LoginPage());
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
        if (!int.TryParse(_spacePicker.SelectedItem?.ToString()?.Replace("Puesto ", ""), out var selectedSpace))
        {
            await DisplayAlertAsync("Falta el puesto", "Selecciona un puesto disponible.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(_plate.Text))
        {
            await DisplayAlertAsync("Falta la placa", "Escribe la placa de la moto.", "OK");
            return;
        }

        var result = await _database.RegisterEntryAsync(
            _plate.Text.Trim(),
            _customer.Text?.Trim() ?? "",
            selectedSpace);

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

        // Solo mostrar puestos libres para evitar asignar accidentalmente uno ocupado.
        var occupiedSpaces = active.Select(v => v.ParkingSpace).ToHashSet();
        var previouslySelectedSpace = _spacePicker.SelectedItem?.ToString();
        _spacePicker.Items.Clear();
        for (int space = 1; space <= Capacity; space++)
        {
            if (!occupiedSpaces.Contains(space))
                _spacePicker.Items.Add($"Puesto {space}");
        }
        if (previouslySelectedSpace is not null)
            _spacePicker.SelectedItem = _spacePicker.Items.FirstOrDefault(x => x == previouslySelectedSpace);

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

    public static T Row<T>(this T view, int row) where T : View
    {
        Grid.SetRow(view, row);
        return view;
    }
}
