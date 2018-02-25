using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Diagnostics.Runtime.Interop;

namespace WindbgScriptRunner
{
    internal class DebugEngineStream : Stream
    {
        private readonly IDebugClient _client;
        private readonly IDebugControl _control;

        public DebugEngineStream(IDebugClient client)
        {
            _client = client;
            _control = (IDebugControl)client;
        }

        public void Clear()
        {
            while (Marshal.ReleaseComObject(_client) > 0) { }
            while (Marshal.ReleaseComObject(_control) > 0) { }
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override void Flush()
        {
        }

        public override long Length => -1;

        public override long Position
        {
            get => 0;
            set { }
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotImplementedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();

        public override void SetLength(long value) => throw new NotImplementedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            string str = Encoding.UTF8.GetString(buffer, offset, count);
            _control.ControlledOutput(DEBUG_OUTCTL.ALL_CLIENTS | DEBUG_OUTCTL.DML, DEBUG_OUTPUT.NORMAL, str);
        }
    }
}