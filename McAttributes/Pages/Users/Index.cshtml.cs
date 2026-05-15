using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using McAttributes.Data;
using McAttributes.Models;
using System.Linq.Expressions;

namespace McAttributes.Pages.Users
{
    public class IndexModel : PageModel
    {
        private readonly IdDbContext _context;

        public IndexModel(IdDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string SearchCriteria => (string)TempData[nameof(SearchCriteria)] ?? String.Empty;

        public IList<User> UserSet { get;set; } = default!;

        int pageSize = 50;
        [BindProperty]
        public int? Page => int.Parse((string)TempData[nameof(Page)] ?? "1");

        public int Count { get; set; }

        public async Task OnGetAsync()
        {
            if (_context.Users != null)
            {
                var searchFilter = GetUserFilter();
                UserSet = await _context.Users.Where(searchFilter).Skip(Math.Max(0, (Page ?? 1) - 1) * pageSize).OrderBy(x => x.LastFetched).Take(pageSize).ToListAsync();
                if (!string.IsNullOrEmpty(SearchCriteria)) { Count = _context.Users.Count(GetUserFilter()); }
            }
        }

        public IActionResult OnPost([FromForm] string SearchCriteria, int Page) {
            TempData[nameof(SearchCriteria)] = SearchCriteria;
            TempData[nameof(Page)] = Page.ToString();

            return RedirectToPage("Index");
        }

        private Expression<Func<User, bool>> GetUserFilter() {
            if (String.IsNullOrEmpty(SearchCriteria)) return x => true;

            var searchTerm = SearchCriteria.Trim().ToLower();

            return user => 
                (user.DisplayName != null && user.DisplayName.ToLower().Contains(searchTerm)) ||
                (user.Mail != null && user.Mail.ToLower().Contains(searchTerm)) ||
                (user.Upn != null && user.Upn.ToLower().Contains(searchTerm)) ||
                (user.EmployeeId != null && user.EmployeeId.ToLower().Contains(searchTerm)) ||
                (user.PreferredGivenName != null && user.PreferredGivenName.ToLower().Contains(searchTerm)) ||
                (user.PreferredSurname != null && user.PreferredSurname.ToLower().Contains(searchTerm));
        }
    }
}
