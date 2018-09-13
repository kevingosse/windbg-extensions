namespace WinDbgExt.RunCSharp
{
    /// <summary>
    /// Interaction logic for OpenEditorButton.xaml
    /// </summary>
    public partial class OpenEditorButton
    {
        public OpenEditorButton(OpenEditorViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}
