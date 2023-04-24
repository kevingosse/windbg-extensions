namespace WinDbgExt.RunCSharp
{
    public partial class OpenEditorButton
    {
        public OpenEditorButton(OpenEditorViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}
