using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using McAttributes.Data;
using McAttributes.Models;

namespace McAttributes.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IssueLogEntriesController : ControllerBase
    {
        private readonly IssueLogContext _context;

        public IssueLogEntriesController(IssueLogContext context)
        {
            _context = context;
        }

        // GET: api/IssueLogEntries
        [HttpGet]
        public async Task<ActionResult<IEnumerable<IssueLogEntry>>> GetIssueLogEntry()
        {
          if (_context.IssueLogEntry == null)
          {
              return NotFound();
          }
            return await _context.IssueLogEntry.ToListAsync();
        }

        // GET: api/IssueLogEntries/5
        [HttpGet("{id}")]
        public async Task<ActionResult<IssueLogEntry>> GetIssueLogEntry(int id)
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

            return issueLogEntry;
        }

        // PUT: api/IssueLogEntries/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutIssueLogEntry(int id, IssueLogEntry issueLogEntry)
        {
            if (id != issueLogEntry.Id)
            {
                return BadRequest();
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
                    return NotFound();
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
