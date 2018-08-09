using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SSPFS.Web.Models;

namespace SSPFS.Web.Controllers
{
    public class DocBoxController : Controller
    {
        private readonly ServerAPI _serverApi;

        public DocBoxController(ServerAPI serverApi)
        {
            _serverApi = serverApi;
        }

        public async Task<IActionResult> Index(Guid id)
        {
            var client = ServerHost.Current.Hosts[id];
            if (client == null)
                return View("NoEncontrado");

            ViewBag.RepoId = id;
            var files = await _serverApi.ListFiles(id);
            return View(files);
        }

        public async Task<IActionResult> Download(Guid id, string name)
        {
            //todo: hacer funcionar con piping para evitar sobre-cargar la memoria.
            var result = await _serverApi.DownloadFile(id, name);

            Response.ContentLength = result.length;
            Response.ContentType = MediaTypeNames.Application.Octet;
            Response.Headers["Content-Disposition"] = new ContentDisposition() {
                DispositionType = DispositionTypeNames.Attachment,
                FileName = name
            }.ToString();

            CopyTo(result.stream, Response.Body, result.length);

            result.RequestContentDownloadedTrigger();

            return Ok();
        }
        private void CopyTo(Stream from, Stream to, int length, int buffer_size = 1024)
        {
            byte[] buffer = new byte[buffer_size];
            int pending_bytes = length;
            int readen_bytes;
            while (pending_bytes > 0)
            {
                readen_bytes = from.Read(buffer, 0, Math.Min(pending_bytes,buffer.Length));
                to.Write(buffer, 0, readen_bytes);
                pending_bytes -= readen_bytes;
            }
        }

        [HttpPost]
        // [RequestSizeLimit(100_000_000)]
        [DisableRequestSizeLimit]
        public IActionResult Upload(Guid id, IFormFile file)
        {
            _serverApi.UploadFile(id, file.FileName,file.Length, file.OpenReadStream());
            return Json(new { OK = 1 });
        }

        public IActionResult Refresh(Guid id)
        {
            var context = (IHubContext<Hubs.DocBoxHub>)Program.Services.GetService(typeof(IHubContext<Hubs.DocBoxHub>));
            context.Clients.Group(id.ToString()).SendAsync("FolderHasChanged");                
            return Ok();
        }
    }
}

