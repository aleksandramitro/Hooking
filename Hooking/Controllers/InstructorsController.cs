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

namespace Hooking.Controllers
{
    public class InstructorsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public UserDetails user;
        private readonly UserManager<IdentityUser> _userManager;

        public InstructorsController(ApplicationDbContext context, 
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Instructors
        public async Task<IActionResult> Index(string searchString="", string sortOrder="", bool triedToDelete=false)
        {
           
            var ins = from b in _context.UserDetails
                      select b;
            if (!String.IsNullOrEmpty(searchString))
            {
                ins = ins.Where(s => s.FirstName.Contains(searchString)
                                       || s.City.Contains(searchString) || s.Country.Contains(searchString) || s.LastName.Contains(searchString)
                                       || s.Address.Contains(searchString));
            }

            switch (sortOrder)
            {
                case "FirstName":
                    ins = ins.OrderBy(b => b.FirstName);
                    break;
                case "Address":
                    ins = ins.OrderBy(b => b.Address);
                    break;
                case "City":
                    ins = ins.OrderBy(b => b.City);
                    break;
                case "Country":
                    ins = ins.OrderBy(b => b.Country);
                    break;
                case "LastName":
                    ins = ins.OrderBy(b => b.FirstName);
                    break;


            }
            List<UserDetails> userIns = await ins.AsNoTracking().ToListAsync();

            var instructors = await _context.Instructor.ToListAsync();
            List<UserDetails> users = new List<UserDetails>();
            foreach (Instructor instructor in instructors)
            {
                Guid guid = new Guid(instructor.UserDetailsId);
                UserDetails user = _context.UserDetails.Where(m => m.Id == guid).FirstOrDefault<UserDetails>();
                foreach(var userIn in userIns)
                {
                    if(userIn.Id==user.Id && !users.Contains(user))
                    {
                        users.Add(user);
                    }
                }
            }
            ViewData["UserInstructors"] = users;

            if (triedToDelete)
            {
                ViewData["StatusMessage"] = "Nije moguće obrisati izabranog instruktora jer ima rezervisane avanture.";
            }

            return View(await _context.Instructor.ToListAsync());
        }

        // GET: Instructors/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var instructor = await _context.Instructor
                .FirstOrDefaultAsync(m => m.Id == id);
            if (instructor == null)
            {
                return NotFound();
            }
            Guid userInstructorId = Guid.Parse(instructor.UserDetailsId);
            var userInstructor = _context.UserDetails.Where(m => m.Id == userInstructorId).FirstOrDefault<UserDetails>();
            var allAdventures = await _context.Adventure.ToListAsync();
            List<Adventure> adventures = new List<Adventure>();
            foreach (Adventure adventure in allAdventures)
            {
                Guid guid = new Guid(adventure.InstructorId);
                if(guid==id)
                {
                    adventures.Add(adventure);
                }
               // Adventure a = _context.Adventure.Where(m => m.InstructorId == adventure.InstructorId).FirstOrDefault<Adventure>();
              
            }
            ViewData["UserInstructor"] = userInstructor;
            ViewData["InstructorsAdventures"] = adventures;


            return View(instructor);
        }

        // GET: Instructors/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Instructors/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserDetailsId,AverageGrade,GradeCount,Biography,Id,RowVersion")] Instructor instructor)
        {
            if (ModelState.IsValid)
            {
                instructor.Id = Guid.NewGuid();
                _context.Add(instructor);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(instructor);
        }

        // GET: Instructors/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var instructor = await _context.Instructor.FindAsync(id);
            if (instructor == null)
            {
                return NotFound();
            }
            return View(instructor);
        }

        // POST: Instructors/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("UserDetailsId,AverageGrade,GradeCount,Biography,Id,RowVersion")] Instructor instructor)
        {
            if (id != instructor.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(instructor);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InstructorExists(instructor.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(instructor);
        }

        // GET: Instructors/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var instructor = await _context.Instructor
                .FirstOrDefaultAsync(m => m.Id == id);
            if (instructor == null)
            {
                return NotFound();
            }

            List<Adventure> adventures = _context.Adventure.Where(a => a.InstructorId == id.ToString()).ToList();
            List<AdventureRealisation> realizations = new List<AdventureRealisation>();
            foreach (Adventure adventure in adventures)
            {
                foreach (AdventureRealisation realization in _context.AdventureRealisation.Where(r =>
                    r.AdventureId == adventure.Id.ToString()))
                {
                    realizations.Add(realization);
                }
            }

            List<AdventureReservation> reservations = new List<AdventureReservation>();
            foreach (AdventureRealisation realization in realizations)
            {
                foreach (AdventureReservation reservation in _context.AdventureReservation.Where(r =>
                    r.AdventureRealisationId == realization.Id.ToString()))
                {
                    reservations.Add(reservation);
                }
            }

            if (reservations.Count != 0)
            {
                return RedirectToAction(nameof(Index), new { triedToDelete = true });
            }

            return View(instructor);
        }

        // POST: Instructors/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            

            var instructor = await _context.Instructor.FindAsync(id);
            _context.Instructor.Remove(instructor);
            UserDetails userDetails = _context.UserDetails.Find(Guid.Parse(instructor.UserDetailsId));
            _context.UserDetails.Remove(userDetails);
            IdentityUser identityUser = await _userManager.FindByIdAsync(userDetails.IdentityUserId);
            await _userManager.DeleteAsync(identityUser);

            List<Adventure> adventures = _context.Adventure.Where(a => a.InstructorId == id.ToString()).ToList();

            foreach (Adventure adventure in adventures)
            {
                foreach(AdventureRealisation realization in _context.AdventureRealisation.Where(r => r.AdventureId == adventure.Id.ToString()))
                {
                    _context.Remove(realization);
                }
                _context.Remove(adventure);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool InstructorExists(Guid id)
        {
            return _context.Instructor.Any(e => e.Id == id);
        }
    }
}
