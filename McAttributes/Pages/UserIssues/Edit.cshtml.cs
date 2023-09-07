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
        public IssueLogEntry Entry { get; set; } = default!;

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
                var _entry = _context.IssueLogEntry.Attach(Entry);
                _entry.State = EntityState.Unchanged;

                var _dbValues = await _entry.GetDatabaseValuesAsync();
                
                foreach (var propName in _entry.Properties.Select(x => x.Metadata.Name)) {
                    var _member = _entry.Members.FirstOrDefault(x => x.Metadata.Name == propName);
                    
                    // We don't want to flag disallowed values as modified.
                    if (!allowUpdate.Contains(propName)) { 
                        _member.IsModified = false;
                        continue;
                    }

                    dynamic dbVal;
                    if (_dbValues?.TryGetValue<dynamic>(propName, out dbVal) ?? false) {
                        if (!Equals(_member?.CurrentValue, dbVal)) {
                            _member.IsModified = true;
                        }
                    }
                }
                

                try {
                    if (_entry.State == EntityState.Modified) {
                        var changes = _context.ChangeTracker;
                        System.Diagnostics.Trace.WriteLine($">> {changes.ToDebugString()}");
                        _context.SaveChanges();
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
          return (_context.IssueLogEntry?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
