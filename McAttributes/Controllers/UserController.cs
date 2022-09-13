using McAttributes.Models;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.OData.Query;
using McAttributes.Data;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace McAttributes.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase {

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
        [EnableQuery]
        public IEnumerable<User> Get() {
            // TODO filter based on requestor identity
            // TODO enforce result set size
            // TODO implement iqueryable for ODATA filtering and pagination
            return from User in _users select User;
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
        public void Post([FromBody] string value) {
        }

        // PUT api/<UserController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value) {
        }
    }
}
