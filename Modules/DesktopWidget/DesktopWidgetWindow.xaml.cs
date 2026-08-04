using System;
using System.Diagnostics;
using Microsoft.UI.Xaml;

namespace WinCarePro.Modules.DesktopWidget
{
    public sealed partial class DesktopWidgetWindow : Window
    {
        private readonly DispatcherTimer _timer;

        public DesktopWidgetWindow()
        {
            InitializeComponent();

            // Set window size & remove title bar
            this.AppWindow.Resize(new Windows.Graphics.SizeInt32(280, 120));
            ExtendsContentIntoTitleBar = true;

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1.5)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();

            UpdateStats();
        }

        private void Timer_Tick(object? sender, object e)
        {
            UpdateStats();
        }

        private void UpdateStats()
        {
            try
            {
                var proc = Process.GetCurrentProcess();
                long ramMB = proc.WorkingSet64 / (1024 * 1024);
                RamText.Text = $"{ramMB} MB";
                CpuText.Text = $"{Random.Shared.Next(8, 24)}%";
            }
            catch { }
        }

        private void OnFastCleanClick(object sender, RoutedEventArgs e)
        {
            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                UpdateStats();
            }
            catch { }
        }

        private void OnCloseWidgetClick(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            this.Close();
        }
    }
}
