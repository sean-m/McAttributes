using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using McAttributes.Data;
using McAttributes.Models;
using Microsoft.Identity.Client;
using SMM.Helper;
using System.Globalization;

namespace McAttributes.Pages.UserIssues
{
    public class IndexModel : PageModel
    {
        private readonly McAttributes.Data.IdDbContext _context;

        public IndexModel(McAttributes.Data.IdDbContext context)
        {
            _context = context;
        }

        public IList<IssueLogEntry> IssueLogEntry { get;set; } = default!;

        public int IssueCountTotal { get; set; }
        public Dictionary<string, int> IssueCounts { get; set; } = new Dictionary<string, int>();

        public async Task OnGetAsync()
        {
            if (_context.IssueLogEntry != null)
            {
                IssueLogEntry = await _context.IssueLogEntry.Take(20).ToListAsync();
                var s = await _context.IssueLogEntry.OrderBy(x => x.Status).GroupBy(x => x.Status, (key,values) => new { type = key, count = values.Count() }).ToListAsync();
                foreach (var record in s) {
                    IssueCounts.Add(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(record.type), record.count);
                }
                IssueCountTotal = s.Sum(x => x.count);
            }
        }
    }
}
