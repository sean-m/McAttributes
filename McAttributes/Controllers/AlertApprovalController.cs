using McAttributes.Data;
using McAttributes.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Newtonsoft.Json.Linq;
using Polly;
using SMM.Helper;
using System.Data.Entity;
using System.Runtime.CompilerServices;

namespace McAttributes.Controllers {
    [Route("api/[controller]")]
    [ApiController]
    public class AlertApprovalController : ControllerBase {


        ILogger _logger;
        readonly IdDbContext _ctx;

        Random jitterer { get; set; } = new Random();

        Policy retry { get; init; }

        public AlertApprovalController(ILogger <AlertApprovalController> logger, IdDbContext dbContext) {
            _logger = logger;
            _ctx = dbContext;

            retry = Policy
                .Handle<Npgsql.NpgsqlOperationInProgressException>()
                .WaitAndRetry(4, retryAttempt =>
                TimeSpan.FromSeconds((double)(Math.Pow(2, retryAttempt)) / 9)
                + TimeSpan.FromMilliseconds(jitterer.Next(0, 500)));
        }

        public class ResolveAlertApproval {
            public long? id { get; set; }
            public long? alertId { get; set; }
        }

        [HttpGet]
        public IActionResult Get([FromQuery] ResolveAlertApproval approval) {

            if (approval.alertId != 0 && approval.alertId != null) {
                var alertResult = retry.ExecuteAndCapture(() => _ctx.AlertApprovals.Where(x => x.AlertId == approval.alertId).ToList());
                return Ok(alertResult.Result);
            }

            if (approval.id == null) {
                return BadRequest("Must pass an object looking like this { id:0, alertId:0 } with one of the properties populated with a non-zero number.");
            }

            var result = retry.ExecuteAndCapture(() => _ctx.AlertApprovals.Find(approval.id));
            return Ok(result.Result);
        }

        [HttpPatch]
        public IActionResult Patch([FromBody] AlertApproval approval) {
            if (!RecordExists<AlertApproval>(approval.Id)) {
                return Post(approval);
            }

            var entity = _ctx.AlertApprovals.Entry(approval);
            PatchObject<AlertApproval>(approval, entity);

            return NoContent();
        }

        [HttpPost]
        public IActionResult Post([FromBody] AlertApproval approval) {
            if (RecordExists<AlertApproval>(approval.Id)) {
                return BadRequest($"Approval with id {approval.Id} already exists. Try PATCH method instead.");
            }

            _ctx.AlertApprovals.Add(approval);
            retry.ExecuteAndCapture(() => _ctx.SaveChanges());

            return Ok();
        }

        private void PatchObject<T>(T source, EntityEntry target) {
            // Loop through properties on the model and update them if
            // they exist in the patch value and differ from the database entry.
            var properties = typeof(User).GetProperties();
            foreach (var property in properties) {
                if (property.Name.Like("Id")) continue;
                if (property.GetValue(target) != property.GetValue(source)) {
                    property.SetValue(target, property.GetValue(source));
                }
            }
        }

        private bool RecordExists<T>(long id) where T : RowVersionedModel
        {
            return (_ctx.Set<T>()?.Any(x => x.Id == id)).GetValueOrDefault();
        }
    }
}
