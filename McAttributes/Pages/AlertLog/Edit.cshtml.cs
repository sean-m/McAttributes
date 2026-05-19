using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using McAttributes.Data;
using McAttributes.Models;

namespace McAttributes.Pages.AlertLog
{
    public class EditModel : PageModel
    {
        private readonly McAttributes.Data.IdDbContext _context;

        public EditModel(McAttributes.Data.IdDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public AlertLogEntry AlertLogEntry { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var alertlogentry =  await _context.AlertLogEntry.FirstOrDefaultAsync(m => m.Id == id);
            if (alertlogentry == null)
            {
                return NotFound();
            }
            AlertLogEntry = alertlogentry;
            return Page();
        }

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Attach(AlertLogEntry).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AlertLogEntryExists(AlertLogEntry.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }

        private bool AlertLogEntryExists(long id)
        {
            return _context.AlertLogEntry.Any(e => e.Id == id);
        }
    }
}
