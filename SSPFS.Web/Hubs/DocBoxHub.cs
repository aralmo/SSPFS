using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace SSPFS.Web.Hubs
{
    public class DocBoxHub : Hub
    {
        public async Task JoinFolder(string identifier)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, identifier);
        }

        public async Task FolderHasChanged(Guid identifier)
        {
            await Clients.Group(identifier.ToString()).SendAsync("FolderHasChanged");
        }
    }
}