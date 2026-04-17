using BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Notes.Application.Commands.CreateUser;
using Notes.Application.Queries.GetUser;
using Notes.Application.Queries.GetUsers;

namespace API.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public UsersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateUserCommand command, CancellationToken ct)
        {
            var user = await _mediator.Send(command, ct);
            var response = ApiResponse<object>.SuccessResponse(user, "User created successfully");
            return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, response);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var users = await _mediator.Send(new GetUsersQuery(), ct);
            var response = ApiResponse<object>.SuccessResponse(users, "Users retrieved successfully");
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(Guid id, CancellationToken ct)
        {
            var user = await _mediator.Send(new GetUserQuery(id), ct);
            if (user == null)
            {
                var errorResponse = ApiResponse<object>.ErrorResponse("User not found", "Not Found");
                return NotFound(errorResponse);
            }
            var response = ApiResponse<object>.SuccessResponse(user, "User retrieved successfully");
            return Ok(response);
        }
    }
}
