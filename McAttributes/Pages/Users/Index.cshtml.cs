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
            if (String.IsNullOrEmpty(SearchCriteria)) return McRule.PredicateBuilder.False<User>();

            var filter = new McRule.ExpressionRuleCollection();
            filter.RuleOperator = McRule.PredicateExpressionPolicyExtensions.RuleOperator.Or;
            filter.Rules = new List<McRule.IExpressionRule>() {
                new McRule.ExpressionRule((nameof(Models.User), nameof(Models.User.Mail), SearchCriteria)),
                new McRule.ExpressionRule((nameof(Models.User), nameof(Models.User.EmployeeId), SearchCriteria)),
                new McRule.ExpressionRule((nameof(Models.User), nameof(Models.User.PreferredGivenName), SearchCriteria)),
                new McRule.ExpressionRule((nameof(Models.User), nameof(Models.User.PreferredSurname), SearchCriteria)),
            };
            
            return PredicateExpressionPolicyExtensions.GetEFPredicateExpression<User>(filter) ?? McRule.PredicateBuilder.False<User>();
        }
    }
}
