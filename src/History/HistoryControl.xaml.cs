using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DbgX.Interfaces.Services;

namespace WinDbgExt.History
{
    public partial class HistoryControl
    {
        private readonly IDbgConsole _console;

        public HistoryControl(IDbgConsole console)
        {
            _console = console;
            InitializeComponent();
        }

        private void ListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = (Tuple<string, string>)HistoryList.SelectedItem;

            var window = new Window();

            var grid = new Grid();

            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var title = new TextBlock { Text = item.Item1 };
            grid.Children.Add(title);

            var content = new TextBox { Text = item.Item2 };
            Grid.SetRow(content, 1);
            grid.Children.Add(content);

            var command = new TextBox { Tag = content };
            Grid.SetRow(command, 2);
            grid.Children.Add(command);

            command.KeyDown += Command_KeyDown;

            window.Content = grid;
            window.Show();
        }

        private async void Command_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                var textBox = (TextBox)sender;

                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    return;
                }

                var content = (TextBox)textBox.Tag;

                content.Text += Environment.NewLine + textBox.Text;

                var command = textBox.Text;
                textBox.Text = string.Empty;
                content.ScrollToEnd();

                var result = await _console.ExecuteCommandAndCaptureOutputAsync(command);
                
                content.Text += Environment.NewLine + CommandHistoryWindow.StripDml(result);
                content.ScrollToEnd();
            }
        }
    }
}
