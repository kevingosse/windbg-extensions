using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using DbgX.Interfaces;
using DbgX.Interfaces.Listeners;
using DbgX.Interfaces.Services;
using DbgX.Util;

namespace WinDbgExt.History
{
    public interface IHistoryManager
    {
        void LogCommand(string command, string output);
    }

    [NamedPartMetadata("CommandHistoryWindow", 0), Export(typeof(IDbgToolWindow))]
    [Export(typeof(IDbgDmlOutputListener))]
    [Export(typeof(IDbgCommandExecutionListener))]
    [Export(typeof(IDbgEngineStatusListener))]
    [Export(typeof(IHistoryManager))]
    public class CommandHistoryWindow : BindableBase, IDbgToolWindow, IDbgDmlOutputListener, IDbgCommandExecutionListener, IDbgEngineStatusListener, IHistoryManager
    {
        private StringBuilder _output = new StringBuilder();
        private string _currentCommand;

        [Import]
        private IDbgConsole _console;

        [Import]
        private IDbgToolWindowManager _toolWindowManager;

        public CommandHistoryWindow()
        {
            History = new ObservableCollection<Tuple<string, string>>();
        }

        public Control GetToolWindowView(object parameter)
        {
            return new ContentControl
            {
                DataContext = this,
                Content = new HistoryControl(_toolWindowManager) { DataContext = this }
            };
        }

        public ObservableCollection<Tuple<string, string>> History { get; }

        public void OnDmlOutput(string text)
        {
            _output.Append(text);
        }

        public void OnCommandExecuted(string command)
        {
            _output.Clear();
            _currentCommand = command;
        }

        public void OnEngineStatusChanged(bool busy)
        {
            if (!busy && _currentCommand != null)
            {
                var output = StripDml(_output.ToString());

                History.Add(Tuple.Create(_currentCommand, output));
            }
        }

        public static string StripDml(string output)
        {
            return output;
            //return Regex.Replace(output, @"<exec[^>]*>", string.Empty)
            //    .Replace("</exec>", string.Empty);
        }

        public void LogCommand(string command, string output)
        {
            History.Add(Tuple.Create(command, output));
        }
    }
}