using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using McAttributes.Data;
using McAttributes.Models;
using SMM.Helper;
using McRule;

namespace McAttributes.Pages.UserIssues
{
    public class DetailsModel : PageModel
    {
        private readonly IdDbContext _context;

        public DetailsModel(IdDbContext context)
        {
            _context = context;
            AssociatedAccountsModel = new AssociatedAccountsModel(context);
        }


        public AlertLogEntry IssueLogEntry { get; set; } = default!;


        #region AssociatedAccountData

        public List<string> accts { get; set; } = new List<string>();
        public AssociatedAccountsModel AssociatedAccountsModel { get; set; }

        public List<User> AssociatedUsers { get; set; } = new List<User>();

        #endregion  AssociatedAccountData

        public async Task<IActionResult> OnGetAsync(uint? id)
        {
            if (id == null || _context.AlertLogEntry == null)
            {
                return NotFound();
            }


            var issuelogentry = await _context.AlertLogEntry.FirstOrDefaultAsync(m => m.Id == id);
            if (issuelogentry == null)
            {
                return NotFound();
            }
            else 
            {
                IssueLogEntry = issuelogentry;
            }


            bool startAdding = false;
            foreach (var l in IssueLogEntry.Description?.Split(new char[] { '\n', '\r' })) {
                if (!String.IsNullOrEmpty(l)) {
                    var line = l.Trim();
                    if (!startAdding && line.Like("The issue involved these accounts*")) {
                        startAdding = true;
                    }
                    if (startAdding && line.Contains("@")) {
                        accts.Add(line);
                    }
                }
            }

            if (accts.Count > 0) {

                var filterCollection = new ExpressionRuleCollection();
                filterCollection.RuleOperator = McRule.RuleOperator.Or;
                var rules = new List<ExpressionRule>();
                foreach (var acct in accts) {
                    rules.Add((nameof(Models.User), nameof(Models.User.Upn), acct).ToFilterRule());
                }
                filterCollection.Rules = rules;

                var efGenerator = PredicateExpressionPolicyExtensions.GetEfExpressionGenerator();
                var filter = efGenerator.GetPredicateExpression<Models.User>(filterCollection) ?? PredicateBuilder.False<Models.User>();

                AssociatedAccountsModel.AssociatedUsers.AddRange(_context.Users.OrderBy(x => x.Id).Where(filter));
            }

            return Page();
        }


        public IActionResult OnPost([FromForm] string foo) {


            return RedirectToPage("Index");
        }

    }

    public class AssociatedAccountsModel : PageModel {
        private IdDbContext _context;

        public List<string> _acctNames { get; set; } = new List<string>();

        public List<User> AssociatedUsers { get; set; } = new List<User>();

        public AssociatedAccountsModel(IdDbContext context) {
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


        public void SetIssue(AlertLogEntry issueLogEntry) {
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

        public AlertLogEntry IssueLogEntry { get; set; } = default!;
    }
}
