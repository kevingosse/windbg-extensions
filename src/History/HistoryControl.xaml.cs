using System;
using System.Windows.Input;
using DbgX.Interfaces;
using DbgX.Interfaces.Services;

namespace WinDbgExt.History
{
    public partial class HistoryControl
    {
        private readonly IDbgToolWindowManager _toolWindowManager;

        public HistoryControl(IDbgToolWindowManager toolWindowManager)
        {
            _toolWindowManager = toolWindowManager;
            InitializeComponent();
        }

        private void ListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = (Tuple<string, string>)HistoryList.SelectedItem;

            _toolWindowManager.OpenToolWindow("CommandWindow", item.Item2);
        }
    }
}
