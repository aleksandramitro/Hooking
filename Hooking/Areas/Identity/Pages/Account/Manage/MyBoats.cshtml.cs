using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hooking.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Hooking.Models;

namespace Hooking.Areas.Identity.Pages.Account.Manage
{
    public class MyBoatsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IEmailSender _emailSender;
        [BindProperty]
        public List<Boat> myBoats { get; set; }

        public MyBoatsModel(ApplicationDbContext context,
                            UserManager<IdentityUser> userManager,
                            RoleManager<IdentityRole> roleManager,
                            SignInManager<IdentityUser> signInManager,
                            IEmailSender emailSender)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _emailSender = emailSender; 
        }
        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            myBoats = await _context.Boat.Where(m => m.BoatOwnerId == user.Id).ToListAsync();
            return Page();
        }
    }
}
