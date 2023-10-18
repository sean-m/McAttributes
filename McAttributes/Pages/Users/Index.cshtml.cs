﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using McAttributes.Data;
using McAttributes.Models;
using System.Linq.Expressions;
using McRule;
using Microsoft.Extensions.Azure;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.RegularExpressions;

using static SMM.FilterPatternHelpers;

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

        private Expression<Func<User,bool>> GetUserFilter () {
            if (String.IsNullOrEmpty(SearchCriteria)) return PredicateBuilder.False<User>();

            var filter = new ExpressionRuleCollection();
            filter.RuleOperator = RuleOperator.Or;
            var _rules = new List<IExpressionPolicy>();

            // If the search criteria contains spaces, it's likely the intent is to match against display name,
            // as it's the only property where users likely have spaces in the value.
            if (SearchCriteria.Trim().Contains(' ')) {
                _rules.Add(
                    new ExpressionRule((nameof(Models.User), nameof(Models.User.DisplayName), SearchCriteria.AddFilterOptionsIfNotSpecified(FilterOptions.Contains | FilterOptions.IgnoreCase)))
                );
            } else {
                _rules.AddRange(new[] {
                    new ExpressionRule((nameof(Models.User), nameof(Models.User.Mail), SearchCriteria.AddFilterOptionsIfNotSpecified(FilterOptions.StartsWith | FilterOptions.IgnoreCase))),
                    new ExpressionRule((nameof(Models.User), nameof(Models.User.EmployeeId), SearchCriteria.AddFilterOptionsIfNotSpecified(FilterOptions.StartsWith | FilterOptions.IgnoreCase))),
                    new ExpressionRule((nameof(Models.User), nameof(Models.User.PreferredGivenName), SearchCriteria.AddFilterOptionsIfNotSpecified(FilterOptions.StartsWith | FilterOptions.IgnoreCase))),
                    new ExpressionRule((nameof(Models.User), nameof(Models.User.PreferredSurname), SearchCriteria.AddFilterOptionsIfNotSpecified(FilterOptions.StartsWith | FilterOptions.IgnoreCase))),
                });
            }
            filter.Rules = _rules;

            var efGenerator = new SMM.NpgsqlGenerator();
            return efGenerator.GetPredicateExpression<User>((IExpressionRuleCollection)filter) ?? PredicateBuilder.False<User>();
        }

    }
}
