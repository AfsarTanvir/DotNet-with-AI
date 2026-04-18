using BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Notes.Application.Commands.Login;
using Notes.Application.Commands.Register;

namespace API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterCommand command)
        {
            var token = await _mediator.Send(command);
            var response = ApiResponse<object>.SuccessResponse(new { token }, "Registration successful");
            return Ok(response);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginCommand command)
        {
            var token = await _mediator.Send(command);
            var response = ApiResponse<object>.SuccessResponse(new { token }, "Login successful");
            return Ok(response);
        }
    }
}
