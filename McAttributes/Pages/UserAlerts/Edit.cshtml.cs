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
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

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
        public AlertLogEntry Entry { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(uint? id)
        {
            if (id == null || _context.AlertLogEntry == null)
            {
                return NotFound();
            }

            var issuelogentry =  await _context.AlertLogEntry.FirstOrDefaultAsync(m => m.Id == id);
            if (issuelogentry == null)
            {
                return NotFound();
            }
            Entry = issuelogentry;
            return Page();
        }

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            var allowUpdate = new HashSet<string> { "Status", "Notes" };
            
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (Entry != null) {
                var _entry = _context.AlertLogEntry.Find(Entry.Id);

                foreach (var prop in _entry?.GetType().GetProperties()) {
                    
                    // We don't want to flag disallowed values as modified.
                    if (!allowUpdate.Contains(prop.Name)) {
                        continue;
                    }

                    dynamic dbVal = prop.GetValue(_entry);
                    dynamic modelValue = prop.GetValue(Entry);
                    if (!Equals(dbVal, modelValue)) {
                        prop.SetValue(_entry, modelValue);
                    }
                }
                
                try {
                    if (_context.Entry(_entry).State == EntityState.Modified) {
                        var changes = _context.ChangeTracker;
                        System.Diagnostics.Trace.WriteLine($">> {changes.ToDebugString()}");
                        await _context.SaveChangesAsync();
                    }
                } catch (DbUpdateConcurrencyException) {
                    if (!IssueLogEntryExists(Entry.Id)) {
                        return NotFound();
                    } else {
                        throw;
                    }
                }
            }

            return RedirectToPage("./Index");
        }

        private bool IssueLogEntryExists(long id)
        {
          return (_context.AlertLogEntry?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
