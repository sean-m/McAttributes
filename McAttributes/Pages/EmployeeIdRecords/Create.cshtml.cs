using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using McAttributes.Data;
using McAttributes.Models;

namespace McAttributes.Pages.EmployeeIdRecords
{
    public class CreateModel : PageModel
    {
        private readonly McAttributes.Data.IdDbContext _context;

        public CreateModel(McAttributes.Data.IdDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public EmployeeIdRecord EmployeeIdRecord { get; set; } = default!;
        

        // To protect from overposting attacks, see https://aka.ms/RazorPagesCRUD
        public async Task<IActionResult> OnPostAsync()
        {
          if (!ModelState.IsValid || _context.EmployeeIds == null || EmployeeIdRecord == null)
            {
                return Page();
            }

            _context.EmployeeIds.Add(EmployeeIdRecord);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
