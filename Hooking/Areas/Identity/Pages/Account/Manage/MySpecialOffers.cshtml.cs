using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hooking.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Hooking.Models;

namespace Hooking.Areas.Identity.Pages.Account.Manage
{
    public class MySpecialOffersModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IEmailSender _emailSender;
        [BindProperty]
        public List<CottageSpecialOffer> cottageSpecialOffers { get; set; }
        public List<Cottage> cottages = new List<Cottage>();
        public MySpecialOffersModel(UserManager<IdentityUser> userManager,
                                    RoleManager<IdentityRole> roleManager,
                                    SignInManager<IdentityUser> signInManager,
                                    IEmailSender emailSender,
                                    ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _context = context;
        }
        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            List<Cottage> myCottages = new List<Cottage>();
            myCottages =   _context.Cottage.Where(m => m.CottageOwnerId == user.Id).ToList();
            cottageSpecialOffers = new List<CottageSpecialOffer>();
            foreach(var cottage in myCottages)
            {
                var cottageId = cottage.Id.ToString();
                List<CottageSpecialOffer> specialOffers = _context.CottageSpecialOffer.Where(m => m.CottageId == cottageId).ToList<CottageSpecialOffer>();
                foreach(var specialOffer in specialOffers)
                {
                    if(specialOffer.StartDate >= DateTime.Now)
                    {
                        cottageSpecialOffers.Add(specialOffer);
                        Guid cottageGuid = Guid.Parse(specialOffer.CottageId);
                        var cottageSpec = _context.Cottage.Where(m => m.Id == cottageGuid).FirstOrDefault<Cottage>();
                        cottages.Add(cottageSpec);
                        ViewData["CottageName"] = cottage.Name;
                    }
                        
                    
                }
            }
            ViewData["cottages"] = cottages;
            return Page();
        }
    }
}
