﻿using McAttributes.Models;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.OData.Query;
using McAttributes.Data;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.AspNetCore.Authorization;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace McAttributes.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
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
        [HttpGet("{aadid}")]
        [EnableQuery(PageSize = 100)]
        public IQueryable<User> Get(string? aadid) {

            // TODO filter based on requestor identity
            if (string.IsNullOrEmpty(aadid)) {
                return _users;
            }

            Guid id;
            if (Guid.TryParse(aadid, out id)) {
                return _users.Where(u => u.AadId == id).AsQueryable();
            }

            throw new HttpRequestException($"Could not parse provided addid value in a Guid: {aadid.Substring(0, Math.Min(64, aadid.Length))}",
                inner: null, statusCode: System.Net.HttpStatusCode.BadRequest);
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

        // PATCH api/<UserController>/5
        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(int id, [FromBody] Dictionary<string, object> value) {

            var entry = _ctx.Set<User>().First(x => x.Id == id);
            if (entry == null) return NotFound();

            // Loop through properties on the model and update them if
            // they exist in the patch value and differ from the database entry.
            var properties = typeof(User).GetProperties();
            foreach (var property in properties) {
                if (property.Name == "Id") continue;
                if (value.ContainsKey(property.Name) && property.GetValue(entry) != value[property.Name]) {
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

        private bool UserRecordExists(int id)
        {
            return (_users?.Any(x => x.Id == id)).GetValueOrDefault();
        }
    }
}
