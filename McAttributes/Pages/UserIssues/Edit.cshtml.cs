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

namespace McAttributes.Pages.UserIssues
{
    public class EditModel : PageModel
    {
        private readonly McAttributes.Data.IdDbContext _context;

        public EditModel(McAttributes.Data.IdDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public IssueLogEntry IssueLogEntry { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(uint? id)
        {
            if (id == null || _context.IssueLogEntry == null)
            {
                return NotFound();
            }

            var issuelogentry =  await _context.IssueLogEntry.FirstOrDefaultAsync(m => m.Id == id);
            if (issuelogentry == null)
            {
                return NotFound();
            }
            IssueLogEntry = issuelogentry;
            return Page();
        }

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Attach(IssueLogEntry).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!IssueLogEntryExists(IssueLogEntry.Id))
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

        private bool IssueLogEntryExists(uint id)
        {
          return (_context.IssueLogEntry?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
