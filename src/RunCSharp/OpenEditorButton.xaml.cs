namespace WinDbgExt.RunCSharp
{
    /// <summary>
    /// Interaction logic for OpenEditorButton.xaml
    /// </summary>
    public partial class OpenEditorButton
    {
        public OpenEditorButton(EditorViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}
