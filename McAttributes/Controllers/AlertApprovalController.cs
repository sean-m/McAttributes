﻿using McAttributes.Data;
using McAttributes.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.EntityFrameworkCore;
using Polly;

namespace McAttributes.Controllers {
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = Globals.AUTH_SCHEMES)]
    public class AlertApprovalController : ODataController {


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

        [HttpGet("{id}")]
        public IQueryable Get(long? id) {
            IdDbContext ctx = _ctxFactory.CreateDbContext();
            if (id != null && id > 0) {
                return ctx.AlertApprovals.Where(x => x.Id == (long)id);
            }
            return ctx.Set<AlertLogApproval>();
        }

        [HttpPatch]
        public async Task<IActionResult> Patch([FromBody] AlertApprovalBase[] approvals) {
            var requestId = jitterer.NextInt64();
            try {
                using (IdDbContext ctx = _ctxFactory.CreateDbContext()) {
                    Console.WriteLine($"\n\n\ncontext hash: {ctx.GetHashCode()}\n\n\n");
                    foreach (var approval in approvals) {
                        var entity = await ctx.AlertApprovals.FirstOrDefaultAsync(a => a.UserId == approval.UserId && a.AlertId == approval.AlertId);
                        if (entity == null) {
                            // That's missing so add it
                            ctx.AlertApprovals.Add(approval.ToAlertApproval());
                            _logger.LogInformation($"Request ID {requestId}  User: {User.Identity?.Name} added alert approval for alert: {approval.AlertId} for user {approval.UserId} to {approval.Status}");
                        } else {
                            // Update the thing we already have if neccessary
                            if (entity.Status != approval.Status) {
                                entity.Status = approval.Status;
                                ctx.AlertApprovals.Entry(entity).State = EntityState.Modified;
                                _logger.LogInformation($"Request ID {requestId}  User: {User.Identity?.Name} set alert approval for alert: {approval.AlertId} for user {approval.UserId} to {approval.Status}");
                            }
                        }
                    }
                    await ctx.SaveChangesAsync();
                }
            } catch (Exception ex) {
                _logger.LogCritical($"Request ID {requestId} Failed. {ex.Message}. Rethrowing.");
                throw;
            }

            return NoContent();
        }

        [HttpPost]
        public IActionResult Post([FromBody] AlertApprovalBase[] approvals) {
            var requestId = jitterer.NextInt64();
            try {
                using (IdDbContext ctx = _ctxFactory.CreateDbContext()) {
                    Console.WriteLine($"\n\n\ncontext hash: {ctx.GetHashCode()}\n\n\n");
                    foreach (var approval in approvals) {
                        var entity = ctx.AlertApprovals.FirstOrDefault(a => a.UserId == approval.UserId && a.AlertId == approval.AlertId);
                        if (entity != null) {
                            return BadRequest($"Approval with id {approval.Id} already exists. Try PATCH method instead.");
                        }

                        System.Diagnostics.Trace.WriteLine($"context hash: {ctx.GetHashCode()}");
                        ctx.AlertApprovals.Add(approval.ToAlertApproval());
                        _logger.LogInformation($"Request ID {requestId}  User: {User.Identity?.Name} added alert approval for alert: {approval.AlertId} for user {approval.UserId} to {approval.Status}");
                    }

                    return Ok(ctx.SaveChanges());
                }
            } catch (Exception ex) {
                _logger.LogCritical($"Request ID {requestId} Failed. {ex.Message}. Rethrowing.");
                throw;
            }
        }
    }
}
