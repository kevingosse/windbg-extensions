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
