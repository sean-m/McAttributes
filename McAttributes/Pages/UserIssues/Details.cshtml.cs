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
            AssociatedAccountsModel = new _AssociatedAccountsModel(context);
        }


        public IssueLogEntry IssueLogEntry { get; set; } = default!;


        #region AssociatedAccountData

        private List<string> accts = new List<string>();
        public _AssociatedAccountsModel AssociatedAccountsModel { get; set; }

        #endregion  AssociatedAccountData

        public async Task<IActionResult> OnGetAsync(uint? id)
        {
            if (id == null || _context.IssueLogEntry == null)
            {
                return NotFound();
            }


            var issuelogentry = await _context.IssueLogEntry.FirstOrDefaultAsync(m => m.Id == id);
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
                        continue;
                    }
                    if (startAdding && line.Like("*@*")) {
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
}
