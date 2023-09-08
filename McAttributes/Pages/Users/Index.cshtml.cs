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
using McRule;
using Microsoft.Extensions.Azure;

namespace McAttributes.Pages.Users
{
    public class IndexModel : PageModel
    {
        private readonly IdDbContext _context;

        public IndexModel(IdDbContext context)
        {
            _context = context;
        }

        public string SearchCriteria => (string)TempData[nameof(SearchCriteria)] ?? String.Empty;

        public IList<User> UserSet { get;set; } = default!;

        public async Task OnGetAsync()
        {
            if (_context.Users != null)
            {
                UserSet = await _context.Users.Where(GetUserFilter()).ToListAsync();
            }
        }

        public IActionResult OnPost([FromForm] string SearchCriteria) {
            TempData[nameof(SearchCriteria)] = SearchCriteria;
         
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
                    new ExpressionRule((nameof(Models.User), nameof(Models.User.DisplayName), $"*{SearchCriteria.Trim(new[] { '*', ' ' })}*"))
                );
            } else {
                _rules.AddRange(new[] {
                    new ExpressionRule((nameof(Models.User), nameof(Models.User.Mail), $"{SearchCriteria}*")),
                    new ExpressionRule((nameof(Models.User), nameof(Models.User.EmployeeId), $"{SearchCriteria}*")),
                    new ExpressionRule((nameof(Models.User), nameof(Models.User.PreferredGivenName), $"{SearchCriteria}*")),
                    new ExpressionRule((nameof(Models.User), nameof(Models.User.PreferredSurname), $"{SearchCriteria}*")),
                });
            }
            filter.Rules = _rules;

            var efGenerator = PredicateExpressionPolicyExtensions.GetEfExpressionGenerator();
            return efGenerator.GetPredicateExpression<User>(filter) ?? PredicateBuilder.False<User>();
        }
    }
}
