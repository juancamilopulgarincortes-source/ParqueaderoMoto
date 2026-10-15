using Microsoft.Maui.Controls.Shapes;

namespace ParqueaderoMoto;

public class LoginPage : ContentPage
{
    private static readonly Color Background = Color.FromArgb("#07121A");
    private static readonly Color CardBackground = Color.FromArgb("#0D1B27");
    private static readonly Color FieldBackground = Color.FromArgb("#142638");
    private static readonly Color MainText = Color.FromArgb("#F3F7FC");
    private static readonly Color SecondaryText = Color.FromArgb("#A8BCD2");
    private static readonly Color MutedText = Color.FromArgb("#7F96AF");
    private static readonly Color Green = Color.FromArgb("#0AA66A");

    private readonly Entry _username = new()
    {
        Placeholder = "Nombre de usuario",
        Text = "admin",
        TextColor = MainText,
        PlaceholderColor = MutedText,
        BackgroundColor = FieldBackground,
        HeightRequest = 52,
        ClearButtonVisibility = ClearButtonVisibility.WhileEditing,
        ReturnType = ReturnType.Next
    };

    private readonly Entry _password = new()
    {
        Placeholder = "Contraseña",
        TextColor = MainText,
        PlaceholderColor = MutedText,
        BackgroundColor = FieldBackground,
        HeightRequest = 52,
        IsPassword = true,
        ReturnType = ReturnType.Go
    };

    private readonly Label _error = new()
    {
        TextColor = Color.FromArgb("#FF7777"),
        FontSize = 13,
        IsVisible = false
    };

    public LoginPage()
    {
        Title = "Iniciar sesión";
        BackgroundColor = Background;
        NavigationPage.SetHasNavigationBar(this, false);

        _password.Completed += async (_, _) => await SignInAsync();

        var logo = new Border
        {
            BackgroundColor = Color.FromArgb("#12382F"),
            Stroke = Color.FromArgb("#1D7558"),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 28 },
            Padding = 22,
            HorizontalOptions = LayoutOptions.Center,
            Content = new Label { Text = "🏍️", FontSize = 48, HorizontalTextAlignment = TextAlignment.Center }
        };

        var loginCard = new Border
        {
            BackgroundColor = CardBackground,
            Stroke = Color.FromArgb("#263D50"),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 22 },
            Padding = 22,
            Content = new VerticalStackLayout
            {
                Spacing = 12,
                Children =
                {
                    new Label { Text = "Iniciar sesión", FontSize = 23, FontAttributes = FontAttributes.Bold, TextColor = MainText },
                    new Label { Text = "Ingresa para administrar tu parqueadero.", FontSize = 13, TextColor = SecondaryText },
                    new Label { Text = "USUARIO", FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = SecondaryText, Margin = new Thickness(0, 8, 0, -5) },
                    _username,
                    new Label { Text = "CONTRASEÑA", FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = SecondaryText, Margin = new Thickness(0, 5, 0, -5) },
                    _password,
                    _error,
                    new Button
                    {
                        Text = "ENTRAR  →",
                        BackgroundColor = Green,
                        TextColor = Colors.White,
                        FontAttributes = FontAttributes.Bold,
                        CornerRadius = 13,
                        HeightRequest = 54,
                        Margin = new Thickness(0, 8, 0, 0),
                        Command = new Command(async () => await SignInAsync())
                    }
                }
            }
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(24, 48, 24, 30),
                Spacing = 20,
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    logo,
                    new Label { Text = "MOTO PARK", FontSize = 29, FontAttributes = FontAttributes.Bold, TextColor = MainText, HorizontalTextAlignment = TextAlignment.Center },
                    new Label { Text = "Tu parqueadero, bajo control.", FontSize = 14, TextColor = SecondaryText, HorizontalTextAlignment = TextAlignment.Center, Margin = new Thickness(0, -17, 0, 4) },
                    loginCard,
                    new Label { Text = "Acceso inicial de prueba: admin / 1234", FontSize = 12, TextColor = MutedText, HorizontalTextAlignment = TextAlignment.Center },
                    new Label { Text = "Parqueadero Moto · Administración", FontSize = 11, TextColor = MutedText, HorizontalTextAlignment = TextAlignment.Center }
                }
            },
            BackgroundColor = Background
        };
    }

    private async Task SignInAsync()
    {
        var username = _username.Text?.Trim();
        var password = _password.Text;

        // Credenciales de demostración. Para producción deben reemplazarse por autenticación segura.
        if (username == "admin" && password == "1234")
        {
            if (Application.Current is not null && Application.Current.Windows.Count > 0)
                Application.Current.Windows[0].Page = new NavigationPage(new MainPage());
            return;
        }

        _error.Text = "Usuario o contraseña incorrectos. Intenta de nuevo.";
        _error.IsVisible = true;
        await Task.CompletedTask;
    }
}
