﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Hooking.Data;
using Hooking.Models;

namespace Hooking.Controllers
{
    public class CottageReviewsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CottageReviewsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: CottageReviews
        public async Task<IActionResult> Index()
        {
            return View(await _context.CottageReview.ToListAsync());
        }

        // GET: CottageReviews/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cottageReview = await _context.CottageReview
                .FirstOrDefaultAsync(m => m.Id == id);
            if (cottageReview == null)
            {
                return NotFound();
            }

            return View(cottageReview);
        }

        // GET: CottageReviews/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: CottageReviews/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CottageId,UserDetailsId,Review,Grade,IsApproved,Id,RowVersion")] CottageReview cottageReview)
        {
            if (ModelState.IsValid)
            {
                cottageReview.Id = Guid.NewGuid();
                _context.Add(cottageReview);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(cottageReview);
        }

        // GET: CottageReviews/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cottageReview = await _context.CottageReview.FindAsync(id);
            if (cottageReview == null)
            {
                return NotFound();
            }
            return View(cottageReview);
        }

        // POST: CottageReviews/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("CottageId,UserDetailsId,Review,Grade,IsApproved,Id,RowVersion")] CottageReview cottageReview)
        {
            if (id != cottageReview.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(cottageReview);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CottageReviewExists(cottageReview.Id))
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
            return View(cottageReview);
        }

        // GET: CottageReviews/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cottageReview = await _context.CottageReview
                .FirstOrDefaultAsync(m => m.Id == id);
            if (cottageReview == null)
            {
                return NotFound();
            }

            return View(cottageReview);
        }

        // POST: CottageReviews/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var cottageReview = await _context.CottageReview.FindAsync(id);
            _context.CottageReview.Remove(cottageReview);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CottageReviewExists(Guid id)
        {
            return _context.CottageReview.Any(e => e.Id == id);
        }
    }
}