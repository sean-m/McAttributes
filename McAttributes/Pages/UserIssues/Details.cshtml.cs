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
    public class DetailsModel : PageModel
    {
        private readonly McAttributes.Data.IdDbContext _context;

        public DetailsModel(McAttributes.Data.IdDbContext context)
        {
            _context = context;
        }

      public IssueLogEntry IssueLogEntry { get; set; } = default!; 

        public async Task<IActionResult> OnGetAsync(uint? id)
        {
            if (id == null || _context.IssueLogEntry == null)
            {
                return NotFound();
            }

            var issuelogentry = await _context.IssueLogEntry.FirstOrDefaultAsync(m => m.Id == id);
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
    }
}
