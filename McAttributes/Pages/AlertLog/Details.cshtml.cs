using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using McAttributes.Data;
using McAttributes.Models;

namespace McAttributes.Pages.AlertLog
{
    public class DetailsModel : PageModel
    {
        private readonly McAttributes.Data.IdDbContext _context;

        public DetailsModel(McAttributes.Data.IdDbContext context)
        {
            _context = context;
        }

        public AlertLogEntry AlertLogEntry { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var alertlogentry = await _context.AlertLogEntry.FirstOrDefaultAsync(m => m.Id == id);
            if (alertlogentry == null)
            {
                return NotFound();
            }
            else
            {
                AlertLogEntry = alertlogentry;
            }
            return Page();
        }
    }
}
