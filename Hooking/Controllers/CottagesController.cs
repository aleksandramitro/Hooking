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
using System.Text.Json;
using Hooking.Areas.Identity.Pages.Account.Manage;
using System.IO;
using System.Web;
using Microsoft.AspNetCore.Http;
using System.Runtime.Serialization.Json;  
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using GoogleMaps.LocationServices;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;

namespace Hooking.Controllers
{
    public class CottagesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly BlobUtility utility;
        private readonly IDistributedCache _cache;
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
                                    IDistributedCache cache)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            utility = new BlobUtility();
            _cache = cache;

        }
       

        // GET: Cottages
        public async Task<IActionResult> Index(string searchString = "", string filter = "", string sortOrder = "")
        {
            //List<Cottage> cottages = await _context.Cottage.ToListAsync();
            var cottages = await _cache.GetAsync("Cottage");
            if ((cottages?.Count() ?? 0) > 0)
            {
                var cottageString = Encoding.UTF8.GetString(cottages);
                var generatedCottages = JsonSerializer.Deserialize<List<Cottage>>(cottageString);
                return Ok(new { LoadedFromRedis = true, Data = generatedCottages });
            }
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
            string cId = cottageId.ToString();
            List<UserDetails> userDetails = new List<UserDetails>();
            List<CottageReservation> cottageReservations = _context.CottageReservation.Where(m => m.CottageId == cId).ToList();
            foreach(CottageReservation cottageReservation in cottageReservations)
            {
                if(cottageReservation.StartDate <= DateTime.Now && cottageReservation.EndDate >= DateTime.Now)
                {
                    UserDetails user = _context.UserDetails.Where(m => m.IdentityUserId == cottageReservation.UserDetailsId).FirstOrDefault<UserDetails>();
                    userDetails.Add(user);
                }
            }
            ViewData["CottageId"] = cottageId;
            return View(userDetails);
        }


        // GET: Cottages/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cottage = await _context.Cottage
                .FirstOrDefaultAsync(m => m.Id == id);
            if (cottage == null)
            {
                return NotFound();
            }
            Guid cottageOwnerId = Guid.Parse(cottage.CottageOwnerId);
            var cottageOwnerUser = _context.CottageOwner.Where(m => m.Id == cottageOwnerId).FirstOrDefault<CottageOwner>();
            var cottageOwnerUserId = Guid.Parse(cottageOwnerUser.UserDetailsId);
            cottageOwner = _context.UserDetails.Where(m => m.Id == cottageOwnerUserId).FirstOrDefault<UserDetails>();
            var cottageId = cottage.Id.ToString();
            CottagesHouseRules cottagesHouseRules = _context.CottagesHouseRules.Where(m => m.CottageId == cottageId).FirstOrDefault<CottagesHouseRules>();
            Guid houseRulesId = Guid.Parse(cottagesHouseRules.HouseRulesId);
            houseRules = _context.HouseRules.Where(m => m.Id == houseRulesId).FirstOrDefault<HouseRules>();
            Guid cottageCancelationPolicyId = Guid.Parse(cottage.CancelationPolicyId);
            cancelationPolicy = _context.CancelationPolicy.Where(m => m.Id == cottageCancelationPolicyId).FirstOrDefault<CancelationPolicy>();
            var cottagesFacilities = _context.CottagesFacilities.Where(m => m.CottageId == cottageId).FirstOrDefault<CottagesFacilities>();
            Guid cottagesFacilitiesId = Guid.Parse(cottagesFacilities.FacilitiesId);
            facilities = _context.Facilities.Where(m => m.Id == cottagesFacilitiesId).FirstOrDefault<Facilities>();
            List<CottagesRooms> cottagesRooms = await _context.CottagesRooms.Where(m => m.CottageId == cottageId).ToListAsync<CottagesRooms>();
            foreach(var cottagesRoom in cottagesRooms)
            {
                Guid cottageRoomId = Guid.Parse(cottagesRoom.CottageRoomId);
                var cottageRoom = _context.CottageRoom.Where(m => m.Id == cottageRoomId).FirstOrDefault<CottageRoom>();
                cottageRooms.Add(cottageRoom);
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
        [HttpGet("/Cottages/MyCottage/{id}")]
        public async Task<IActionResult> MyCottage(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cottage = await _context.Cottage
                .FirstOrDefaultAsync(m => m.Id == id);
            Guid cottageOwnerId = Guid.Parse(cottage.CottageOwnerId);
            var cottageOwnerUser = _context.CottageOwner.Where(m => m.Id == cottageOwnerId).FirstOrDefault<CottageOwner>();
            var cottageOwnerUserId = Guid.Parse(cottageOwnerUser.UserDetailsId); 
            cottageOwner = _context.UserDetails.Where(m => m.Id == cottageOwnerUserId).FirstOrDefault<UserDetails>();
            var cottageId = cottage.Id.ToString();
            CottagesHouseRules cottagesHouseRules = _context.CottagesHouseRules.Where(m => m.CottageId == cottageId).FirstOrDefault<CottagesHouseRules>();
            Guid houseRulesId = Guid.Parse(cottagesHouseRules.HouseRulesId);
            houseRules = _context.HouseRules.Where(m => m.Id == houseRulesId).FirstOrDefault<HouseRules>();
            Guid cottageCancelationPolicyId = Guid.Parse(cottage.CancelationPolicyId);
            cancelationPolicy = _context.CancelationPolicy.Where(m => m.Id == cottageCancelationPolicyId).FirstOrDefault<CancelationPolicy>();
            var cottagesFacilities = _context.CottagesFacilities.Where(m => m.CottageId == cottageId).FirstOrDefault<CottagesFacilities>();
            Guid cottagesFacilitiesId = Guid.Parse(cottagesFacilities.FacilitiesId);
            facilities = _context.Facilities.Where(m => m.Id == cottagesFacilitiesId).FirstOrDefault<Facilities>();
            List<CottagesRooms> cottagesRooms = await _context.CottagesRooms.Where(m => m.CottageId == cottageId).ToListAsync<CottagesRooms>();
            foreach (var cottagesRoom in cottagesRooms)
            {
                Guid cottageRoomId = Guid.Parse(cottagesRoom.CottageRoomId);
                var cottageRoom = _context.CottageRoom.Where(m => m.Id == cottageRoomId).FirstOrDefault<CottageRoom>();
                cottageRooms.Add(cottageRoom);
            }
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
            var userDetails = _context.UserDetails.Where(m => m.IdentityUserId == user.Id).FirstOrDefault();
            var userDetailsId = userDetails.Id.ToString();
            var cottageOwner = _context.CottageOwner.Where(m => m.UserDetailsId == userDetailsId).FirstOrDefault();
            var cottageOwnerId = cottageOwner.Id.ToString();
            List<Cottage> myCottages = await _context.Cottage.Where(m => m.CottageOwnerId == cottageOwnerId).ToListAsync();
            return View(myCottages);
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
                cottage.Id = Guid.NewGuid();
                var user = await _userManager.GetUserAsync(User);
                var userId = Guid.Parse(user.Id);
                var userDetails = _context.UserDetails.Where(m => m.IdentityUserId == user.Id).FirstOrDefault();
                var userDetailsId = userDetails.Id.ToString();
                var cottageOwner = _context.CottageOwner.Where(m => m.UserDetailsId == userDetailsId).FirstOrDefault();
                cottage.CottageOwnerId = cottageOwner.Id.ToString();
                cottage.CancelationPolicyId = "0";
                cottage.AverageGrade = 0;
                cottage.GradeCount = 0;
                _context.Add(cottage);
                await _context.SaveChangesAsync();
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
                    List<CottageReservation> cottageReservations = await _context.CottageReservation.Where(m => m.CottageId == cottageId).ToListAsync<CottageReservation>();
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
                        foreach(var reservation in _context.CottageReservation.Local)
                        {
                            if(reservation.CottageId == id.ToString())
                            {
                                return RedirectToAction("ConcurrencyError", "Home");
                            }
                        }
                        Cottage cottageTemp = await _context.Cottage.FindAsync(id);
                        cottageTemp.Name = cottage.Name;
                        cottageTemp.Description = cottage.Description;
                        cottageTemp.RegularPrice = cottage.RegularPrice;
                        cottageTemp.WeekendPrice = cottage.WeekendPrice;
                        _context.Update(cottageTemp);
                        await _context.SaveChangesAsync();
                        StatusMessage = "Uspesno ste izmeni informacije o vikendici!";
                    }
                    else
                    {
                        StatusMessage = "Ne mozete izmeniti informacije o vikendice jer je rezervisana!";
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
                return RedirectToPage("/Account/Manage/MyCottages", new { area = "Identity" });
            }
            return RedirectToPage("/Account/Manage/MyCottages", new { area = "Identity" });
        }
        
        // GET: Cottages/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cottage = await _context.Cottage
                .FirstOrDefaultAsync(m => m.Id == id);
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
            var userId = Guid.Parse(user.Id);
            var userDetails = _context.UserDetails.Where(m => m.IdentityUserId == user.Id).FirstOrDefault();
            var userDetailsId = userDetails.Id.ToString();
            var cottageOwner = _context.CottageOwner.Where(m => m.UserDetailsId == userDetailsId).FirstOrDefault();
            var cottageOwnerId = cottageOwner.Id.ToString();
            List<Cottage> myCottages = await _context.Cottage.Where(m => m.CottageOwnerId == cottageOwnerId).ToListAsync();
            return View(myCottages);
        }

        // POST: Cottages/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var cottage = await _context.Cottage.FindAsync(id);
            var cottageId = cottage.Id.ToString();
            List<CottageSpecialOfferReservation> cottageSpecialOfferReservationsFull = new List<CottageSpecialOfferReservation>();
            List<CottageReservation> cottageReservations = await _context.CottageReservation.Where(m => m.CottageId == cottageId).ToListAsync<CottageReservation>();
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

            bool deleted = false;
            if(cottageSpecialOfferReservationsFull.Count == 0 && cottageReservations.Count == 0)
            {
                _context.Cottage.Remove(cottage);
                await _context.SaveChangesAsync();
                StatusMessage = "Uspesno ste poslali zahtev za brisanje vikendice!";
                deleted = true;
            } else
            {
                StatusMessage = "Ne mozete poslati zahtev za brisanje vikendice jer je rezervisana!";
            }

            ViewData["StatusMessage"] = StatusMessage;
            if (User.IsInRole("Admin"))
            {
                if (deleted)
                {
                    return RedirectToAction("Records", "Home");
                }

                return await Delete(id);
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
                        await blockBlob.UploadFromStreamAsync(fileStream);
                        
                    }
                    catch (Exception e)
                    {
                        var r = e.Message;
                        return null;
                    }
                    
                    if(blockBlob != null)
                    {
                        CottageImage cottageImage = new CottageImage();
                        cottageImage.Id = Guid.NewGuid();
                        cottageImage.CottageId = id.ToString();
                        cottageImage.ImageUrl = blockBlob.Uri.ToString();
                        _context.CottageImages.Add(cottageImage);
                        await _context.SaveChangesAsync();
                    }
                   
                }
                return RedirectToPage("/Account/Manage/MyCottages", new { area = "Identity" });
            }
            else
            {
                return RedirectToPage("/Account/Manage/MyCottages", new { area = "Identity" });
            }


        }
        private bool CottageExists(Guid id)
        {
            return _context.Cottage.Any(e => e.Id == id);
        }

        [HttpPost("/Cottages/CottagesFiltered")]

        public async Task<IActionResult> CottagesFiltered(DateTime StartDate,DateTime EndDate, int MaxPersonCount)
        {
            List<Cottage> cottages = await _context.Cottage.ToListAsync();
            List<CottageNotAvailablePeriod> cottageNotAvailablePeriods = await _context.CottageNotAvailablePeriod.ToListAsync();

            List<Cottage> filteredCottages = new List<Cottage>();
   

            foreach (CottageNotAvailablePeriod ctgNotAvailable in cottageNotAvailablePeriods)
            {

               // System.Diagnostics.Debug.WriteLine("ctgNotAvailableID: " + ctgNotAvailable.CottageId.ToString());
                foreach(Cottage ctg in cottages)
                {
                    //  System.Diagnostics.Debug.WriteLine("VikendicaID: " + ctg.Id.ToString());
                    if (ctgNotAvailable.CottageId != null)
                    {
                        if (ctg.Id == Guid.Parse(ctgNotAvailable.CottageId))
                        {
                            if ((ctgNotAvailable.StartTime >= StartDate && ctgNotAvailable.StartTime <= EndDate) && ctgNotAvailable.EndTime >= EndDate)
                            {
                                //  cottageNotAvailablePeriods.Remove(ctgNotAvailable);
                            }
                            else if ((ctgNotAvailable.EndTime >= StartDate && ctgNotAvailable.EndTime <= EndDate) && ctgNotAvailable.StartTime <= StartDate)
                            {
                                // cottageNotAvailablePeriods.Remove(ctgNotAvailable);
                            }
                            else
                            {
                                if (!filteredCottages.Contains(ctg))
                                {
                                    filteredCottages.Add(ctg);
                                }
                            }

                        }
                        else
                        {
                            if (!filteredCottages.Contains(ctg))
                            {
                                filteredCottages.Add(ctg);
                            }
                        }
                    }
                   
                }
            }
            ViewData["StartDate"] = StartDate;
            ViewData["EndDate"] = EndDate;
            ViewData["MaxPersonCount"] = MaxPersonCount;
            System.Diagnostics.Debug.WriteLine("cottageFiltered");
            System.Diagnostics.Debug.WriteLine(MaxPersonCount.ToString());


            return View(filteredCottages);
        }
        [HttpGet("/Cottages/FinishCottageReservation")]
        public async Task<IActionResult> FinishCottageReservation(Guid? id,DateTime StartDate,DateTime EndDate,int MaxPersonCount)
        {
            System.Diagnostics.Debug.WriteLine("finishCottageReservation");
            System.Diagnostics.Debug.WriteLine(MaxPersonCount.ToString());
            if (id == null)
            {
                return NotFound();
            }

            var cottage = await _context.Cottage
                .FirstOrDefaultAsync(m => m.Id == id);
            if (cottage == null)
            {
                return NotFound();
            }
            Guid cottageOwnerId = Guid.Parse(cottage.CottageOwnerId);
            var cottageOwnerUser = _context.CottageOwner.Where(m => m.UserDetailsId == cottage.CottageOwnerId).FirstOrDefault<CottageOwner>();

            cottageOwner = _context.UserDetails.Where(m => m.IdentityUserId == cottageOwnerUser.UserDetailsId).FirstOrDefault<UserDetails>();
            var cottageId = cottage.Id.ToString();
            CottagesHouseRules cottagesHouseRules = _context.CottagesHouseRules.Where(m => m.CottageId == cottageId).FirstOrDefault<CottagesHouseRules>();
            Guid houseRulesId = Guid.Parse(cottagesHouseRules.HouseRulesId);
            houseRules = _context.HouseRules.Where(m => m.Id == houseRulesId).FirstOrDefault<HouseRules>();
            Guid cottageCancelationPolicyId = Guid.Parse(cottage.CancelationPolicyId);
            cancelationPolicy = _context.CancelationPolicy.Where(m => m.Id == cottageCancelationPolicyId).FirstOrDefault<CancelationPolicy>();
            var cottagesFacilities = _context.CottagesFacilities.Where(m => m.CottageId == cottageId).FirstOrDefault<CottagesFacilities>();
            Guid cottagesFacilitiesId = Guid.Parse(cottagesFacilities.FacilitiesId);
            facilities = _context.Facilities.Where(m => m.Id == cottagesFacilitiesId).FirstOrDefault<Facilities>();
            List<CottagesRooms> cottagesRooms = await _context.CottagesRooms.Where(m => m.CottageId == cottageId).ToListAsync<CottagesRooms>();
            foreach (var cottagesRoom in cottagesRooms)
            {
                Guid cottageRoomId = Guid.Parse(cottagesRoom.CottageRoomId);
                var cottageRoom = _context.CottageRoom.Where(m => m.Id == cottageRoomId).FirstOrDefault<CottageRoom>();
                cottageRooms.Add(cottageRoom);
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
            System.Diagnostics.Debug.WriteLine(StartDate.ToString());
            System.Diagnostics.Debug.WriteLine(EndDate.ToString());
            ViewData["StartDate"] = StartDate;
            ViewData["EndDate"] = EndDate;
            ViewData["MaxPersonCount"] = MaxPersonCount;

            return View(cottage);
        }

    }
}
