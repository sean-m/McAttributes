using McAttributes.Models;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.OData.Query;
using McAttributes.Data;
using Microsoft.AspNetCore.OData.Routing.Controllers;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace McAttributes.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ODataController {

        ILogger _logger;
        readonly DbContext _ctx;
        readonly DbSet<Models.User> _users;
        
        public UserController(ILogger<UserController> logger, IdDbContext dbContext) {
            _logger = logger;
            _ctx = dbContext;
            _users = _ctx.Set<Models.User>();
        }

        // GET: api/<UserController>
        [HttpGet]
        [EnableQuery(PageSize = 100)]
        public IQueryable<User> Get() {
            // TODO filter based on requestor identity
            // TODO enforce result set size
            // TODO implement iqueryable for ODATA filtering and pagination
            return _users;
        }

        // GET api/<UserController>/5
        [HttpGet("{AadId}")]
        public Models.User Get(String AadId) {
            var compare = Guid.Parse(AadId);
            var result = _users.FirstOrDefault(x => x.AadId == compare);
            return result;
        }

        // POST api/<UserController>
        [HttpPost]
        public async void Post([FromBody] User value) {
            _users.Add(value);
            await _ctx.SaveChangesAsync();
        }

        // PUT api/<UserController>/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] User value) {

            if (id != value.Id)
                return BadRequest($"Specified id: {id} and entity id: {value.Id} do not match.");

            _ctx.Entry(value).State = EntityState.Modified;

            try
            {
                await _ctx.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserRecordExists(id))
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

        private bool UserRecordExists(int id)
        {
            return (_users?.Any(x => x.Id == id)).GetValueOrDefault();
        }
    }
}
