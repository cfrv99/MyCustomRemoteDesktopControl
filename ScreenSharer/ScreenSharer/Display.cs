using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScreenSharer
{
    public class Display
    {
        #region ddlImports
        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        private static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetActiveWindow(IntPtr hWnd);

        const int SWP_SHOWWINDOW = 0x0040;
        #endregion

        private readonly Screen _screen;
        public bool IsPrimary { get { return _screen.Primary; } }

        private List<App> _activeApplications;
        private int _currentListIndex;

        public Display(Screen screen)
        {
            _screen = screen;
            Init();
        }

        private void Init()
        {
            _activeApplications = new List<App>();
            _currentListIndex = 0;
        }

        public void AddApp(App app)
        {
            _activeApplications.Add(app);
            ShowAppOnDisplay(app);
        }

        public void RotateToNextApp()
        {
            if (_activeApplications.Count <= 0) return;
            SetAppToForeground(_activeApplications[_currentListIndex]);

            var newIndex = _currentListIndex + 1;
            _currentListIndex = newIndex < _activeApplications.Count ? newIndex : 0;
        }

        private void SetAppToForeground(App app)
        {
            SetActiveWindow(app.Process.MainWindowHandle);
            Thread.Sleep(1000);
            SetForegroundWindow(app.Process.MainWindowHandle);
        }

        public static List<Display> GetAllDisplays()
        {
            var allScreens = Screen.AllScreens.ToList();
            var displays = new List<Display>();
            allScreens.ForEach(screen => displays.Add(new Display(screen)));
            return displays;
        }

        private void ShowAppOnDisplay(App app)
        {
            SetWindowPos(app.Process.MainWindowHandle, 0, _screen.WorkingArea.Left, _screen.WorkingArea.Top, _screen.WorkingArea.Width, _screen.WorkingArea.Height, SWP_SHOWWINDOW);
        }
    }


    public class App
    {
        private readonly string _executeablePath;
        private readonly string _args;

        public Process Process { get; private set; }

        public App(string executeablePath, string args)
        {
            _executeablePath = executeablePath;
            _args = args;
        }

        public void StartApp()
        {
            var newProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _executeablePath,
                    Arguments = _args,
                    WindowStyle = ProcessWindowStyle.Normal
                }
            };
            newProcess.Start();
            Thread.Sleep(1000);
            newProcess.WaitForInputIdle();
            Process = newProcess;
        }
    }
}
