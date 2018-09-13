using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using DbgX.Interfaces.Services;
using ICSharpCode.AvalonEdit.Highlighting;

namespace WinDbgExt.RunCSharp
{
    public partial class CSharpScriptWindow
    {
        private readonly CSharpScriptWindowViewModel _viewModel;
        private IDbgThemeService _themeService;

        public CSharpScriptWindow(CSharpScriptWindowViewModel viewModel, IDbgThemeService themeService)
        {
            _viewModel = viewModel;
            _themeService = themeService;

            DataContext = _viewModel;

            _themeService.CurrentThemeChanged.Subscribe(this, t => t.ThemeChanged);
            
            InitializeComponent();
            TextEditor.TextArea.MouseWheel += TextArea_MouseWheel;

            ApplyTheme();
        }

        private void ThemeChanged(object sender, CurrentThemeChangedEventArgs e)
        {
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            TextEditor.Foreground = new SolidColorBrush(_themeService.GetColor("ControlText"));
            TextEditor.Background = new SolidColorBrush(_themeService.GetColor("Control"));
            TextEditor.TextArea.SelectionBrush = new SolidColorBrush(_themeService.GetColor("TextEditor.ActiveTextSelection"));
            TextEditor.TextArea.SelectionForeground = new SolidColorBrush(_themeService.GetColor("ControlText"));
        }

        private void TextArea_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                return;
            }

            var fontSize = TextEditor.FontSize;

            fontSize += e.Delta > 0 ? 1 : -1;

            if (fontSize < 8)
            {
                fontSize = 8;
            }

            TextEditor.FontSize = fontSize;
        }

        private void TextEditor_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                _viewModel?.RunCommand.Execute();
            }
        }
    }
}