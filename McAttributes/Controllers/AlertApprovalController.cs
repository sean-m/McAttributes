using McAttributes.Data;
using McAttributes.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics.Internal;
using Newtonsoft.Json.Linq;
using Polly;
using SMM.Helper;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace McAttributes.Controllers {
    [Route("api/[controller]")]
    [ApiController]
    public class AlertApprovalController : ControllerBase {


        ILogger _logger;
        readonly dynamic _ctxFactory;

        Random jitterer { get; set; } = new Random();

        Policy retry { get; init; }

        public AlertApprovalController(ILogger<AlertApprovalController> logger, IDbContextFactory<IdDbContext> factory) {
            _logger = logger;
            _ctxFactory = factory;

            retry = Policy
                .Handle<Npgsql.NpgsqlOperationInProgressException>()
                .WaitAndRetry(6, retryAttempt =>
                TimeSpan.FromMilliseconds(200)
                + TimeSpan.FromMilliseconds(jitterer.Next(0, 1000)));
        }

        public class ResolveAlertApproval {
            public long? id { get; set; }
            public long? alertId { get; set; }
        }

        [HttpGet]
        public IActionResult Get([FromQuery] ResolveAlertApproval approval) {
            IActionResult innerResult = NoContent();

            if (approval.alertId != 0 && approval.alertId != null) {
                using (IdDbContext ctx = _ctxFactory.CreateDbContext()) {
                    var list = ctx.AlertApprovals.Where(x => x.AlertId == approval.alertId).ToList();
                    innerResult = Ok(list);
                }
            } else {
                using (IdDbContext ctx = _ctxFactory.CreateDbContext()) {
                    if (approval.id == null) {
                        innerResult = BadRequest("Must pass an object looking like this { id:0, alertId:0 } with one of the properties populated with a non-zero number.");
                    }

                    var result = retry.ExecuteAndCapture(() => ctx.AlertApprovals.Find(approval.id));
                    innerResult = Ok(result.Result);
                }
            }
            
            return innerResult;
        }


        [HttpPatch]
        public async Task<IActionResult> Patch([FromBody] AlertApprovalBase[] approvals) {
            IActionResult innerResult = NoContent();
            
            using (IdDbContext ctx = _ctxFactory.CreateDbContext()) {
                Console.WriteLine($"\n\n\ncontext hash: {ctx.GetHashCode()}\n\n\n");
                foreach (var approval in approvals) {
                    var entity = await ctx.AlertApprovals.FirstOrDefaultAsync(a => a.UserId == approval.UserId && a.AlertId == approval.AlertId);
                    if (entity == null) {
                        // That's missing so add it
                        ctx.AlertApprovals.Add(approval.ToAlertApproval());
                    } else {
                        // Update the thing we already have if neccessary
                        if (entity.Status != approval.Status) {
                            entity.Status = approval.Status;
                            ctx.AlertApprovals.Entry(entity).State = EntityState.Modified;
                        }
                    }
                }
                await ctx.SaveChangesAsync();
            }
        
            return innerResult;
        }

        [HttpPost]
        public IActionResult Post([FromBody] AlertApprovalBase[] approvals) {
            
            using (IdDbContext ctx = _ctxFactory.CreateDbContext()) {
                Console.WriteLine($"\n\n\ncontext hash: {ctx.GetHashCode()}\n\n\n");
                foreach (var approval in approvals) {
                    var entity = ctx.AlertApprovals.FirstOrDefault(a => a.UserId == approval.UserId && a.AlertId == approval.AlertId);
                    if (entity != null) {
                        return BadRequest($"Approval with id {approval.Id} already exists. Try PATCH method instead.");
                    }

                    System.Diagnostics.Trace.WriteLine($"context hash: {ctx.GetHashCode()}");
                    ctx.AlertApprovals.Add(approval.ToAlertApproval());
                }

                return Ok(ctx.SaveChanges());
            }
        }
    }
}
