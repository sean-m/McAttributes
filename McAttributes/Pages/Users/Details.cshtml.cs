using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using McAttributes.Data;
using McAttributes.Models;

namespace McAttributes.Pages.Users
{
    public class DetailsModel : PageModel
    {
        private readonly McAttributes.Data.IdDbContext _context;

        public DetailsModel(McAttributes.Data.IdDbContext context)
        {
            _context = context;
        }

      public User User { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(string? id)
        {
            if (string.IsNullOrWhiteSpace(id) || _context.Users == null)
            {
                return NotFound();
            }

            long _id = -1;
            if (!long.TryParse(id.Trim(), out _id) || _id < 0) { return BadRequest("'id' must be a positive number that fits in a long."); }

            var user = await _context.Users.FirstOrDefaultAsync(m => m.Id == _id);
            if (user == null)
            {
                return NotFound();
            }
            else
            {
                User = user;
            }
            return Page();
        }
    }
}
