using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SPM.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HelloController : ControllerBase
    {
        private readonly Errors _errors;

        public HelloController(Errors errors)
        {
            _errors = errors;
        }

        [HttpGet("message")]
        public IActionResult GetMessage()
        {
            if (_errors.ProduceError())
            {
                return StatusCode(401);
            }
            return Ok("Hello from service");
        }
        [HttpGet("error")]
        public IActionResult GenerateError()
        {
            try
            {
                // Simulate an error
                throw new Exception("This is a simulated error");
            }
            catch (Exception ex)
            {
                //_errors.LogError(ex.Message);
                return StatusCode(500, "Internal Server Error");
            }
        }
    }
}
