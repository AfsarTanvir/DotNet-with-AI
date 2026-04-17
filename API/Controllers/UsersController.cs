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
            return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, user);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var users = await _mediator.Send(new GetUsersQuery(), ct);
            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(Guid id, CancellationToken ct)
        {
            var user = await _mediator.Send(new GetUserQuery(id), ct);
            if (user == null)
                return NotFound();
            return Ok(user);
        }
    }
}
