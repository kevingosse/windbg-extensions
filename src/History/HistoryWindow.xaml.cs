using System;
using System.Windows;
using System.Windows.Controls;

namespace WinDbgExt.History
{
    public partial class HistoryWindow
    {
        public HistoryWindow()
        {
            InitializeComponent();
        }

        private void HistoryList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var listBox = (ListBox)sender;

            var viewModel = (HistoryWindowViewModel)listBox.DataContext;

            var selectedItem = (Tuple<string, string>)listBox.SelectedItem;

            viewModel.OpenCommand.Execute(selectedItem.Item2);
        }
    }
}
