using McAttributes.Data;
using McAttributes.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.EntityFrameworkCore;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace McAttributes.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EmployeeIdController : ODataController
    {
        private ILogger _logger;
        private DbContext _ctx;
        private readonly DbSet<EmployeeIdRecord> _employeeIds;
        public EmployeeIdController(ILogger<UserController> logger, IdDbContext dbContext)
        {
            _logger = logger;
            _ctx = dbContext;
            _employeeIds = dbContext.Set<EmployeeIdRecord>();
        }

        // GET: api/<EmployeeIdController>
        [HttpGet]
        [EnableQuery(PageSize = 100)]
        public IEnumerable<EmployeeIdRecord> Get()
        {
            return _employeeIds;
        }

        // GET api/<EmployeeIdController>/5
        [HttpGet("{id}")]
        [EnableQuery]
        public IQueryable<EmployeeIdRecord> Get(string id) 
            => from eid in _employeeIds
            where (eid.CloudSourceAnchor.Equals(id, StringComparison.CurrentCultureIgnoreCase))
            || eid.UserPrincipalName.Equals(id, StringComparison.CurrentCultureIgnoreCase)
            select eid;

        // POST api/<EmployeeIdController>
        [HttpPost]
        public void Post([FromBody] EmployeeIdRecord value)
        {
            _employeeIds.Add(value);
            _ctx.SaveChanges();
        }

        // PUT api/<EmployeeIdController>/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] EmployeeIdRecord value)
        {
            if (id != value.Id)
                return BadRequest($"Specified id: {id} and entity id: {value.Id} do not match.");

            _ctx.Entry(value).State = EntityState.Modified;

            try
            {
                await _ctx.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EmployeeIdRecordExists(id))
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

        private bool EmployeeIdRecordExists(int id)
        {
            return (_employeeIds?.Any(x => x.Id == id)).GetValueOrDefault();
        }
    }
}
