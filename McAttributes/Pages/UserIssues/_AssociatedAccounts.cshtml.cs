using McAttributes.Data;
using McAttributes.Models;
using McRule;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SMM.Helper;

namespace McAttributes.Pages.UserIssues {
    public class _AssociatedAccountsModel : PageModel
    {
        private IdDbContext _context;

        public List<string> _acctNames { get; set; } = new List<string>();

        public List<User> AssociatedUsers { get; set; } = new List<User>();

        public _AssociatedAccountsModel(IdDbContext context)
        {
            _context = context;
        }

        public async Task OnGet() {

            if (_acctNames.Count > 0) {

                var filterCollection = new ExpressionRuleCollection();
                filterCollection.RuleOperator = RuleOperator.Or;
                var rules = new List<McRule.ExpressionRule>();
                foreach (var acct in _acctNames) {
                    rules.Add((nameof(Models.User), nameof(Models.User.Upn), acct).ToFilterRule());
                }
                var filter = filterCollection.GetPredicateExpression<User>().Compile();

                AssociatedUsers.AddRange(_context.Users.OrderBy(x => x.Id).Where(filter));
            }
        }

        public void SetIssue(IssueLogEntry issueLogEntry) {
            IssueLogEntry = issueLogEntry;

            bool startAdding = false;
            foreach (var l in IssueLogEntry.Description?.Split(new char[] { '\n', '\r' })) {
                if (!String.IsNullOrEmpty(l)) {
                    var line = l.Trim();
                    if (!startAdding && line.Like("The issue involved these accounts*")) {
                        startAdding = true;
                        continue;
                    }
                    if (startAdding && line.Like("*@*")) {
                        _acctNames.Add(line);
                    }
                }
            }
        }

        public IssueLogEntry IssueLogEntry { get; set; } = default!;
    }
}
