﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        public IActionResult Index(Guid id)
        {
            ViewBag.RepoId = id;
            var files = _serverApi.ListFiles(id);
            return View(files);
        }

        public IActionResult Download(Guid id, string name)
        {
            var stream = _serverApi.DownloadFile(id, name);
            return File(stream, System.Net.Mime.MediaTypeNames.Application.Octet, name);
        }


        [HttpGet]
        public IActionResult Upload(Guid id)
        {
            return View();
        }

        [HttpPost]
        public IActionResult Upload(Guid id, IFormFile file)
        {
            _serverApi.UploadFile(id, file.FileName, file.OpenReadStream());
            return RedirectToAction(nameof(Index), new { id = id });
        }
    }
}
