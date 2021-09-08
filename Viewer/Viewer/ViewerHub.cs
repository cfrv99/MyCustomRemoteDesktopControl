using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Viewer
{
    public class ViewerHub : Hub
    {
        public async Task Image(string base64)
        {
            await Clients.All.SendAsync("ViewImage", base64);
        }

        public async Task ImageByteArray(byte[] array)
        {
            var task = Task.Run(() =>
            {
                var base64 = Convert.ToBase64String(array);
                return base64;
            });
            var base64 = await task;
            await Clients.All.SendAsync("ViewImage", base64);

        }
        public async Task ImageStream(MemoryStream stream)
        {
            var array = stream.ToArray();
            var base64 = Convert.ToBase64String(array);
            await Clients.All.SendAsync("ViewImage", base64);
        }

        public async Task CursorPosition(int x, int y)
        {
            await Clients.All.SendAsync("GetCursorPosition", x, y);
        }

        public async Task ClickEvents(string clickType, int x, int y)
        {
            await Clients.All.SendAsync("GetClickPosition", clickType, x, y);
        }
    }
}
