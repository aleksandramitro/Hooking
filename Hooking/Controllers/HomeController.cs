﻿using Hooking.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Hooking.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult ConcurrencyError()
        {
            return View();
        }
        public IActionResult ConcurrencyActionError()
        {
            return View();
        }
        public IActionResult Cottages()
        {
            return Redirect("/Cottages");
        }
        public IActionResult Boats()
        {
            return Redirect("/Boats");
        }
        public IActionResult Adventures()
        {
            return Redirect("/Adventures");
        }

        public IActionResult Records()
        {
            return Redirect("/Records");
        }
        public IActionResult ReservationReviews()
        {
            return View();
        }
        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Appeals()
        {
            return View();
        }

        public IActionResult Reviews()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
