using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScreenSharer
{
    public partial class Form1 : Form
    {
        private HubConnection _hubConnection;
        private string connectionUrl = "http://192.168.0.106:5002/viewer";
        //private string connectionUrl = "http://remoteserver99-001-site1.dtempurl.com/viewer";
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool SetCursorPos(int x, int y);
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, UIntPtr dwExtraInfo);
        private const uint MOUSEEVENTF_LEFTDOWN = 0x02;
        private const uint MOUSEEVENTF_LEFTUP = 0x04;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const uint MOUSEEVENTF_RIGHTUP = 0x10;
        private static System.Timers.Timer timer;
        public Form1()
        {
            InitializeComponent();
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(connectionUrl).Build();
            _hubConnection.On<int, int>("GetCursorPosition", (x, y) => SetMousePorisitionInScreen(x, y));
            _hubConnection.On<string, int, int>("GetClickPosition", (clickType, x, y) => SetMouseClickInScreen(clickType, x, y));

        }
        private static byte[] ImageToByte(Image img)
        {
            ImageConverter converter = new ImageConverter();
            return (byte[])converter.ConvertTo(img, typeof(byte[]));
        }
        private void SetMousePorisitionInScreen(int x, int y)
        {
            double cofisient = Math.Round(0.4901960, 3);
            var yNis = Math.Round(0.78431372549, 5);
            var xNis = Math.Round(0.625, 3);
            var xPos = (int)Math.Round(x / xNis, 2);
            var yPos = (int)Math.Round(y / yNis, 2);
            SetCursorPos(xPos, yPos);
        }
        private void SetMouseClickInScreen(string clickType, int x, int y)
        {
            switch (clickType)
            {
                case "click":
                    sendMouseClick(x, y);
                    break;
                case "dblclick":
                    sendMouseRightclick(x, y);
                    break;

                default:
                    break;
            }
        }
        private void ByteToImage(byte[] array)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "Images//bos.jpg");
            File.SetAttributes(@"C:\Users\Home\source\repos\ScreenSharer\ScreenSharer\Images\bos.jpg", FileAttributes.Normal);

            File.WriteAllBytes(@"C:\Users\Home\source\repos\ScreenSharer\ScreenSharer\Images\bos.jpg", array);
        }
        private async void Form1_Load(object sender, EventArgs e)
        {
        }
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        private void StartChrome()
        {
            var allScreens = Screen.AllScreens.ToList();

            var screenOfChoice = allScreens[0]; // repllace with your own logic

            var chromeProcess = new Process
            {
                StartInfo =
            {
                    Arguments = "https://www.google.com --new-window --start-fullscreen",
                    FileName = @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
                    WindowStyle = ProcessWindowStyle.Normal
            }
            };

            chromeProcess.Start();

            // Needed to move the the process.
            Thread.Sleep(1000);

            MoveWindow(chromeProcess.MainWindowHandle, screenOfChoice.WorkingArea.Right, screenOfChoice.WorkingArea.Top, screenOfChoice.WorkingArea.Width, screenOfChoice.WorkingArea.Height, false);
        }
        private async void button1_Click(object sender, EventArgs e)
        {
            await _hubConnection.StartAsync();
            string base64Image;
            while (true)
            {
                Rectangle bound = Screen.PrimaryScreen.Bounds;
                Bitmap screenshot = new Bitmap(bound.Width, bound.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                Graphics graphics = Graphics.FromImage(screenshot);
                graphics.CopyFromScreen(bound.X, bound.Y, 0, 0, bound.Size, CopyPixelOperation.SourceCopy);
                screenshot = new Bitmap(screenshot, bound.Width, bound.Height);

                string sigBase64 = null;
                using (var ms = new MemoryStream())
                {
                    screenshot.Save(ms, ImageFormat.Png);
                    var bytes = ms.ToArray();
                    sigBase64 = Convert.ToBase64String(ms.ToArray());
                    await _hubConnection.InvokeAsync("Image", sigBase64);
                }

                // var image = ImageToByte(screenshot);
                //base64Image = Convert.ToBase64String(image);

                await Task.Delay(50);
            }

            //await _hubConnection.StartAsync();
            //timer = new System.Timers.Timer();
            //timer.Elapsed += timer_trick;
            //timer.Interval = 100;
            //timer.Enabled = true;
        }
        private async void timer_trick(object sender, EventArgs e)
        {
            var data = await GetBitMapScreen();
            await _hubConnection.InvokeAsync("Image", data);
        }

        private Task<string> GetBitMapScreen()
        {

            var thread = Task.Run(() =>
            {
                string sigBase64 = null;
                //string base64Image;
                Rectangle bound = Screen.PrimaryScreen.Bounds;
                Bitmap screenshot = new Bitmap(bound.Width, bound.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                Graphics graphics = Graphics.FromImage(screenshot);
                graphics.CopyFromScreen(bound.X, bound.Y, 0, 0, bound.Size, CopyPixelOperation.SourceCopy);
                screenshot = new Bitmap(screenshot, bound.Width, bound.Height);


                using (var ms = new MemoryStream())
                {
                    screenshot.Save(ms, ImageFormat.Png);
                    sigBase64 = Convert.ToBase64String(ms.ToArray());
                }

                // var image = ImageToByte(screenshot);
                //base64Image = Convert.ToBase64String(image);

                return sigBase64;
            });
            return thread;

        }
        void sendMouseRightclick(int x, int y)
        {
            mouse_event(MOUSEEVENTF_RIGHTDOWN | MOUSEEVENTF_RIGHTUP, Convert.ToUInt32(x), Convert.ToUInt32(y), 0, UIntPtr.Zero);
        }
        void sendMouseClick(int x, int y)
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, Convert.ToUInt32(x), Convert.ToUInt32(y), 0, UIntPtr.Zero);

        }
        //void sendMouseDoubleClick(Point p)
        //{
        //    mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, p.X, p.Y, 0, 0);

        //    Thread.Sleep(150);

        //    mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, p.X, p.Y, 0, 0);
        //}

        //void sendMouseRightDoubleClick(Point p)
        //{
        //    mouse_event(MOUSEEVENTF_RIGHTDOWN | MOUSEEVENTF_RIGHTUP, p.X, p.Y, 0, 0);

        //    Thread.Sleep(150);

        //    mouse_event(MOUSEEVENTF_RIGHTDOWN | MOUSEEVENTF_RIGHTUP, p.X, p.Y, 0, 0);
        //}

        //void sendMouseDown()
        //{
        //    mouse_event(MOUSEEVENTF_LEFTDOWN, 50, 50, 0, 0);
        //}

        //void sendMouseUp()
        //{
        //    mouse_event(MOUSEEVENTF_LEFTUP, 50, 50, 0, 0);
        //}
    }
}
