﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Hooking.Data;
using Hooking.Models;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using Hooking.Areas.Identity.Pages.Account.Manage;
using System.IO;
using System.Web;
using Microsoft.AspNetCore.Http;
using System.Runtime.Serialization.Json;  
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using GoogleMaps.LocationServices;
using Hooking.Services;

namespace Hooking.Controllers
{
    public class CottagesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ICottageService _cottageService;
        private readonly ICottageReservationsService _cottageReservationsService;
        private readonly ICottageRoomsService _cottageRoomsService;
        private readonly IFacilitiesService _facilitiesService;
        private readonly IHouseRulesService _houseRulesService;
        private readonly BlobUtility utility;
        public UserDetails cottageOwner;
        public CancelationPolicy cancelationPolicy;
        public HouseRules houseRules;
        public Facilities facilities;
        public List<CottageRoom> cottageRooms = new List<CottageRoom>();
        public Guid cottageId;
        public List<CottageImage> cottageImages = new List<CottageImage>();
        [TempData]
        public string StatusMessage { get; set; }

        public CottagesController(ApplicationDbContext context, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager,
                                  ICottageService cottageService, ICottageReservationsService cottageReservationsService,
                                  ICottageRoomsService cottageRoomsService,
                                  IFacilitiesService facilitiesService,
                                  IHouseRulesService houseRulesService)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            utility = new BlobUtility();
            _cottageService = cottageService;
            _cottageReservationsService = cottageReservationsService;
            _cottageRoomsService = cottageRoomsService;
            _facilitiesService = facilitiesService;
            _houseRulesService = houseRulesService;

        }
       

        // GET: Cottages
        public async Task<IActionResult> Index(string searchString = "", string filter = "", string sortOrder = "")
        {
            List<Cottage> cottages = await _context.Cottage.ToListAsync();
            List<Cottage> filteredCottages = new List<Cottage>();

            ViewData["Name"] = String.IsNullOrEmpty(sortOrder) ? "Name" : "";
            ViewData["Address"] = String.IsNullOrEmpty(sortOrder) ? "Address" : "";
            ViewData["City"] = String.IsNullOrEmpty(sortOrder) ? "City" : "";
            ViewData["Country"] = String.IsNullOrEmpty(sortOrder) ? "Country" : "";
            ViewData["AverageGrade"] = String.IsNullOrEmpty(sortOrder) ? "AverageGrade" : "";
            var ctg = from b in _context.Cottage
                      select b;
            switch (sortOrder)
            {
                case "Name":
                    ctg = ctg.OrderBy(b => b.Name);
                    break;
                case "Address":
                    ctg = ctg.OrderBy(b => b.Address);
                    break;
                case "City":
                    ctg = ctg.OrderBy(b => b.City);
                    break;
                case "Country":
                    ctg = ctg.OrderBy(b => b.Country);
                    break;
                case "AverageGrade":
                    ctg = ctg.OrderBy(b => b.AverageGrade);
                    break;


            }
            if (!String.IsNullOrEmpty(searchString))
            {
                ctg = ctg.Where(s => s.Name.Contains(searchString)
                                       || s.City.Contains(searchString) || s.Country.Contains(searchString));
            }

          
            return View(ctg.ToList());
        }
        [HttpGet("/Cottages/ShowAllUsers/{id}")]
        public async Task<IActionResult> ShowAllUsers(Guid id)
        {
            cottageId = id;
            ViewData["CottageId"] = cottageId;
            var allUsers = _context.UserDetails.ToListAsync();
            return View(allUsers);
        }


        // GET: Cottages/Details/5
        public async Task<IActionResult> Details(Guid id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cottage = _cottageService.GetCottageById(id);
            if (cottage == null)
            {
                return NotFound();
            }
            Guid cottageOwnerId = Guid.Parse(cottage.CottageOwnerId);
            var cottageOwnerUser = _context.CottageOwner.Where(m => m.UserDetailsId == cottage.CottageOwnerId).FirstOrDefault<CottageOwner>();
           
            cottageOwner = _context.UserDetails.Where(m => m.IdentityUserId == cottageOwnerUser.UserDetailsId).FirstOrDefault<UserDetails>();
            var cottageId = cottage.Id.ToString();
            houseRules = _houseRulesService.GetByCottageId(cottage.Id);
            Guid cottageCancelationPolicyId = Guid.Parse(cottage.CancelationPolicyId);
            cancelationPolicy = _context.CancelationPolicy.Where(m => m.Id == cottageCancelationPolicyId).FirstOrDefault<CancelationPolicy>();
            facilities = _facilitiesService.GetByCottageId(cottage.Id);
            cottageRooms = _cottageRoomsService.GetAllByCottageId(cottage.Id).ToList();
            var fullAddress = cottage.Address + "," + cottage.City + "," + cottage.Country;
          
            cottageImages = _context.CottageImages.Where(m => m.CottageId == cottageId).ToList();
            ViewBag.PhotoCount = cottageImages.Count;
            ViewData["CottageOwner"] = cottageOwner;
            ViewData["HouseRules"] = houseRules;
            ViewData["CancellationPolicyId"] = cancelationPolicy;
            ViewData["Facilities"] = facilities;
            ViewData["CottageRooms"] = cottageRooms;
            ViewData["CottageImages"] = cottageImages;
            ViewData["FullAddress"] = fullAddress;
            return View(cottage);
        }
        [HttpGet("/Cottages/MyCottage/{id}")]
        public async Task<IActionResult> MyCottage(Guid id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cottage = _cottageService.GetCottageById(id);
            Guid cottageOwnerId = Guid.Parse(cottage.CottageOwnerId);
            var cottageOwnerUser = _context.CottageOwner.Where(m => m.UserDetailsId == cottage.CottageOwnerId).FirstOrDefault<CottageOwner>();

            cottageOwner = _context.UserDetails.Where(m => m.IdentityUserId == cottageOwnerUser.UserDetailsId).FirstOrDefault<UserDetails>();
            var cottageId = cottage.Id.ToString();
            houseRules = _houseRulesService.GetByCottageId(cottage.Id);
            Guid cottageCancelationPolicyId = Guid.Parse(cottage.CancelationPolicyId);
            cancelationPolicy = _context.CancelationPolicy.Where(m => m.Id == cottageCancelationPolicyId).FirstOrDefault<CancelationPolicy>();
            facilities = _facilitiesService.GetByCottageId(cottage.Id);
            cottageRooms = _cottageRoomsService.GetAllByCottageId(cottage.Id).ToList();
            if (cottage == null)
            {
                return NotFound();
            }
            var fullAddress = cottage.Address + "," + cottage.City + "," + cottage.Country;
           
            cottageImages = _context.CottageImages.Where(m => m.CottageId == cottageId).ToList();
            ViewBag.PhotoCount = cottageImages.Count;
            ViewData["CottageOwner"] = cottageOwner;
            ViewData["HouseRules"] = houseRules;
            ViewData["CancellationPolicyId"] = cancelationPolicy;
            ViewData["Facilities"] = facilities;
            ViewData["CottageRooms"] = cottageRooms;
            ViewData["CottageImages"] = cottageImages;
            ViewData["FullAddress"] = fullAddress;
            return View(cottage);
        }

        // GET: Cottages/Create
        public IActionResult Create()
        {
            return View();
        }
        [HttpGet("/Cottages/CottagesForSpecialOffer")]
        public async Task<IActionResult> CottagesForSpecialOffer()
        {
            var user = await _userManager.GetUserAsync(User);
            
            return View(_cottageService.GetAllByOwnerId(user.Id));
        }

        // POST: Cottages/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Address,City,Country,Description,RoomCount,Area,AverageGrade,GradeCount,CancellationPolicyId,RegularPrice,WeekendPrice,CottageOwnerId,Id,RowVersion")] Cottage cottage)
        {
            if (ModelState.IsValid)
            {
               
                var user = await _userManager.GetUserAsync(User);
                _cottageService.Create(user.Id, cottage);
                return RedirectToAction("Create", "HouseRules", new { id = cottage.Id});
            }
            return RedirectToPage("/Account/Manage/MyCottages", new { area = "Identity" });
        }

        // GET: Cottages/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cottage = await _context.Cottage.FindAsync(id);
            if (cottage == null)
            {
                return NotFound();
            }
            return View(cottage);
        }
       
        // POST: Cottages/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Name,Description,RegularPrice,WeekendPrice,Id,RowVersion")] Cottage cottage)
        {
            if (id != cottage.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    
                    var cottageId = cottage.Id.ToString();
                    List<CottageSpecialOfferReservation> cottageSpecialOfferReservationsFull = new List<CottageSpecialOfferReservation>();
                    List<CottageReservation> cottageReservations = _cottageReservationsService.GetAllFutureByCottageId(cottageId).ToList();
                    List<CottageSpecialOfferReservation> cottageSpecialOfferReservations = await _context.CottageSpecialOfferReservation.ToListAsync<CottageSpecialOfferReservation>();
                    foreach (var cottageSpecialOfferReservation in cottageSpecialOfferReservations)
                    {
                        Guid csId = Guid.Parse(cottageSpecialOfferReservation.CottageSpecialOfferId);
                        var cottageSpecialOffer = _context.CottageSpecialOffer.Where(m => m.Id == csId).FirstOrDefault<CottageSpecialOffer>();
                        if (cottageSpecialOffer.CottageId == cottageId)
                        {
                            cottageSpecialOfferReservationsFull.Add(cottageSpecialOfferReservation);
                        }
                    }


                    if (cottageSpecialOfferReservationsFull.Count == 0 && cottageReservations.Count == 0)
                    {
                        _cottageService.Edit(id, cottage);
                    }   
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CottageExists(cottage.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                ViewData["StatusMessage"] = StatusMessage;
                return RedirectToRoute("default",
                                        new { controller = "Cottages", action = "MyCottage", id = cottage.Id });
            }
            return RedirectToPage("/Account/Manage/MyCottages", new { area = "Identity" });
        }
        
        // GET: Cottages/Delete/5
        public async Task<IActionResult> Delete(Guid id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cottage = _cottageService.GetCottageById(id);
            if (cottage == null)
            {
                return NotFound();
            }

            return View(cottage);
        }
        [HttpGet("/Cottages/CottagesForReservation")]
        public async Task<IActionResult> CottagesForReservation()
        {
            var user = await _userManager.GetUserAsync(User);
            return View(_cottageService.GetAllByOwnerId(user.Id));
        }

        // POST: Cottages/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var cottage = _cottageService.GetCottageById(id);
            var cottageId = cottage.Id.ToString();
            List<CottageSpecialOfferReservation> cottageSpecialOfferReservationsFull = new List<CottageSpecialOfferReservation>();
            List<CottageReservation> cottageReservations = _cottageReservationsService.GetAllFutureByCottageId(cottageId).ToList();
            List<CottageSpecialOfferReservation> cottageSpecialOfferReservations = await _context.CottageSpecialOfferReservation.ToListAsync<CottageSpecialOfferReservation>();
            foreach(var cottageSpecialOfferReservation in cottageSpecialOfferReservations)
            {
                Guid csId = Guid.Parse(cottageSpecialOfferReservation.CottageSpecialOfferId);
                var cottageSpecialOffer = _context.CottageSpecialOffer.Where(m => m.Id == csId).FirstOrDefault<CottageSpecialOffer>();
                if(cottageSpecialOffer.CottageId == cottageId)
                {
                    cottageSpecialOfferReservationsFull.Add(cottageSpecialOfferReservation);
                }
            }

            if (User.IsInRole("Admin"))
            {
                return RedirectToAction("Records", "Home");
            }

            if(cottageSpecialOfferReservationsFull.Count == 0 && cottageReservations.Count == 0)
            {
                _cottageService.DeleteById(id);
                
            } 
            
            return RedirectToPage("/Account/Manage/MyCottages", new { area = "Identity" });
        }
        [HttpPost("/Cottages/UploadImage/{id}")]
        public async Task<ActionResult> UploadImage(Guid id,IFormFile file)
        {
            if (file != null)
            {
                string ContainerName = "cottage"; //hardcoded container name
                string fileName = Path.GetFileName(file.FileName);
                using (var fileStream = file.OpenReadStream())
                {
                    string UserConnectionString = string.Format("DefaultEndpointsProtocol=https;AccountName=hookingstorage;AccountKey=+v8L5XkQZ7Z2CTfdTd03pngWlA4xu02caFJDGUkvGlo/rv8uZnM9CQQYleH3lpb+3Z8sefUOlC0EaoXWIquyDg==;EndpointSuffix=core.windows.net");
                    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(UserConnectionString);
                    CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                    CloudBlobContainer container = blobClient.GetContainerReference(ContainerName.ToLower());
                    CloudBlockBlob blockBlob = container.GetBlockBlobReference(fileName);
                    try
                    {
                        blockBlob.UploadFromStreamAsync(fileStream);

                    }
                    catch (Exception e)
                    {
                        var r = e.Message;
                        return null;
                    }

                    if (blockBlob != null)
                    {
                        CottageImage cottageImage = new CottageImage();
                        cottageImage.Id = Guid.NewGuid();
                        cottageImage.CottageId = id.ToString();
                        cottageImage.ImageUrl = blockBlob.Uri.ToString();
                        _context.CottageImages.Add(cottageImage);
                        _context.SaveChanges();
                    }

                }
                return RedirectToRoute("default",
                                       new { controller = "Cottages", action = "MyCottage", id = id });
            }
            else
            {
                return RedirectToRoute("default",
                                       new { controller = "Cottages", action = "MyCottage", id = id });
            }


        }
        private bool CottageExists(Guid id)
        {
            return _context.Cottage.Any(e => e.Id == id);
        }

    }
}
