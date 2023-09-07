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

namespace McAttributes.Pages.EmployeeIdRecords
{
    public class EditModel : PageModel
    {
        private readonly McAttributes.Data.IdDbContext _context;

        public EditModel(McAttributes.Data.IdDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public EmployeeIdRecord EmployeeIdRecord { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(uint? id)
        {
            if (id == null || _context.EmployeeIds == null)
            {
                return NotFound();
            }

            var employeeidrecord =  await _context.EmployeeIds.FirstOrDefaultAsync(m => m.Id == id);
            if (employeeidrecord == null)
            {
                return NotFound();
            }
            EmployeeIdRecord = employeeidrecord;
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

            _context.Attach(EmployeeIdRecord).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EmployeeIdRecordExists(EmployeeIdRecord.Id))
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

        private bool EmployeeIdRecordExists(long id)
        {
          return (_context.EmployeeIds?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
