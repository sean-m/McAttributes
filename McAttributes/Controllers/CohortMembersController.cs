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
    public class CohortMembersController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IdDbContext _context;

        public CohortMembersController(ILogger<CohortMembersController> logger, IdDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // GET: api/CohortMembers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CohortMember>>> GetCohortMember()
        {
          if (_context.CohortMember == null)
          {
              return NotFound();
          }
            return await _context.CohortMember.ToListAsync();
        }

        // GET: api/CohortMembers/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CohortMember>> GetCohortMember(string id)
        {
            if (_context.CohortMember == null)
            {
                return NotFound();
            }
            var cohortMember = await _context.CohortMember.FindAsync(id);

            if (cohortMember == null)
            {
                return NotFound();
            }

            return cohortMember;
        }

        // PUT: api/CohortMembers/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCohortMember(string id, CohortMember cohortMember)
        {
            if (id != cohortMember.Id)
            {
                return BadRequest();
            }

            _context.Entry(cohortMember).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CohortMemberExists(id))
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

        // POST: api/CohortMembers
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<CohortMember>> PostCohortMember(CohortMember cohortMember)
        {
          if (_context.CohortMember == null)
          {
              return Problem("Entity set 'IdDbContext.CohortMember'  is null.");
          }
            _context.CohortMember.Add(cohortMember);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetCohortMember", new { id = cohortMember.Id }, cohortMember);
        }

        // DELETE: api/CohortMembers/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCohortMember(string id)
        {
            if (_context.CohortMember == null)
            {
                return NotFound();
            }
            var cohortMember = await _context.CohortMember.FindAsync(id);
            if (cohortMember == null)
            {
                return NotFound();
            }

            _context.CohortMember.Remove(cohortMember);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CohortMemberExists(string id)
        {
            return (_context.CohortMember?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
