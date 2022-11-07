using McAttributes.Models;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.OData.Query;
using McAttributes.Data;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace McAttributes.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IssueLogEntriesController : ODataController
    {
        private readonly IssueLogContext _context;

        public IssueLogEntriesController(IssueLogContext context)
        {
            _context = context;
        }

        // GET: api/IssueLogEntries
        [HttpGet]
        [EnableQuery(PageSize = 100)]
        public IQueryable<IssueLogEntry> Get()
        {
            return _context.IssueLogEntry;
        }

        // GET: api/IssueLogEntries/5
        [HttpGet("{id}")]
        public IssueLogEntry Get(int id)
        {
            var issueLogEntry = _context.IssueLogEntry.Find(id);

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

            _context.Entry(issueLogEntry).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
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

        // POST: api/IssueLogEntries
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<IssueLogEntry>> PostIssueLogEntry(IssueLogEntry issueLogEntry)
        {
          if (_context.IssueLogEntry == null)
          {
              return Problem("Entity set 'IssueLogContext.IssueLogEntry'  is null.");
          }
            _context.IssueLogEntry.Add(issueLogEntry);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetIssueLogEntry", new { id = issueLogEntry.Id }, issueLogEntry);
        }

        // DELETE: api/IssueLogEntries/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteIssueLogEntry(int id)
        {
            if (_context.IssueLogEntry == null)
            {
                return NotFound();
            }
            var issueLogEntry = await _context.IssueLogEntry.FindAsync(id);
            if (issueLogEntry == null)
            {
                return NotFound();
            }

            _context.IssueLogEntry.Remove(issueLogEntry);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool IssueLogEntryExists(int id)
        {
            return (_context.IssueLogEntry?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
