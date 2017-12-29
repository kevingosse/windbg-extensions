namespace WinDbgExt.History
{
    public interface IHistoryManager
    {
        void LogCommand(string command, string output);
    }
}