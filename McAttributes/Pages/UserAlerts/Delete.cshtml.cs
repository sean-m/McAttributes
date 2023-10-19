using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using McAttributes.Data;
using McAttributes.Models;

namespace McAttributes.Pages.UserIssues
{
    public class DeleteModel : PageModel
    {
        private readonly McAttributes.Data.IdDbContext _context;

        public DeleteModel(McAttributes.Data.IdDbContext context)
        {
            _context = context;
        }

        [BindProperty]
      public AlertLogEntry IssueLogEntry { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(uint? id)
        {
            if (id == null || _context.AlertLogEntry == null)
            {
                return NotFound();
            }

            var issuelogentry = await _context.AlertLogEntry.FirstOrDefaultAsync(m => m.Id == id);

            if (issuelogentry == null)
            {
                return NotFound();
            }
            else 
            {
                IssueLogEntry = issuelogentry;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(uint? id)
        {
            if (id == null || _context.AlertLogEntry == null)
            {
                return NotFound();
            }
            var issuelogentry = await _context.AlertLogEntry.FindAsync(id);

            if (issuelogentry != null)
            {
                IssueLogEntry = issuelogentry;
                _context.AlertLogEntry.Remove(IssueLogEntry);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
