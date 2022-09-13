using McAttributes.Data;
using McAttributes.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace McAttributes.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeIdController : ControllerBase
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
        [EnableQuery]
        public IEnumerable<EmployeeIdRecord> Get()
        {
            return _employeeIds.AsQueryable();
        }

        // GET api/<EmployeeIdController>/5
        [HttpGet("{id}")]
        [EnableQuery]
        public IQueryable<EmployeeIdRecord> Get(string id) => from eid in _employeeIds
                                            where (eid.CloudSourceAnchor.Equals(id, StringComparison.CurrentCultureIgnoreCase))
                                            || eid.UserPrincipalName.Equals(id, StringComparison.CurrentCultureIgnoreCase)
                                            select eid;

        // POST api/<EmployeeIdController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<EmployeeIdController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }
    }
}
