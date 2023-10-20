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
using SMM;

using static SMM.FilterPatternHelpers;

namespace McAttributes.Pages.UserIssues
{
    public class IndexModel : PageModel
    {
        private readonly IdDbContext _context;

        public IndexModel(IdDbContext context)
        {
            _context = context;
        }

        public IList<AlertLogEntry> IssueLogEntry { get;set; } = default!;

        [BindProperty]
        public string SearchCriteria => (string)TempData[nameof(SearchCriteria)] ?? String.Empty;

        public int IssueCountTotal { get => IssueCounts.Sum(x => x.Value); }
        public Dictionary<string, int> IssueCounts { get; set; } = new Dictionary<string, int>();

        [BindProperty]
        public bool ShowReview => bool.Parse((string)TempData[nameof(ShowReview)] ?? "true");

        [BindProperty]
        public bool ShowDenied => bool.Parse((string)TempData[nameof(ShowDenied)] ?? "false");

        [BindProperty]
        public bool ShowItsFine => bool.Parse((string)TempData[nameof(ShowItsFine)] ?? "false");

        [BindProperty]
        public bool ShowResolved => bool.Parse((string)TempData[nameof(ShowResolved)] ?? "false");


        [BindProperty]
        public int? Page => int.Parse((string)TempData[nameof(Page)] ?? "1");

        private int pageSize = 50;

        public async Task OnGetAsync()
        {
            if (new[] { ShowReview, ShowDenied, ShowResolved }.All(x => !x)) {
                TempData[nameof(ShowReview)] = true.ToString();
            }

            if (_context.AlertLogEntry != null)
            {
                var issueFilter = GetUserFilter();
                IssueLogEntry = await _context.AlertLogEntry.OrderBy(x => x.Id).Where(issueFilter).Skip(Math.Max(0,(Page ?? 1) - 1) * pageSize).Take(pageSize).ToListAsync();
                var s = await _context.AlertLogEntry.OrderBy(x => x.Status).GroupBy(x => x.Status, (key,values) => new { type = key, count = values.Count() }).ToListAsync();
                foreach (var record in s) {
                    IssueCounts.Add(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(record.type), record.count);
                }
            }
        }


        public IActionResult OnPost([FromForm] string SearchCriteria, bool ShowReview, bool ShowDenied, bool ShowItsFine, bool ShowResolved, int Page) {
            
            TempData[nameof(SearchCriteria)] = SearchCriteria;
            TempData[nameof(ShowReview)] = ShowReview.ToString();
            TempData[nameof(ShowDenied)] = ShowDenied.ToString();
            TempData[nameof(ShowItsFine)] = ShowItsFine.ToString();
            TempData[nameof(ShowResolved)] = ShowResolved.ToString();
            TempData[nameof(Page)] = Page.ToString();
            return RedirectToPage("Index");
        }

        private Expression<Func<AlertLogEntry, bool>> GetUserFilter() {

            var efGenerator = new SMM.NpgsqlGenerator();

            var toggleFilter = new ExpressionRuleCollection {
                RuleOperator = RuleOperator.Or
            };
            var _rules = new List<IExpressionPolicy> ();
            if (ShowReview) { _rules.Add(new ExpressionRule((nameof(Models.AlertLogEntry), nameof(Models.AlertLogEntry.Status), "review"))); }
            if (ShowDenied) { _rules.Add(new ExpressionRule((nameof(Models.AlertLogEntry), nameof(Models.AlertLogEntry.Status), "denied"))); }
            if (ShowResolved) { _rules.Add(new ExpressionRule((nameof(Models.AlertLogEntry), nameof(Models.AlertLogEntry.Status), "resolved"))); }
            toggleFilter.Rules = _rules;

            // Empty search should just return available records based on the selected search options
            if (String.IsNullOrEmpty(SearchCriteria)) {
                return efGenerator.GetPredicateExpressionOrFalse<AlertLogEntry>(toggleFilter);
            }

            string firstSearchToken = SearchCriteria.Split()?.FirstOrDefault() ?? string.Empty;
            var searchFilter = new ExpressionRuleCollection();
            searchFilter.RuleOperator = RuleOperator.Or;
            searchFilter.Rules = new List<IExpressionPolicy>() {
                new ExpressionRule((nameof(Models.AlertLogEntry), nameof(Models.AlertLogEntry.AttrName), $"employeeId:{firstSearchToken}".AddFilterOptionsIfNotSpecified(FilterOptions.IgnoreCase | FilterOptions.StartsWith))),
                new ExpressionRule((nameof(Models.AlertLogEntry), nameof(Models.AlertLogEntry.Description), SearchCriteria.AddFilterOptionsIfNotSpecified(FilterOptions.Contains | FilterOptions.IgnoreCase)))
            };


            var filterExpression = PredicateBuilder.And(
                efGenerator.GetPredicateExpressionOrFalse<AlertLogEntry>(toggleFilter), 
                efGenerator.GetPredicateExpressionOrFalse<AlertLogEntry>(searchFilter));
            return filterExpression;
        }
    }
}
