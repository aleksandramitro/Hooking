using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hooking.Data;
using Hooking.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Hooking.Areas.Identity.Pages.Account.Manage
{
    public class CottageReportsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
       
        public CottageReportsModel(ApplicationDbContext context,
                                    UserManager<IdentityUser> userManager
                                    )
        {
            _context = context;
            _userManager = userManager;
          
        }
        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            List<Cottage> cottages = _context.Cottage.Where(m => m.CottageOwnerId == user.Id).ToList();
            double totalIncome = 0;
            foreach(Cottage cottage in cottages)
            {
                string cottageId = cottage.Id.ToString();
                List<CottageReservation> cottageReservations = _context.CottageReservation.Where(m => m.CottageId == cottageId).ToList();
                foreach(CottageReservation cottageReservation in cottageReservations)
                {
                    if(cottageReservation.EndDate <= DateTime.Now)
                    {
                        totalIncome += cottageReservation.Price;
                    }
                }
                List<CottageSpecialOffer> cottageSpecialOffers = _context.CottageSpecialOffer.Where(m => m.CottageId == cottageId).ToList();
                foreach(CottageSpecialOffer cottageSpecialOffer in cottageSpecialOffers)
                {
                    if(cottageSpecialOffer.EndDate <= DateTime.Now && cottageSpecialOffer.IsReserved == true)
                    {
                        totalIncome += cottageSpecialOffer.Price;
                    }
                }
            }
            ViewData["TotalIncome"] = totalIncome;
            return Page();
        }
    }
}
