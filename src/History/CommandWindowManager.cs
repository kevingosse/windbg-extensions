using System.ComponentModel.Composition;
using System.Windows.Controls;
using DbgX.Interfaces;
using DbgX.Interfaces.Services;

namespace WinDbgExt.History
{
    [NamedPartMetadata("CommandWindow", 0), Export(typeof(IDbgToolWindow))]
    public class CommandWindowManager : IDbgToolWindow
    {
        [Import]
        private IDbgConsole _console;

        [Import]
        private IHistoryManager _historyManager;

        public Control GetToolWindowView(object parameter)
        {
            return new ContentControl { Content = new CommandControl(_console, _historyManager, (string)parameter) };
        }
    }
}
