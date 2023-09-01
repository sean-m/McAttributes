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
    public class DetailsModel : PageModel
    {
        private readonly McAttributes.Data.IdDbContext _context;

        public DetailsModel(McAttributes.Data.IdDbContext context)
        {
            _context = context;
        }

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
    }
}
