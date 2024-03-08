using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace McAttributes.Controllers {
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = Globals.AUTH_SCHEMES)]
    public class MeController : ControllerBase {
        ILogger _logger;
        MeController(ILogger<MeController> logger) {
            _logger = logger;
        }

        [HttpGet]
        [Authorize]
        public object Get() {
            return HttpContext.User?.Identity;
        }
    }
}
