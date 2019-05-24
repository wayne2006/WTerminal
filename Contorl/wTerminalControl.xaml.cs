using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using tterm;
using tterm.Terminal;

namespace wContorl
{
    /// <summary>
    /// wTerminalControl.xaml 的交互逻辑
    /// </summary>
    public partial class wTerminalControl : UserControl
    {
        private const int MinColumns = 52;
        private const int MinRows = 4;
        private const int ReadyDelay = 1000;
        private ConfigurationService _configService = new ConfigurationService();
        private int _tickInitialised;
        private bool _ready;
        private Size _consoleSizeDelta;

        private TerminalSession _currentSession;
        private TerminalSize _terminalSize;

        protected DpiScale Dpi => VisualTreeHelper.GetDpi(this);
        public Size Size
        {
            get => RenderSize;
            set
            {
                if (value != RenderSize)
                {
                    var dpi = Dpi;
                    int width = (int)(value.Width * dpi.DpiScaleX);
                    int height = (int)(value.Height * dpi.DpiScaleY);
                }
            }
        }
        public wTerminalControl()
        {
            InitializeComponent();

            var config = _configService.Load();
            if (config.AllowTransparancy)
            {
                //AllowsTransparency = true;
            }
        }

        public void Clear()
        {
            _currentSession.Clear();

        }
        public void Write(string text)
        {
            _currentSession.WriteOutput(text);
        }

        public void WriteLine(string text)
        {
            _currentSession.WriteLineOutput(text);
        }

        public void WriteInLines(List<string> text)
        {
            _currentSession.WriteInLinesOutput(text);
        }
        public void WriteCursorUp(int rows)
        {
            _currentSession.WriteCursorUp(rows);
        }
        public void WriteCursorDown(int rows)
        {
            _currentSession.WriteCursorDown(rows);
        }
        public override void EndInit()
        {
            base.EndInit();

            var config = _configService.Config;

            int columns = Math.Max(config.Columns, MinColumns);
            int rows = Math.Max(config.Rows, MinRows);
            _terminalSize = new TerminalSize(columns, rows);

            Profile profile = config.Profile;
            if (profile == null)
            {
                profile = new Profile()
                {
                    Arguments = null,
                    Command = "",
                    CurrentWorkingDirectory = "",
                    EnvironmentVariables = null
                };
            }

            var session = new TerminalSession(_terminalSize, profile);
            _currentSession = session;
            terminalControl.Session = session;
            terminalControl.Focus();

        }

        private void terminalControl_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                {

                }
                else
                {
                    var terminal = terminalControl;
                    const double FontSizeDelta = 2;
                    double fontSize = terminal.FontSize;
                    if (e.Delta > 0)
                    {
                        if (fontSize < 54)
                        {
                            fontSize += FontSizeDelta;
                        }
                    }
                    else
                    {
                        if (fontSize > 8)
                        {
                            fontSize -= FontSizeDelta;
                        }
                    }
                    if (terminal.FontSize != fontSize)
                    {
                        terminal.FontSize = fontSize;

                        FixTerminalSize();
                    }
                    e.Handled = true;
                }
            }
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            FixTerminalSize();
        }

        private void FixTerminalSize()
        {
            var size = GetBufferSizeForWindowSize(Size);
            SetTermialSize(size);
        }

        private TerminalSize GetBufferSizeForWindowSize(Size size)
        {
            Size charSize = terminalControl.CharSize;
            Size newConsoleSize = new Size(Math.Max(size.Width - _consoleSizeDelta.Width, 0),
                                           Math.Max(size.Height - _consoleSizeDelta.Height, 0));

            int columns = (int)Math.Floor(newConsoleSize.Width / charSize.Width);
            int rows = (int)Math.Floor(newConsoleSize.Height / charSize.Height);

            columns = Math.Max(columns, MinColumns);
            rows = Math.Max(rows, MinRows);

            return new TerminalSize(columns, rows);
        }

        private void SetTermialSize(TerminalSize size)
        {
            if (_terminalSize != size)
            {
                _terminalSize = size;
                if (_currentSession != null)
                {
                    _currentSession.Size = size;
                }

                if (Ready)
                {
                    // Save configuration
                    _configService.Config.Columns = size.Columns;
                    _configService.Config.Rows = size.Rows;
                    _configService.Save();

                }
            }
        }
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            _tickInitialised = Environment.TickCount;
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            Size result = base.ArrangeOverride(arrangeBounds);
            _consoleSizeDelta = new Size(Math.Max(arrangeBounds.Width - terminalControl.ActualWidth, 0),
                                         Math.Max(arrangeBounds.Height - terminalControl.ActualHeight, 0));
            return result;
        }

        public bool Ready
        {
            get
            {
                // HACK Try and find a more reliable way to check if we are ready.
                //      This is to prevent the resize hint from showing at startup.
                if (!_ready)
                {
                    _ready = Environment.TickCount > _tickInitialised + ReadyDelay;
                }
                return _ready;
            }
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            var modifiers = Keyboard.Modifiers;
            if (modifiers.HasFlag(ModifierKeys.Control))
            {
                switch (e.Key)
                {
                    case Key.W:
                        {
                            if (_currentSession != null)
                            {
                                CloseSession(_currentSession);
                            }
                            e.Handled = true;
                            break;
                        }
                    case Key.X:
                        {
                            if (_currentSession != null)
                            {
                                //for (int i = 0; i < 1000; i++)
                                //{
                                //    //_currentSession.Write(i + " Test\r\n");
                                //    //_currentSession.Buffer.Type(i + " Test\r\n");
                                //    _currentSession.WriteOutput(i + " Test\r\n");

                                //}
                                _currentSession.WriteOutput("Test");
                                _currentSession.WriteLineOutput("Test");
                            }
                            e.Handled = true;
                            break;
                        }
                }
            }
            base.OnPreviewKeyDown(e);
        }

        private void CloseSession(TerminalSession session)
        {
            session.Dispose();
        }
    }
}
