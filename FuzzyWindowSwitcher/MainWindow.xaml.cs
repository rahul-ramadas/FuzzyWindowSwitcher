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
using System.Drawing;
using System.IO;
using ManagedWinapi.Windows;

namespace FuzzyWindowSwitcher
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            m_NotifyIcon = new System.Windows.Forms.NotifyIcon();
            Stream iconStream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/FuzzyWindowSwitcher.ico")).Stream;
            m_NotifyIcon.Icon = new System.Drawing.Icon(iconStream);
            iconStream.Dispose();
            m_NotifyIcon.Visible = true;
            m_NotifyIcon.Click +=
                delegate(object Sender, EventArgs Args)
                {
                    this.Show();
                    this.WindowState = WindowState.Normal;
                };

            this.PreviewKeyDown += new KeyEventHandler(HandleEsc);

            var windows = SystemWindow.FilterToplevelWindows(new Predicate<SystemWindow>(IsAppWindow));

            m_Windows = new List<string>();
            foreach (var window in windows)
            {
                m_Windows.Add(window.Title);
            }

            WindowList.ItemsSource = m_Windows;
        }

        static bool IsAppWindow(SystemWindow Window)
        {
            return Window.Visible &&
                !Window.ExtendedStyle.HasFlag(WindowExStyleFlags.TOOLWINDOW);
        }

        private void HandleEsc(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            m_NotifyIcon.Visible = false;

            base.OnClosed(e);
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                this.Hide();
            }

            base.OnStateChanged(e);
        }

        private System.Windows.Forms.NotifyIcon m_NotifyIcon;
        private List<string> m_Windows;
    }
}
