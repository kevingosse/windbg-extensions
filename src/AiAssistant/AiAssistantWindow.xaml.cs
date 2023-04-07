using DbgX.Interfaces.Services;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using WinDbgExt.History;

namespace WinDbgExt.AiAssistant
{
    public partial class AiAssistantWindow
    {
        private readonly AiAssistantWindowViewModel _viewModel;
        private readonly IDbgConsole _console;
        private Paragraph _loadingParagraph;

        public AiAssistantWindow(AiAssistantWindowViewModel viewModel, IDbgConsole console)
        {
            _console = console;

            _viewModel = viewModel;
            DataContext = viewModel;

            viewModel.NewCompletion += NewCompletion;
            viewModel.Loading += Loading;

            InitializeComponent();
        }

        private void GoalTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                _viewModel.UpdatePrompt(((TextBox)sender).Text);
            }
        }

        private void Loading(bool isLoading)
        {
            if (isLoading)
            {
                var paragraph = new Paragraph();

                AiOutputTextBox.Document.Blocks.Add(paragraph);

                paragraph.Inlines.Add(new Run("Thinking...") { FontStyle = FontStyles.Italic });
            }
            else if (_loadingParagraph != null)
            {
                AiOutputTextBox.Document.Blocks.Remove(_loadingParagraph);
                _loadingParagraph = null;
            }
        }

        private void NewCompletion(string completion)
        {
            var paragraph = new Paragraph();

            AiOutputTextBox.Document.Blocks.Add(paragraph);

            var matches = Regex.Matches(completion, @"<exec cmd=\""(?<command>.+?)\"">(?<label>.+?)</exec>");

            int index = 0;

            foreach (Match match in matches)
            {
                paragraph.Inlines.Add(new Run(System.Net.WebUtility.HtmlDecode(completion.Substring(index, match.Index - index))));

                var hyperLink = new Hyperlink(new Run(match.Groups["label"].Value));

                var command = match.Groups["command"].Value;

                hyperLink.Command = new DmlCommand(() => _ = _console.ExecuteCommandAsync(command));

                paragraph.Inlines.Add(hyperLink);

                index = match.Index + match.Length;
            }

            if (index < completion.Length)
            {
                paragraph.Inlines.Add(new Run(completion.Substring(index)));
            }

            AiOutputTextBox.ScrollToEnd();
        }
        
        private class DmlCommand : ICommand
        {
            private readonly Action _command;

            public DmlCommand(Action command)
            {
                _command = command;
            }

            public event EventHandler CanExecuteChanged;

            public bool CanExecute(object parameter) => true;

            public void Execute(object parameter) => _command();
        }
    }
}
