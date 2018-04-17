using System.Windows.Input;

namespace WinDbgExt.RunCSharp
{
    public partial class EditorControl
    {
        private CSharpScriptWindow _viewModel;

        public EditorControl()
        {
            DataContextChanged += (_, e) => _viewModel = e.NewValue as CSharpScriptWindow;
            InitializeComponent();
            TextEditor.TextArea.MouseWheel += TextArea_MouseWheel;
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