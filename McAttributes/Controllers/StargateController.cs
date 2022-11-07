using McAttributes.Models;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.OData.Query;
using McAttributes.Data;
using Microsoft.AspNetCore.OData.Routing.Controllers;


// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace McAttributes.Controllers {
    [Route("api/[controller]")]
    [ApiController]
    public class StargateController : ODataController {


        ILogger _logger;
        readonly DbContext _ctx;
        readonly DbSet<Stargate> _stargate;

        public StargateController(ILogger<StargateController> logger, IdDbContext dbContext) {
            _logger = logger;
            _ctx = dbContext;
            _stargate = _ctx.Set<Stargate>();
        }

        // GET: api/<StargateController>
        [HttpGet]
        [EnableQuery(PageSize = 100)]
        public IQueryable<Stargate> Get() {
            return _stargate;
        }

        // GET api/<StargateController>/5
        [HttpGet("{id}")]
        public Stargate Get(int id) {
           return _stargate.First(x => x.id == id);
        }

        // TODO implement these
        //// POST api/<StargateController>
        //[HttpPost]
        //public void Post([FromBody] string value) {
        //}

        //// PUT api/<StargateController>/5
        //[HttpPut("{id}")]
        //public void Put(int id, [FromBody] string value) {
        //}
    }
}
