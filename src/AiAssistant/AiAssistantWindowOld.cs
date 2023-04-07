using DbgX.Interfaces.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using DbgX.Interfaces.Services;
using WinDbgExt.History;
using System.Data.Common;
using System.Text.RegularExpressions;
using System.Windows.Documents;
using System.Windows.Threading;
using System.Windows.Input;

namespace WinDbgExt.AiAssistant
{
    public class AiAssistantWindowOld : ToolWindowView
    {
        private TextBox _commandTextBox;
        private TextBox _goalTextBox;
        private readonly AiAssistantWindowViewModel _viewModel;

        private RichTextBox _contentTextBox;
        private readonly IDbgConsole _console;

        private TextBlock _loading;
        private Paragraph _loadingParagraph;

        public AiAssistantWindowOld(AiAssistantWindowViewModel viewModel, IDbgConsole console)
        {
            _console = console;
            _viewModel = viewModel;
            DataContext = _viewModel;

            viewModel.NewCompletion += NewCompletion;
            viewModel.Loading += Loading;

            var grid = new Grid();

            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition());

            var stackPanel = new StackPanel { Orientation = Orientation.Vertical };

            stackPanel.Children.Add(new TextBlock { Text = "Problem statement:" });

            _goalTextBox = new();
            _goalTextBox.KeyDown += GoalTextBox_KeyDown;

            stackPanel.Children.Add(_goalTextBox);

            grid.Children.Add(stackPanel);
            Grid.SetRow(stackPanel, 0);

            //_loading = new TextBlock()
            //{
            //    Text = "Thinking...",
            //    FontStyle = FontStyles.Italic,
            //    Visibility = Visibility.Hidden
            //};

            //grid.Children.Add(_loading);
            //Grid.SetRow(_loading, 1);

            _contentTextBox = new()
            {
                Margin = new Thickness(0, 5, 0, 0),
                IsDocumentEnabled = true,
                IsReadOnly = true
            };

            grid.Children.Add(_contentTextBox);
            Grid.SetRow(_contentTextBox, 2);

            Content = grid;
        }

        private void Loading(bool isLoading)
        {
            //if (_loading != null)
            //{
            //    _loading.Visibility = isLoading ? Visibility.Visible : Visibility.Hidden;
            //}

            if (isLoading)
            {
                var paragraph = new Paragraph();

                _contentTextBox.Document.Blocks.Add(paragraph);

                paragraph.Inlines.Add(new Run("Thinking...") { FontStyle = FontStyles.Italic });
            }
            else if (_loadingParagraph != null)
            {
                _contentTextBox.Document.Blocks.Remove(_loadingParagraph);
                _loadingParagraph = null;
            }
        }

        private void NewCompletion(string completion)
        {
            AppendOutput(completion);
        }

        private void GoalTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                _viewModel.UpdatePrompt(_goalTextBox.Text);
            }
        }

        private async Task ExecuteCommand(string command)
        {
            await _console.ExecuteCommandAsync(command);
        }

        private void AppendOutput(string content)
        {
            var paragraph = new Paragraph();

            _contentTextBox.Document.Blocks.Add(paragraph);

            var matches = Regex.Matches(content, @"<exec cmd=\""(?<command>.+?)\"">(?<label>.+?)</exec>");

            int index = 0;

            foreach (Match match in matches)
            {
                paragraph.Inlines.Add(new Run(System.Net.WebUtility.HtmlDecode(content.Substring(index, match.Index - index))));

                var hyperLink = new Hyperlink(new Run(match.Groups["label"].Value));

                var command = match.Groups["command"].Value;

                hyperLink.Command = new DmlCommand(() => _ = ExecuteCommand(command));

                paragraph.Inlines.Add(hyperLink);

                index = match.Index + match.Length;
            }

            if (index < content.Length)
            {
                paragraph.Inlines.Add(new Run(content.Substring(index)));
            }

            _contentTextBox.ScrollToEnd();
        }

        public class DmlCommand : ICommand
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
