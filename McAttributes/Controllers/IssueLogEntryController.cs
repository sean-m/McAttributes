using McAttributes.Models;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.OData.Query;
using McAttributes.Data;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Newtonsoft.Json.Linq;

namespace McAttributes.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IssueLogEntryController : ODataController
    {
        private readonly IssueLogContext _ctx;

        public IssueLogEntryController(IssueLogContext context)
        {
            _ctx = context;
        }

        // GET: api/IssueLogEntries
        [HttpGet]
        [EnableQuery(PageSize = 100)]
        public IQueryable<IssueLogEntry> Get()
        {
            return _ctx.IssueLogEntry;
        }

        // GET: api/IssueLogEntries/5
        [HttpGet("{id}")]
        public IssueLogEntry? Get(int id)
        {
            var issueLogEntry = _ctx.IssueLogEntry.Find(id);

            return issueLogEntry;
        }

        // PUT: api/IssueLogEntries/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, IssueLogEntry issueLogEntry)
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

            var entry = _ctx.Set<IssueLogEntry>().First(x => x.Id == id);
            if (entry == null) return NotFound();

            // Loop through properties on the model and update them if
            // they exist in the patch value and differ from the database entry.
            var properties = typeof(IssueLogEntry).GetProperties();
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
        public async Task<ActionResult<IssueLogEntry>> PostIssueLogEntry(IssueLogEntry issueLogEntry)
        {
          if (_ctx.IssueLogEntry == null)
          {
              return Problem("Entity set 'IssueLogContext.IssueLogEntry'  is null.");
          }
            _ctx.IssueLogEntry.Add(issueLogEntry);
            await _ctx.SaveChangesAsync();

            return CreatedAtAction("GetIssueLogEntry", new { id = issueLogEntry.Id }, issueLogEntry);
        }

        // DELETE: api/IssueLogEntries/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteIssueLogEntry(int id)
        {
            if (_ctx.IssueLogEntry == null)
            {
                return NotFound();
            }
            var issueLogEntry = await _ctx.IssueLogEntry.FindAsync(id);
            if (issueLogEntry == null)
            {
                return NotFound();
            }

            _ctx.IssueLogEntry.Remove(issueLogEntry);
            await _ctx.SaveChangesAsync();

            return NoContent();
        }

        private bool IssueLogEntryExists(int id)
        {
            return (_ctx.IssueLogEntry?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
