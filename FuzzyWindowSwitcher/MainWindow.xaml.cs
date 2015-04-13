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
using System.Globalization;
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

            m_Windows = new List<SystemWindow>();
            WindowList.DisplayMemberPath = "Title";
            WindowList.ItemsSource = m_Windows;
            var view = CollectionViewSource.GetDefaultView(WindowList.ItemsSource) as CollectionView;
            view.Filter = FilterWindowList;
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
                WindowState = WindowState.Minimized;
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
        private List<SystemWindow> m_Windows;

        private void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            OnWindowSelected();
        }

        private void OnWindowSelected()
        {
            var selectedItem = WindowList.SelectedItem;
            if (selectedItem == null)
            {
                return;
            }

            var selectedWindow = selectedItem as SystemWindow;

            SystemWindow.ForegroundWindow = selectedWindow;
            if (selectedWindow.WindowState == System.Windows.Forms.FormWindowState.Minimized)
            {
                selectedWindow.WindowState = System.Windows.Forms.FormWindowState.Normal;
            }
        }

        private void OnActivated(object sender, EventArgs e)
        {
            var windows = SystemWindow.FilterToplevelWindows(new Predicate<SystemWindow>(IsAppWindow));
            Array.Sort(windows, (wnd1, wnd2) => wnd1.Title.CompareTo(wnd2.Title));

            //
            // If an item was already selected in the list, try to find it in the new list and
            // select it.
            //

            var selectedWindow = WindowList.SelectedItem as SystemWindow;

            m_Windows.Clear();
            m_Windows.AddRange(windows);

            WindowList.Items.Refresh();
            CollectionViewSource.GetDefaultView(WindowList.ItemsSource).Refresh();

            //
            // If the previously selected window is not found in the new list, simply select
            // the first element.
            //

            int selectIndex = 0;

            if (selectedWindow != null)
            {
                for (int i = 0; i < WindowList.Items.Count; ++i)
                {
                    SystemWindow wnd = WindowList.Items[i] as SystemWindow;
                    if (wnd.HWnd == selectedWindow.HWnd)
                    {
                        selectIndex = i;
                        break;
                    }
                }
            }

            if (!WindowList.Items.IsEmpty)
            {
                WindowList.SelectedIndex = selectIndex;
                WindowList.UpdateLayout();
                (WindowList.ItemContainerGenerator.ContainerFromIndex(selectIndex) as ListViewItem).Focus();
            }

            FuzzySearchTitle.Focus();
        }

        private bool FilterWindowList(object wnd)
        {
            var filter = FuzzySearchTitle.Text;
            if (string.IsNullOrWhiteSpace(filter))
            {
                return true;
            }

            var window = wnd as SystemWindow;

            int index = CultureInfo.InvariantCulture.CompareInfo.IndexOf(window.Title, filter, CompareOptions.IgnoreCase);
            if (index != -1)
            {
                return true;
            }

            return false;
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
            {
                return;
            }

            OnWindowSelected();
            e.Handled = true;
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            //
            // Intercept Up and Down keys and switch focus to the window list.
            //

            if (e.Key != Key.Up && e.Key != Key.Down)
            {
                return;
            }

            if (WindowList.Items.IsEmpty)
            {
                return;
            }

            if (!FuzzySearchTitle.IsFocused)
            {
                return;
            }

            ((UIElement)Keyboard.FocusedElement).MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        }

        private void OnKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            FuzzySearchTitle.SelectAll();
        }

        private void OnListKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            int index = WindowList.SelectedIndex;
            if (index == -1)
            {
                return;
            }

            (WindowList.ItemContainerGenerator.ContainerFromIndex(index) as ListViewItem).Focus();
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(WindowList.ItemsSource).Refresh();

            //
            // If what was selected is now filtered out, select the first window.
            //

            if (WindowList.SelectedIndex == -1 && !WindowList.Items.IsEmpty)
            {
                WindowList.SelectedIndex = 0;
            }
        }
    }
}
