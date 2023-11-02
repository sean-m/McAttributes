using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using McAttributes.Data;
using McAttributes.Models;
using McAttributes.Controllers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace McAttributes.Pages.UserIssues
{
    public class DeleteModel : PageModel
    {
        private readonly McAttributes.Data.IdDbContext _context;
        ILogger _logger;

        public DeleteModel(ILogger<DeleteModel> logger, McAttributes.Data.IdDbContext context)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
      public AlertLogEntry IssueLogEntry { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(long? id)
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
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(long? id)
        {
            if (id == null || _context.AlertLogEntry == null)
            {
                return NotFound();
            }
            var issuelogentry = await _context.AlertLogEntry.FindAsync(id);

            if (issuelogentry != null)
            {
                try {
                    _logger.LogWarning($"User: {User.Identity.Name}  issuing delete on record \n{ToKvText(issuelogentry)}");
                    IssueLogEntry = issuelogentry;
                    _context.AlertLogEntry.Remove(IssueLogEntry);
                    await _context.SaveChangesAsync();
                    _logger.LogWarning($"User: {User.Identity.Name}  delete successful on record: {issuelogentry.Id}");
                } catch (Exception ex) {
                    _logger.LogWarning($"User: {User.Identity.Name}  delete unsuccesful on record: {issuelogentry.Id}");
                    throw ex;
                }
            } else {
                _logger.LogWarning($"User: {User.Identity.Name}  attempted delete on record: {id} but the record could not be found.");
            }

            return RedirectToPage("./Index");
        }

        public string ToKvText(object entry) {
            if (entry == null) return string.Empty;

            StringBuilder sb = new StringBuilder();

            var properties = entry.GetType().GetProperties();
            var longestPropertyLength = properties.Select(x => x.Name.Length).Max();
            int indentation = longestPropertyLength + 3;

            foreach (var p in properties) {
                sb.Append(p.Name.PadRight(longestPropertyLength));
                sb.Append(" : ");
                bool multiLine = false;
                foreach (var value in (p.GetValue(entry) ?? "NULL").ToString().Split("\n")) {
                    var strValue = value ?? "NULL";
                    if (multiLine) {
                        sb.AppendLine((strValue).PadLeft(indentation + strValue.Length));
                    } else {
                        sb.AppendLine((strValue).PadRight(longestPropertyLength));
                        multiLine = true;
                    }
                }
            }

            return sb.ToString();
        }
    }
}
