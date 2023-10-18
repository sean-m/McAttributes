using McAttributes.Models;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.OData.Query;
using McAttributes.Data;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Authorization;

namespace McAttributes.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class IssueLogEntryController : ODataController
    {
        private readonly IdDbContext _ctx;

        public IssueLogEntryController(ILogger<IssueLogEntryController> logger, IdDbContext context)
        {
            _ctx = context;
        }

        // GET: api/IssueLogEntries
        [HttpGet]
        [EnableQuery(PageSize = 100)]
        public IQueryable<AlertLogEntry> Get()
        {
            return _ctx.Set<AlertLogEntry>();
        }

        // PUT: api/IssueLogEntries/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, AlertLogEntry issueLogEntry)
        {
            if (id != issueLogEntry.Id)
            {
                return BadRequest($"IssueLogEntry record id {issueLogEntry?.Id} does not match specified record id: {id}");
            }

            _ctx.Entry(issueLogEntry).State = EntityState.Modified;

            try
            {
                await _ctx.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!IssueLogEntryExists(id))
                {
                    return NotFound($"Record with id: {id} not found.");
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // PUT: api/IssueLogEntries/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> Patch(int id, Dictionary<string,object> value) {

            var entry = _ctx.Set<AlertLogEntry>().First(x => x.Id == id);
            if (entry == null) return NotFound();

            // Loop through properties on the model and update them if
            // they exist in the patch value and differ from the database entry.
            var properties = typeof(AlertLogEntry).GetProperties();
            foreach (var property in properties) {
                // Cannot update the id, created or alertHash value of the model.
                if (property.Name.Equals("Id", StringComparison.CurrentCultureIgnoreCase)) continue;
                if (property.Name.Equals("Created", StringComparison.CurrentCultureIgnoreCase)) continue;
                if (property.Name.Equals("AlertHash", StringComparison.CurrentCultureIgnoreCase)) continue;

                if (value.ContainsKey(property.Name) && property.GetValue(entry) != value[property.Name]) {
                    property.SetValue(entry, value[property.Name]);
                }
            }

            _ctx.Entry(entry).State = EntityState.Modified;

            try {
                await _ctx.SaveChangesAsync();
            } catch (DbUpdateConcurrencyException) {
                if (!IssueLogEntryExists(id)) {
                    return NotFound($"Record with id: {id} not found.");
                } else {
                    throw;
                }
            }

            return NoContent();
        }


        // POST: api/IssueLogEntries
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<AlertLogEntry>> PostIssueLogEntry(AlertLogEntry issueLogEntry)
        {
            if (_ctx.Set<AlertLogEntry>() == null)
            {
                return Problem("Entity set 'IssueLogContext.IssueLogEntry'  is null.");
            }
            _ctx.Set<AlertLogEntry>().Add(issueLogEntry);
            await _ctx.SaveChangesAsync();

            return CreatedAtAction("GetIssueLogEntry", new { id = issueLogEntry.Id }, issueLogEntry);
        }

        // DELETE: api/IssueLogEntries/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteIssueLogEntry(int id)
        {
            if (_ctx.Set<AlertLogEntry>() == null)
            {
                return NotFound();
            }
            var entry = await _ctx.Set<AlertLogEntry>().FindAsync(id);
            if (entry == null)
            {
                return NotFound();
            }

            _ctx.Set<AlertLogEntry>().Remove(entry);
            await _ctx.SaveChangesAsync();

            return NoContent();
        }

        private bool IssueLogEntryExists(int id)
        {
            return (_ctx.Set<AlertLogEntry>()?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
