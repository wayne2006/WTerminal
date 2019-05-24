using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using tterm.Ansi;
using tterm.Utility;

namespace tterm.Terminal
{
    internal class TerminalSession : IDisposable
    {
        private readonly StreamWriter _ptyWriter;
        private readonly object _bufferSync = new object();
        private bool _disposed;

        public event EventHandler TitleChanged;
        public event EventHandler OutputReceived;
        public event EventHandler BufferSizeChanged;
        public event EventHandler Finished;

        public string Title { get; set; }
        public bool Active { get; set; }
        public bool Connected { get; private set; }
        public bool ErrorOccured { get; private set; }
        public Exception Exception { get; private set; }

        public TerminalBuffer Buffer { get; }

        public TerminalSize Size
        {
            get => Buffer.Size;
            set
            {
                if (Buffer.Size != value)
                {
                     Buffer.Size = value;
                    BufferSizeChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public TerminalSession(TerminalSize size, Profile profile)
        {
            Buffer = new TerminalBuffer(size);
            //_ptyWriter = new StreamWriter(_pty.StandardInput, Encoding.UTF8)
            //{
            //    AutoFlush = true
            //};
            //RunOutputLoop();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }


        private Task ConsoleOutputAsync(Stream stream)
        {
            return Task.Run(async delegate
            {
                var ansiParser = new AnsiParser();
                var sr = new StreamReader(stream, Encoding.UTF8);
                do
                {
                    int offset = 0;
                    var buffer = new char[1024];
                    int readChars = await sr.ReadAsync(buffer, offset, buffer.Length - offset);
                    if (readChars > 0)
                    {
                        var reader = new ArrayReader<char>(buffer, 0, readChars);
                        var codes = ansiParser.Parse(reader);
                        ReceiveOutput(codes);

                        //Write("type:" + codes[0].Type.ToString() + " text:" + codes[0].Text + " || ");
                    }
                    //Thread.Sleep(100);
                }
                while (!sr.EndOfStream);
                //while (true);
            });
        }

        public void WriteOutput(string text)
        {
            TerminalCode charAtt = new TerminalCode(TerminalCodeType.CharAttributes, new CharAttributes() { BackgroundColour = 0 });
            TerminalCode t = new TerminalCode(TerminalCodeType.Text, text);
            TerminalCode[] ts = new TerminalCode[] { charAtt,t };
            ReceiveOutput(ts);
        }
        public void WriteLineOutput(string text)
        {
            TerminalCode charAtt = new TerminalCode(TerminalCodeType.CharAttributes, new CharAttributes() { BackgroundColour = 0 });
            TerminalCode t = new TerminalCode(TerminalCodeType.Text, text);
            TerminalCode eraseInLine = new TerminalCode(TerminalCodeType.EraseInLine);
            TerminalCode carriageReturn = new TerminalCode(TerminalCodeType.CarriageReturn);
            TerminalCode lineFeed = new TerminalCode(TerminalCodeType.LineFeed);
            TerminalCode[] ts = new TerminalCode[] { charAtt,t, eraseInLine, carriageReturn, lineFeed };
            ReceiveOutput(ts);
        }

        public void WriteInLinesOutput(List<string> list)
        {
            int count = list.Count;
            List<TerminalCode> ar = new List<TerminalCode>();

            TerminalCode charAtt = new TerminalCode(TerminalCodeType.CharAttributes, new CharAttributes() { BackgroundColour = 0 });
            foreach (var item in list)
            {
                TerminalCode t = new TerminalCode(TerminalCodeType.Text, item);
                TerminalCode carriageReturn = new TerminalCode(TerminalCodeType.CarriageReturn);
                TerminalCode lineFeed = new TerminalCode(TerminalCodeType.LineFeed);
                
                
                ar.Add(t);
                ar.Add(carriageReturn);
                ar.Add(lineFeed);
                
            }

            TerminalCode cursorUp = new TerminalCode(TerminalCodeType.CursorUp,count,0);
            ar.Add(cursorUp);


            TerminalCode[] ts = ar.ToArray();
            ReceiveOutput(ts);
        }
        public void WriteCursorUp(int rows)
        {
            TerminalCode cursorUp = new TerminalCode(TerminalCodeType.CursorUp, rows, 0);

            TerminalCode[] ts = new TerminalCode[] { cursorUp };
            ReceiveOutput(ts);
        }

        public void WriteCursorDown(int rows)
        {
            TerminalCode cursorDown = new TerminalCode(TerminalCodeType.CursorDown, rows, 0);

            TerminalCode[] ts = new TerminalCode[] { cursorDown };
            ReceiveOutput(ts);
        }

        public void Clear()
        {
            TerminalCode t = new TerminalCode(TerminalCodeType.EraseInDisplay);
            TerminalCode[] ts = new TerminalCode[] { t };
            ReceiveOutput(ts);
        }

        private void ReceiveOutput(IEnumerable<TerminalCode> codes)
        {
            lock (_bufferSync)
            {
                foreach (var code in codes)
                {
                    ProcessTerminalCode(code);
                }
            }
            OutputReceived?.Invoke(this, EventArgs.Empty);
        }

        private void ProcessTerminalCode(TerminalCode code)
        {
            switch (code.Type)
            {
                case TerminalCodeType.ResetMode:
                    Buffer.ShowCursor = false;
                    break;
                case TerminalCodeType.SetMode:
                    Buffer.ShowCursor = true;

                    // HACK We want clear to reset the window position but not general typing.
                    //      We therefore reset the window only if the cursor is moved to the top.
                    if (Buffer.CursorY == 0)
                    {
                        Buffer.WindowTop = 0;
                    }
                    break;
                case TerminalCodeType.Text:
                    Buffer.Type(code.Text);
                    break;
                case TerminalCodeType.LineFeed://换行
                    if (Buffer.CursorY == Buffer.Size.Rows -1)
                    {
                        Buffer.ShiftUp();
                    }
                    else
                    {
                        Buffer.CursorY++;
                    }
                    break;
                case TerminalCodeType.CarriageReturn://回车
                    Buffer.CursorX = 0;
                    break;
                case TerminalCodeType.CharAttributes:
                    Buffer.CurrentCharAttributes = code.CharAttributes;
                    break;
                case TerminalCodeType.CursorPosition:
                    Buffer.CursorX = code.Column;
                    Buffer.CursorY = code.Line;
                    break;
                case TerminalCodeType.CursorUp:
                    Buffer.CursorY -= code.Line;
                    break;
                case TerminalCodeType.CursorDown:
                    Buffer.CursorY += code.Line;
                    break;
                case TerminalCodeType.CursorCharAbsolute:
                    Buffer.CursorX = code.Column;
                    break;
                case TerminalCodeType.EraseInLine://行内删除(用空白字符填充)
                    if (code.Line == 0)
                    {
                        Buffer.ClearBlock(Buffer.CursorX, Buffer.CursorY, Buffer.Size.Columns - 1, Buffer.CursorY);
                    }
                    break;
                case TerminalCodeType.EraseInDisplay:
                    Buffer.Clear();
                    Buffer.CursorX = 0;
                    Buffer.CursorY = 0;
                    break;
                case TerminalCodeType.SetTitle:
                    Title = code.Text;
                    TitleChanged?.Invoke(this, EventArgs.Empty);
                    break;
            }
        }

        public void Write(string text)
        {
            //_ptyWriter.Write(text);
            //WriteOutput(text);
        }

        public void Paste()
        {
            string text = Clipboard.GetText();
            if (!String.IsNullOrEmpty(text))
            {
                Write(text);
            }
        }

        private void SessionErrored(Exception ex)
        {
            Connected = false;
            Exception = ex;
            Finished?.Invoke(this, EventArgs.Empty);
        }

        private void SessionClosed()
        {
            Connected = false;
            Finished?.Invoke(this, EventArgs.Empty);
        }
    }
}
