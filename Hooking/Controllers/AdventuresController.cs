﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hooking.Data;
using Hooking.Models;
using Hooking.Models.DTO;
using Hooking.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Hooking.Controllers
{
    public class AdventuresController : Controller
    {
        private readonly IAdventureService _adventureService;
        private readonly ApplicationDbContext _context;
        public List<AdventureImage> adventureImages = new List<AdventureImage>();

        public AdventuresController(IAdventureService adventureService, 
            ApplicationDbContext context)
        {
            _adventureService = adventureService;
            _context = context;
        }

        // GET: Adventures
        public IActionResult Index()
        {
            return View(_adventureService.GetAdventures());
        }
        public IActionResult Charts()
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var adventures = _adventureService.GetInstructorAdventures(userId);
            
            return View();
        }

        public IActionResult InstructorIndex(bool triedToDelete = false)
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var adventures = _adventureService.GetInstructorAdventures(userId);
            

            UserDetails userDetails = _context.UserDetails.FirstOrDefault(u => u.IdentityUserId == userId);

            if (userDetails == null)
            {
                return NotFound();
            }

            Instructor instructor = _context.Instructor.FirstOrDefault(i => i.UserDetailsId == userDetails.Id.ToString());

            if (instructor == null)
            {
                return NotFound();
            }

            ViewData["InstructorId"] = instructor.Id.ToString();

            if (triedToDelete)
            {
                ViewData["Status"] = "Nije moguće obrisati realizaciju jer za nju postoje rezervacije.";
            }

            return View(adventures);
        }
        public IActionResult MyAdventures()
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var adventures = _adventureService.GetInstructorAdventures(userId);

            return View(adventures);
        }
        [HttpGet("/Adventures/ShowAllUsers/{id}")]
        public IActionResult ShowAllUsers(Guid id)
        {
            var adventureId = id;
            ViewData["AdventureId"] = adventureId;

            var allUsers = _adventureService.GetAllUserDetails();
            
            return View(allUsers);
        }

        // GET: Adventures/Details/5
        public IActionResult Details(Guid? adventureId)
        {
            if (adventureId == null)
            {
                return NotFound();
            }

            AdventureDTO dto = _adventureService.GetAdventureDto((Guid)adventureId);
            if (dto == null)
            {
                return NotFound();
            }
            adventureImages = _adventureService.GetAdventureImages(adventureId ?? throw new NullReferenceException()).ToList();
            ViewData["AdventureImages"] = adventureImages;
            return View(dto);
        }
        [HttpPost("/Adventures/UploadImage/{id}")]
        public async Task<ActionResult> UploadImage(Guid id, IFormFile file)
        {
            if (file != null)
            {
                string ContainerName = "adventure"; //hardcoded container name
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

                    if (blockBlob != null)
                    {
                        _adventureService.AddAdventureImage(id, blockBlob);
                    }

                }
                return RedirectToAction(nameof(InstructorIndex));
            }
            else
            {
                return RedirectToAction(nameof(InstructorIndex));
            }


        }

        // GET: Adventures/Create
        public IActionResult Create(string instructorId)
        {
            Adventure newAdventure = new Adventure
            {
                InstructorId = instructorId
            };
            return View(new AdventureDTO(newAdventure));
        }

        // POST: Adventures/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create([Bind("InstructorId,Name,Address,City,Country,Description,MaxPersonCount,CancellationPolicyId,ChildFriendly,YouKeepCatch,CatchAndReleaseAllowed,CabinSmoking,AverageGrade,Price")] AdventureDTO dto)
        {
            if (ModelState.IsValid)
            {
                _adventureService.Create(dto);

                return RedirectToAction(nameof(InstructorIndex));
            }
            return View(dto);
        }

        // GET: Adventures/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var adventure = _adventureService.FindAdventureById(id ?? throw new NullReferenceException());
            if (adventure == null)
            {
                return NotFound();
            }
            return View(adventure);
        }

        // POST: Adventures/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("InstructorId,Name,Address,City,Country,Description,MaxPersonCount,CancellationPolicyId,AverageGrade,Price,Id,RowVersion")] Adventure adventure)
        {
            if (id != adventure.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _adventureService.UpdateAdventure(adventure);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AdventureExists(adventure.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(InstructorIndex));
            }
            return View(adventure);
        }
        public IActionResult AdventureForSpecialOffer()
        {
            List<Adventure> adventures = _adventureService.GetAdventuresForSpecialOffer(User).ToList();
            return View(adventures);
        }
        // GET: Adventures/Delete/5
        public async Task<IActionResult> Delete(Guid? adventureId, Guid? instructorId)
        {
            if (adventureId == null)
            {
                return NotFound();
            }

            

            List<AdventureRealisation> realizations =
                _context.AdventureRealisation.Where(r => r.AdventureId == adventureId.ToString()).ToList();

            if (realizations.Count != 0)
            {
                List<AdventureReservation> reservations = new List<AdventureReservation>();

                foreach (AdventureRealisation realization in realizations)
                {
                    /*_context.Add(new AdventureReservation
                    {
                        AdventureRealisationId = realization.Id.ToString(),
                        IsReviewed = false,
                        UserDetailsId = "dadb6eb8-be7b-47af-950b-7ad2871fe206"
                    });*/
                    await _context.SaveChangesAsync();
                    foreach (AdventureReservation reservation in _context.AdventureReservation
                        .Where(r => r.AdventureRealisationId == realization.Id.ToString()).ToList())
                    {
                        reservations.Add(reservation);
                    }
                    if (reservations.Count != 0)
                    {
                        return RedirectToAction(nameof(InstructorIndex), new { triedToDelete = true });
                    }
                }
            }

            var adventure = _adventureService.FindAdventureById((Guid) adventureId);
            if (adventure == null)
            {
                return NotFound();
            }

            return View(adventure);
        }

        // POST: Adventures/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var adventure = await _context.Adventure.FindAsync(id);
            List<AdventureRealisation> adventureRealisations = new List<AdventureRealisation>();
            string adventureId = adventure.Id.ToString();
            adventureRealisations = await _context.AdventureRealisation.Where(m => m.AdventureId == adventureId).ToListAsync();
            if(adventureRealisations.Count == 0)
            {
                _context.Adventure.Remove(adventure);
            }
            List<AdventureSpecialOffer> adventureSpecialOffers = new List<AdventureSpecialOffer>();
            adventureSpecialOffers = await _context.AdventureSpecialOffer.Where(m => m.AdventureId == adventureId).ToListAsync();
            if(adventureSpecialOffers.Count == 0)
            {
                _context.Adventure.Remove(adventure);
                AdventuresAdventureRules rules = _context.AdventuresAdventureRules.FirstOrDefault(r => r.AdventureId == adventureId);
                _context.AdventuresAdventureRules.Remove(rules);
                await _context.SaveChangesAsync();
            }

           return RedirectToAction(nameof(InstructorIndex));
        }

        private bool AdventureExists(Guid id)
        {
            return _adventureService.AdventureExists(id);
        }

        [HttpPost("/Adventures/InstructorFiltered")]

        public async Task<IActionResult> InstructorFiltered(DateTime StartTime, DateTime EndDate)
        {
            List<Instructor> instructors = await _context.Instructor.ToListAsync();
            List<InstructorNotAvailablePeriod> instructorNotAvailablePeriods = await _context.InstructorNotAvailablePeriod.ToListAsync();

            List<Instructor> filteredInstructors = new List<Instructor>();


            foreach (InstructorNotAvailablePeriod insNotAvailable in instructorNotAvailablePeriods)
            {

                // System.Diagnostics.Debug.WriteLine("ctgNotAvailableID: " + ctgNotAvailable.CottageId.ToString());
                foreach (Instructor ins in instructors)
                {
                    //  System.Diagnostics.Debug.WriteLine("VikendicaID: " + ctg.Id.ToString());
                    if (insNotAvailable.InstructorId != null)
                    {
                        if (ins.Id == Guid.Parse(insNotAvailable.InstructorId))
                        {
                            if ((insNotAvailable.StartTime >= StartTime && insNotAvailable.StartTime <= EndDate) && insNotAvailable.EndTime >= EndDate)
                            {
                                //  cottageNotAvailablePeriods.Remove(ctgNotAvailable);
                            }
                            else if ((insNotAvailable.EndTime >= StartTime && insNotAvailable.EndTime <= EndDate) && insNotAvailable.StartTime <= StartTime)
                            {
                                // cottageNotAvailablePeriods.Remove(ctgNotAvailable);
                            }
                            else
                            {
                                if (!filteredInstructors.Contains(ins))
                                {
                                    filteredInstructors.Add(ins);
                                }
                            }

                        }
                        else
                        {
                            if (!filteredInstructors.Contains(ins))
                            {
                                filteredInstructors.Add(ins);
                            }
                        }
                    }

                }
            }
            ViewData["StartDate"] = StartTime;
            ViewData["EndDate"] = EndDate;

            List<UserDetails> userIns = await _context.UserDetails.ToListAsync();

            var inses = filteredInstructors;

            List<UserDetails> users = new List<UserDetails>();
            foreach (Instructor instructor in inses)
            {
                Guid guid = new Guid(instructor.UserDetailsId);
                UserDetails user = _context.UserDetails.Where(m => m.Id == guid).FirstOrDefault<UserDetails>();
                foreach (var userIn in userIns)
                {
                    if (userIn.Id == user.Id && !users.Contains(user))
                    {
                        users.Add(user);
                    }
                }
            }
            ViewData["UserInstructors"] = users;

            return View(filteredInstructors);
        }
        [HttpGet("/Adventures/FinishAdventureReservation")]
        public async Task<IActionResult> FinishAdventureReservation(Guid? adventureId)
        {
            if (adventureId == null)
            {
                return NotFound();
            }

            AdventureDTO dto = _adventureService.GetAdventureDto((Guid)adventureId);
            if (dto == null)
            {
                return NotFound();
            }
            adventureImages = _adventureService.GetAdventureImages(adventureId ?? throw new NullReferenceException()).ToList();
            ViewData["AdventureImages"] = adventureImages;
            return View(dto);

        }
    }
}
