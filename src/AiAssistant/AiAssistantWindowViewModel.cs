using System.ComponentModel.Composition;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Threading;
using AI.Dev.OpenAI.GPT;
using DbgX.Interfaces;
using DbgX.Interfaces.Listeners;
using DbgX.Interfaces.Logging;
using DbgX.Interfaces.Services;
using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Models;

namespace WinDbgExt.AiAssistant
{
    [Export(typeof(IDbgDmlOutputListener))]
    [NamedPartMetadata("AiAssistant"), Export(typeof(IDbgToolWindow))]
    public class AiAssistantWindowViewModel : IDbgToolWindow, IDbgDmlOutputListener
    {
        private readonly StringBuilder _output = new();
        private readonly List<ChatMessage> _conversation = new();

        private const string PromptTemplate = """
            You are a debugging assistant, integrated to Windbg. The user is debugging a .NET application. SOS is already loaded.

            Commands that the user execute are forwarded to you. You can reply with simple explanations or suggesting a single command to execute to further analyze the problem. Only suggest one command at a time!

            When you suggest a command to execute, use the DML format to transform them into hyperlinks: <exec cmd="command">label</exec>.

            The high level description of the problem provided by the user is:
            <description>
            """;

        [Import]
        private IDbgConsole _console;

        [Import]
        private IDbgLogger _logger;

        private readonly OpenAIAPI _api;
        private string _prompt;
        private int _promptTokens;

        public AiAssistantWindowViewModel()
        {
            _api = OpenAIAPI.ForAzure("RESOURCE-NAME", "gpt-4", new APIAuthentication("API_KEY"));
            _api.ApiVersion = "2023-03-15-preview";
        }

        public event Action<string> NewCompletion;
        public event Action<bool> Loading;

        public FrameworkElement GetToolWindowView(object parameter) => new AiAssistantWindow(this, _console);

        public void OnDmlOutput(string text)
        {
            _output.Append(text);
        }

        public void OnCommandCompletion()
        {
            var output = Regex.Replace(_output.ToString(), "<.*?>", String.Empty);
            _output.Clear();

            _ = SendCommand(output);
        }

        public void UpdatePrompt(string description)
        {
            _prompt = PromptTemplate.Replace("<description>", description);
            _promptTokens = GPT3Tokenizer.Encode(_prompt).Count;

            _ = SendCommand(null);
        }

        private async Task SendCommand(string text)
        {
            if (_prompt == null)
            {
                return;
            }

            Loading?.Invoke(true);

            await Dispatcher.Yield();

            try
            {
                if (text != null)
                {
                    _conversation.Add(new ChatMessage(ChatMessageRole.User, text));
                }

                int tokenLimit = 1024 * 8;

                int tokenCount;

                do
                {
                    tokenCount = _promptTokens + _conversation.Sum(message => GPT3Tokenizer.Encode(message.Content).Count);

                    if (tokenCount > tokenLimit && _conversation.Count > 0)
                    {
                        _conversation.RemoveAt(0);
                    }
                } while (tokenCount > tokenLimit);

                var actualConversation = new List<ChatMessage>
                {
                    new(ChatMessageRole.System, _prompt)
                };

                actualConversation.AddRange(_conversation);

                var request = new ChatRequest
                {
                    Messages = actualConversation,
                    Model = Model.GPT4,
                    //Temperature = 0
                };

                var sb = new StringBuilder();

                foreach (var message in actualConversation)
                {
                    sb.Append(message.Role + ": " + message.Content);
                }

                _logger.Log("AI", Environment.NewLine + Environment.NewLine + "-----------------" + Environment.NewLine + sb);

                var result = await _api.Chat.CreateChatCompletionAsync(request);

                NewCompletion?.Invoke(result.Choices[0].Message.Content);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            Loading?.Invoke(false);
        }
    }
}