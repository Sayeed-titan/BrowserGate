using System.Windows;
using System.Windows.Controls;

namespace BrowserGate.Services;

/// <summary>
/// Placeholder ("hint") text for TextBox and PasswordBox via an attached
/// property. Usage in XAML:
///
///     xmlns:s="clr-namespace:BrowserGate.Services"
///     <TextBox s:Hint.Text="Recovery Gmail address"/>
///     <PasswordBox s:Hint.Text="Choose a master password"/>
///
/// Internally tracks whether the control has content (Text or Password)
/// in the HasContent attached property. The TextBox / PasswordBox templates
/// in App.xaml watch HasContent + the Hint.Text via TemplateBinding-style
/// references and toggle a placeholder TextBlock's visibility.
/// </summary>
public static class Hint
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.RegisterAttached(
            "Text", typeof(string), typeof(Hint),
            new PropertyMetadata("", OnTextChanged));

    public static readonly DependencyProperty HasContentProperty =
        DependencyProperty.RegisterAttached(
            "HasContent", typeof(bool), typeof(Hint),
            new PropertyMetadata(false));

    public static string GetText(DependencyObject d) => (string)d.GetValue(TextProperty);
    public static void SetText(DependencyObject d, string v) => d.SetValue(TextProperty, v);

    public static bool GetHasContent(DependencyObject d) => (bool)d.GetValue(HasContentProperty);
    public static void SetHasContent(DependencyObject d, bool v) => d.SetValue(HasContentProperty, v);

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        switch (d)
        {
            case PasswordBox pb:
                pb.PasswordChanged -= OnPasswordChanged;
                pb.PasswordChanged += OnPasswordChanged;
                SetHasContent(pb, pb.Password.Length > 0);
                break;
            case TextBox tb:
                tb.TextChanged -= OnTextBoxChanged;
                tb.TextChanged += OnTextBoxChanged;
                SetHasContent(tb, tb.Text.Length > 0);
                break;
        }
    }

    private static void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox pb) SetHasContent(pb, pb.Password.Length > 0);
    }

    private static void OnTextBoxChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox tb) SetHasContent(tb, tb.Text.Length > 0);
    }
}
