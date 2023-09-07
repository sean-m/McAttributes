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
using McRule;
using System.Linq.Expressions;

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

        public string SearchCriteria => (string)TempData[nameof(SearchCriteria)] ?? String.Empty;

        public int IssueCountTotal { get => IssueCounts.Sum(x => x.Value); }
        public Dictionary<string, int> IssueCounts { get; set; } = new Dictionary<string, int>();

        public async Task OnGetAsync()
        {
            if (_context.IssueLogEntry != null)
            {
                IssueLogEntry = await _context.IssueLogEntry.OrderBy(x => x.Id).Where(GetUserFilter()).Take(20).ToListAsync();
                var s = await _context.IssueLogEntry.OrderBy(x => x.Status).GroupBy(x => x.Status, (key,values) => new { type = key, count = values.Count() }).ToListAsync();
                foreach (var record in s) {
                    IssueCounts.Add(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(record.type), record.count);
                }
            }
        }


        public IActionResult OnPost([FromForm] string SearchCriteria) {
            TempData[nameof(SearchCriteria)] = SearchCriteria;

            return RedirectToPage("Index");
        }

        private Expression<Func<IssueLogEntry, bool>> GetUserFilter() {
            if (String.IsNullOrEmpty(SearchCriteria)) return PredicateBuilder.True<IssueLogEntry>();

            var filter = new ExpressionRuleCollection();
            filter.RuleOperator = PredicateExpressionPolicyExtensions.RuleOperator.Or;
            filter.Rules = new List<IExpressionRule>() {
                new ExpressionRule((nameof(Models.IssueLogEntry), nameof(Models.IssueLogEntry.AttrName), $"employeeId:{SearchCriteria.Split().Take(1)}*")),
                new ExpressionRule((nameof(Models.IssueLogEntry), nameof(Models.IssueLogEntry.Description), $"*{SearchCriteria.Trim(new[] { '*', ' ' })}*"))
            };

            var efExpression = PredicateExpressionPolicyExtensions.GetEfExpressionGenerator();
            return filter.GetPredicateExpression<IssueLogEntry>(efExpression) ?? PredicateBuilder.False<IssueLogEntry>();
        }
    }
}
