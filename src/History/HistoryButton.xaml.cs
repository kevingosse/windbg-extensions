
namespace WinDbgExt.History
{
    /// <summary>
    /// Interaction logic for HistoryButton.xaml
    /// </summary>
    public partial class HistoryButton
    {
        public HistoryButton(HistoryViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}
