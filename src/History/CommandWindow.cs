using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using DbgX.Interfaces;
using DbgX.Interfaces.Services;

namespace WinDbgExt.History
{
    [NamedPartMetadata("CommandWindow"), Export(typeof(IDbgToolWindow))]
    public class CommandWindow : IDbgToolWindow
    {
        [Import]
        private IDbgConsole _console;

        [Import]
        private IHistoryManager _historyManager;

        public FrameworkElement GetToolWindowView(object parameter)
        {
            return new ContentControl { Content = new CommandControl(_console, _historyManager, (string)parameter) };
        }
    }
}
