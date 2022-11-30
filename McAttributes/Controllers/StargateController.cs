using McAttributes.Models;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.OData.Query;
using McAttributes.Data;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.AspNetCore.Authorization;


// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace McAttributes.Controllers {
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StargateController : ODataController {


        ILogger _logger;
        readonly DbContext _ctx;

        public StargateController(ILogger<StargateController> logger, IdDbContext dbContext) {
            _logger = logger;
            _ctx = dbContext;
        }

        // GET: api/<StargateController>
        [EnableQuery(PageSize = 100)]
        public IQueryable<Stargate> Get() {
            return _ctx.Set<Stargate>();
        }

        //TODO implement these
        // POST api/<StargateController>
        [HttpPost]
        public async Task<long> Post([FromBody] Stargate value) {
            _ctx.Set<Stargate>().Add(value);
            await _ctx.SaveChangesAsync();
            return value.Id;
        }

        // PUT api/<StargateController>/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Stargate value) {
            if (id != value.Id)
                return BadRequest($"Specified id: {id} and entity id: {value.Id} do not match.");

            _ctx.Entry(value).State = EntityState.Modified;

            try {
                await _ctx.SaveChangesAsync();
            } catch (DbUpdateConcurrencyException) {
                if (!_ctx.Set<Stargate>().Any(x => x.Id == id)) {
                    return NotFound();
                } else {
                    throw;
                }
            }

            return NoContent();
        }
    }
}
