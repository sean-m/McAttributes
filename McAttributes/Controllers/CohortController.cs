using McAttributes.Data;
using McAttributes.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Swashbuckle.AspNetCore.SwaggerGen;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace McAttributes.Controllers {
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CohortController : ODataController {

        ILogger _logger;
        readonly DbContext _ctx;

        public CohortController(ILogger<CohortController> logger, IdDbContext dbContext) {
            _logger = logger;
            _ctx = dbContext;
        }

        // GET api/<CohortController>/5
        [HttpGet("{id}")]
        public IActionResult Get([FromRoute] string id) {
            //if (id == null) throw new ArgumentNullException("id");
            long longId = -1;
            if (id is string memberId) {
                if (string.IsNullOrEmpty(memberId)) {
                    return NoContent();
                }

                var result = _ctx.Set<CohortDescription>().Where(x => x.CohortMembers.Any(m => m.Id == memberId)).ToList();

                if (result == null) {
                    return NotFound($"Cohort with id {memberId} not found.");
                }

                return Ok(result);
            } else if (long.TryParse(id, out longId)) {
                var result = _ctx.Set<CohortDescription>().Where(x => x.Id == longId).ToList();
                if (result == null) {
                    return NotFound($"Cohort with id {longId} not found.");
                }

                return Ok(result);
            }
            
            return BadRequest($"id {id} cannot resolve against any cohort ids or memberIds.");
        }

        // POST api/<CohortController>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CohortDescription value) {
            if (value.Id != null) {
                long _id = (long)value.Id;
                if (Exists(_id)) {
                    return BadRequest($"Cohort description with id {_id} already exists. If updating, use PUT method. If creating, id must be null.");
                }
            }

            var validation = value.Validate();
            if (!validation.Succeeded) {
                return BadRequest(validation.Message);
            }

            _ctx.Add<CohortDescription>(value);
            await _ctx.SaveChangesAsync();
            return Ok(value.Id);
        }

        // PUT api/<CohortController>/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(long id, [FromBody] CohortDescription value) {

            if (id != value.Id) {
                throw new BadHttpRequestException($"Specified id: {id} and entity id: {value.Id} do not match.");
            }

            var validation = value.Validate();
            if (!validation.Succeeded) {
                throw new BadHttpRequestException(validation.Message);
            }

            _ctx.Entry(value).State = EntityState.Modified;

            try {
                await _ctx.SaveChangesAsync();
            } catch (DbUpdateConcurrencyException) {
                if (!Exists(value.Id)) {
                    throw new HttpRequestException($"Cohort with id {id} not found.", null, System.Net.HttpStatusCode.NotFound);
                } else {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(long id, [FromBody] Dictionary<string, object> value) {

            var entry = _ctx.Set<User>().First(x => x.Id == id);
            if (entry == null) throw new HttpRequestException($"Cohort with id {id} not found.", null, System.Net.HttpStatusCode.NotFound);

            // Loop through properties on the model and update them if
            // they exist in the patch value and differ from the database entry.
            var properties = typeof(User).GetProperties();
            foreach (var property in properties) {
                if (property.Name == "Id") continue;
                if (value.ContainsKey(property.Name) && property.GetValue(entry) != value[property.Name]) {
                    dynamic propValue = GetValueAsType(value[property.Name], property.PropertyType) ?? property.PropertyType.GetDefaultValue();
                    property.SetValue(entry, value[property.Name]);
                }
            }

            _ctx.Entry(entry).State = EntityState.Modified;

            try {
                await _ctx.SaveChangesAsync();
            } catch (DbUpdateConcurrencyException) {
                throw;
            }

            return NoContent();
        }

        // DELETE api/<CohortController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id) {

            var entry = await _ctx.Set<CohortDescription>().FindAsync(id);
            if (entry == null) {
                return NotFound($"Cohort with id {id} not found.");
            }

            _ctx.Set<CohortDescription>().Remove(entry);
            await _ctx.SaveChangesAsync();

            return NoContent();
        }

        private static object? GetValueAsType(object source, Type desiredType) {
            if (source == null) return source;

            string strSrc = source.ToString();
            if (string.IsNullOrEmpty(strSrc)) {
                return null;
            }

            if (desiredType == typeof(Guid)) {
                return Guid.Parse(source.ToString());
            } else if (desiredType == typeof(DateTime?)) {
                return DateTime.Parse(source.ToString());
            } else if (desiredType.IsEnum) {
                dynamic result;
                if (Enum.TryParse(desiredType, strSrc, true, out result)) return result;
                else return null;
            }

            return Convert.ChangeType(source, desiredType);
        }

        private bool Exists(long? id) {
            if (id == null) { return false; }
            return (_ctx.Set<CohortDescription>()?.Any(x => x.Id == id)).GetValueOrDefault();
        }

        private bool Exists(long id) {
            return (_ctx.Set<CohortDescription>()?.Any(x => x.Id == id)).GetValueOrDefault();
        }

    }
}
