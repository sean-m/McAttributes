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
using Microsoft.Extensions.Azure;
using System.Transactions;

namespace McAttributes.Pages.UserIssues
{
    public class IndexModel : PageModel
    {
        private readonly IdDbContext _context;

        public IndexModel(IdDbContext context)
        {
            _context = context;
        }

        public IList<IssueLogEntry> IssueLogEntry { get;set; } = default!;

        [BindProperty]
        public string SearchCriteria => (string)TempData[nameof(SearchCriteria)] ?? String.Empty;

        public int IssueCountTotal { get => IssueCounts.Sum(x => x.Value); }
        public Dictionary<string, int> IssueCounts { get; set; } = new Dictionary<string, int>();

        [BindProperty]
        public bool ShowReview => bool.Parse((string)TempData[nameof(ShowReview)] ?? "true");

        [BindProperty]
        public bool ShowDenied => bool.Parse((string)TempData[nameof(ShowDenied)] ?? "false");

        [BindProperty]
        public bool ShowResolved => bool.Parse((string)TempData[nameof(ShowResolved)] ?? "false");

        public async Task OnGetAsync()
        {
            if (new[] { ShowReview, ShowDenied, ShowResolved }.All(x => !x)) {
                TempData[nameof(ShowReview)] = true.ToString();
            }

            if (_context.IssueLogEntry != null)
            {
                IssueLogEntry = await _context.IssueLogEntry.OrderBy(x => x.Id).Where(GetUserFilter()).Take(20).ToListAsync();
                var s = await _context.IssueLogEntry.OrderBy(x => x.Status).GroupBy(x => x.Status, (key,values) => new { type = key, count = values.Count() }).ToListAsync();
                foreach (var record in s) {
                    IssueCounts.Add(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(record.type), record.count);
                }
            }
        }


        public IActionResult OnPost([FromForm] string SearchCriteria, bool ShowReview, bool ShowDenied, bool ShowResolved) {
            
            TempData[nameof(SearchCriteria)] = SearchCriteria;
            TempData[nameof(ShowReview)] = ShowReview.ToString();
            TempData[nameof(ShowDenied)] = ShowDenied.ToString();
            TempData[nameof(ShowResolved)] = ShowResolved.ToString();

            return RedirectToPage("Index");
        }

        private Expression<Func<IssueLogEntry, bool>> GetUserFilter() {

            var efGenerator = new SMM.NpgsqlGenerator();

            var toggleFilter = new ExpressionRuleCollection {
                RuleOperator = RuleOperator.Or
            };
            var _rules = new List<IExpressionPolicy> ();
            if (ShowReview) { _rules.Add(new ExpressionRule((nameof(Models.IssueLogEntry), nameof(Models.IssueLogEntry.Status), "review"))); }
            if (ShowDenied) { _rules.Add(new ExpressionRule((nameof(Models.IssueLogEntry), nameof(Models.IssueLogEntry.Status), "denied"))); }
            if (ShowResolved) { _rules.Add(new ExpressionRule((nameof(Models.IssueLogEntry), nameof(Models.IssueLogEntry.Status), "resolved"))); }
            toggleFilter.Rules = _rules;

            // Empty search should just return available records based on the selected search options
            if (String.IsNullOrEmpty(SearchCriteria)) {
                return efGenerator.GetPredicateExpressionOrFalse<IssueLogEntry>(toggleFilter);
            }

            string firstSearchToken = SearchCriteria.Split()?.FirstOrDefault() ?? string.Empty;
            var searchFilter = new ExpressionRuleCollection();
            searchFilter.RuleOperator = RuleOperator.Or;
            searchFilter.Rules = new List<IExpressionPolicy>() {
                new ExpressionRule((nameof(Models.IssueLogEntry), nameof(Models.IssueLogEntry.AttrName), $"employeeId:{firstSearchToken}*")),
                new ExpressionRule((nameof(Models.IssueLogEntry), nameof(Models.IssueLogEntry.Description), $"*{SearchCriteria.Trim(new[] { '*', ' ' })}*"))
            };


            var filterExpression = PredicateBuilder.And(
                efGenerator.GetPredicateExpressionOrFalse<IssueLogEntry>(toggleFilter), 
                efGenerator.GetPredicateExpressionOrFalse<IssueLogEntry>(searchFilter));
            return filterExpression;
        }
    }
}
