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
    public class IndexModel : PageModel
    {
        private readonly McAttributes.Data.IdDbContext _context;

        public IndexModel(McAttributes.Data.IdDbContext context)
        {
            _context = context;
        }

        public IList<EmployeeIdRecord> EmployeeIdRecord { get;set; } = default!;

        public async Task OnGetAsync()
        {
            if (_context.EmployeeIds != null)
            {
                EmployeeIdRecord = await _context.EmployeeIds.ToListAsync();
            }
        }
    }
}
