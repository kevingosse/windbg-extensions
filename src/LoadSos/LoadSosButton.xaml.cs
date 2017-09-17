namespace WinDbgExt.LoadSos
{
    /// <summary>
    /// Interaction logic for LoadSosButton.xaml
    /// </summary>
    public partial class LoadSosButton
    {
        public LoadSosButton(ToggleSosViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}
