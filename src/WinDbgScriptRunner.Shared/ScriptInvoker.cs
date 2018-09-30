using System;

namespace WindbgScriptRunner
{
    [Serializable]
    public class ScriptInvoker
    {
        private readonly IntPtr _client;
        private readonly string _args;

        public ScriptInvoker(IntPtr client, string args)
        {
            _client = client;
            _args = args;
        }

        public void Invoke()
        {
            ScriptRunner.Run(_client, _args);
        }
    }
}