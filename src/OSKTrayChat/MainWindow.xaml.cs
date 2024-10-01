using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using Markdig;
using Microsoft.Extensions.Logging;
using OSKTrayChat.Agents;
using OSKTrayChat.ViewModels;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Xml;
using Application = System.Windows.Application;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using Point = System.Drawing.Point;

namespace OSKTrayChat
{
    public partial class MainWindow : Window
    {
        private NotifyIcon notifyIcon;
        private ContextMenuStrip contextMenu;
        private bool isClosing = false;
        private Point trayIconPosition;
        private bool isAnimating = false;
        private MarkdownPipeline markdownPipeline;
        private ILogger<MainWindow> _logger;
        private ITestCaseWritingAgent _tcWritingAgent;

        private ChatViewModel ViewModel => (ChatViewModel)this.DataContext;

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        public MainWindow(ILogger<MainWindow> logger, ITestCaseWritingAgent tcWritingAgent)
        {
            _logger = logger;
            _tcWritingAgent = tcWritingAgent;

            this.Loaded += MainWindow_Loaded;

            InitializeComponent();

            CreateTrayIcon();

            Closing += MainWindow_Closing;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            Opacity = 0;
            Hide();

            InputBox.PreviewKeyDown += InputBox_PreviewKeyDown;

            trayIconPosition = GetTrayIconPosition();

            SetupMarkdownEditor();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.ViewModel.Init();
        }

        private void CreateTrayIcon()
        {
            notifyIcon = new NotifyIcon
            {
                Icon = new Icon(SystemIcons.Application, 40, 40),
                Visible = true
            };

            contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Show/Hide", null, ToggleWindowVisibility);
            contextMenu.Items.Add("Exit", null, Exit);

            notifyIcon.ContextMenuStrip = contextMenu;
            notifyIcon.Click += ToggleWindowVisibility;
        }

        private IntPtr FindTrayIconHandle()
        {
            IntPtr hWnd = FindWindow("Shell_TrayWnd", null);
            hWnd = FindWindowEx(hWnd, IntPtr.Zero, "TrayNotifyWnd", null);
            return FindWindowEx(hWnd, IntPtr.Zero, "SysPager", null);
        }

        private Point GetTrayIconPosition()
        {
            var trayIconHandle = FindTrayIconHandle();
            if (trayIconHandle != IntPtr.Zero && GetWindowRect(trayIconHandle, out RECT iconRect))
            {
                return new Point((iconRect.Left + iconRect.Right) / 2, iconRect.Top);
            }
            else
            {
                // Fallback to screen corner if we can't find the tray icon
                var screen = Screen.PrimaryScreen;
                if (screen != null)
                {
                    return new Point(screen.WorkingArea.Right, screen.WorkingArea.Bottom);
                }
            }
            return new Point(0, 0); // Fallback if all else fails
        }

        private void PositionWindowAboveTrayIcon()
        {
            IntPtr trayHandle = FindTrayIconHandle();
            if (trayHandle != IntPtr.Zero && GetWindowRect(trayHandle, out RECT trayRect))
            {
                var screen = Screen.FromPoint(new Point(trayRect.Left, trayRect.Top));

                // Calculate the position
                double left = trayRect.Left + (trayRect.Right - trayRect.Left) / 2 - Width / 2;
                double top = trayRect.Top - Height - 20;

                // Ensure the window stays within the screen bounds
                if (left < screen.WorkingArea.Left)
                    left = screen.WorkingArea.Left;
                else if (left + Width > screen.WorkingArea.Right)
                    left = screen.WorkingArea.Right - Width;

                if (top < screen.WorkingArea.Top)
                    top = screen.WorkingArea.Top;

                Left = left;
                Top = top;
            }
            else
            {
                // Fallback positioning if we can't find the notification area
                var screen = Screen.PrimaryScreen;
                Left = screen.WorkingArea.Right - Width - 10;
                Top = screen.WorkingArea.Bottom - Height - 10;
            }
        }

        private void ShowWithAnimation()
        {
            if (isAnimating) return;
            isAnimating = true;

            PositionWindowAboveTrayIcon();
            this.ContentContainer.Margin = new Thickness(0, ActualHeight, 0, -ActualHeight);
            Show();
            Activate();

            var storyboard = new Storyboard();

            var fadeInAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(200)
            };
            Storyboard.SetTarget(fadeInAnimation, this);
            Storyboard.SetTargetProperty(fadeInAnimation, new PropertyPath(OpacityProperty));

