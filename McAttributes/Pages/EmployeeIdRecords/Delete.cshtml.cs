using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using McAttributes.Data;
using McAttributes.Models;

namespace McAttributes.Pages.EmployeeIdRecords
{
    public class DeleteModel : PageModel
    {
        private readonly McAttributes.Data.IdDbContext _context;

        public DeleteModel(McAttributes.Data.IdDbContext context)
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

            var employeeidrecord = await _context.EmployeeIds.FirstOrDefaultAsync(m => m.Id == id);

            if (employeeidrecord == null)
            {
                return NotFound();
            }
            else 
            {
                EmployeeIdRecord = employeeidrecord;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(uint? id)
        {
            if (id == null || _context.EmployeeIds == null)
            {
                return NotFound();
            }
            var employeeidrecord = await _context.EmployeeIds.FindAsync(id);

            if (employeeidrecord != null)
            {
                EmployeeIdRecord = employeeidrecord;
                _context.EmployeeIds.Remove(EmployeeIdRecord);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