            var slideUpAnimation = new ThicknessAnimation
            {
                From = new Thickness(0, ActualHeight, 0, -ActualHeight),
                To = new Thickness(0),
                Duration = TimeSpan.FromMilliseconds(200)
            };
            Storyboard.SetTarget(slideUpAnimation, ContentContainer);
            Storyboard.SetTargetProperty(slideUpAnimation, new PropertyPath(FrameworkElement.MarginProperty));

            storyboard.Children.Add(fadeInAnimation);
            storyboard.Children.Add(slideUpAnimation);

            storyboard.Completed += (s, e) =>
            {
                isAnimating = false;
            };

            storyboard.Begin();
        }

        private void HideWithAnimation()
        {
            if (isAnimating) return;
            isAnimating = true;

            var storyboard = new Storyboard();

            var fadeOutAnimation = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(200)
            };
            Storyboard.SetTarget(fadeOutAnimation, this);
            Storyboard.SetTargetProperty(fadeOutAnimation, new PropertyPath(OpacityProperty));

            var slideDownAnimation = new ThicknessAnimation
            {
                From = new Thickness(0),
                To = new Thickness(0, ActualHeight, 0, -ActualHeight),
                Duration = TimeSpan.FromMilliseconds(200)
            };
            Storyboard.SetTarget(slideDownAnimation, ContentContainer);
            Storyboard.SetTargetProperty(slideDownAnimation, new PropertyPath(FrameworkElement.MarginProperty));

            storyboard.Children.Add(fadeOutAnimation);
            storyboard.Children.Add(slideDownAnimation);

            storyboard.Completed += (s, e) =>
            {
                if (!isClosing) Hide();
                ContentContainer.Margin = new Thickness(0);
                isAnimating = false;
            };

            storyboard.Begin();
        }

        private void ToggleWindowVisibility(object? sender, EventArgs e)
        {
            if (IsVisible)
            {
                HideWithAnimation();
            }
            else
            {
                ShowWithAnimation();
            }
        }

        private void MainWindow_Deactivated(object sender, EventArgs e)
        {
            if (!isClosing && !IsMouseOver)
            {
                HideWithAnimation();
            }
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!isClosing)
            {
                e.Cancel = true;
                isClosing = true;
                HideWithAnimation();
            }
        }

        private void Exit(object? sender, EventArgs e)
        {
            isClosing = true;
            notifyIcon.Visible = false;
            Application.Current.Shutdown();
        }

        private void InputBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.Shift)
            {
                _ = SendMessage();
                e.Handled = true;
            }
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            _ = SendMessage();
        }
        private void SetupMarkdownEditor()
        {
            try
            {
                // Set up Markdown highlighting for AvalonEdit
                using (var stream = GetType().Assembly.GetManifestResourceStream("OSKTrayChat.MarkdownHighlighting.xshd"))
                {
                    if (stream != null)
                    {
                        using (var reader = new XmlTextReader(stream))
                        {
                            InputBox.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                        }
                    }
                    else
                    {
                        Console.WriteLine("MarkdownHighlighting.xshd resource not found. Falling back to default highlighting.");
                        InputBox.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("XML");
                    }
                }

                // Add error handling for text changes
                InputBox.TextChanged += (sender, e) =>
                {
                    try
                    {
                        InputBox.TextArea.TextView.Redraw();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error updating highlighting: {ex.Message}");
                        // Fallback: disable syntax highlighting if an error occurs
                        InputBox.SyntaxHighlighting = null;
                    }
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading Markdown highlighting: {ex.Message}");
                // Fallback: use default text editor without syntax highlighting
                InputBox.SyntaxHighlighting = null;
            }

            // Set up Markdown pipeline
            markdownPipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        }

        private async Task SendMessage()
        {
            string markdownText = InputBox.Text;

            InputBox.Clear();

            if (!string.IsNullOrWhiteSpace(markdownText))
            {
                try
                {
                    var res = _tcWritingAgent.InvokeStreamAsync(markdownText);

                    var text = string.Empty;

                    await foreach (var r in res)
                    {
                        text += r;
                        Dispatcher.Invoke(() =>
                        {
                            OutputBox.Text = text;
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending message");
                    MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            notifyIcon.Visible = false;
            base.OnClosed(e);
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            this.ViewModel.SelectOpenFile();
        }
    }
}