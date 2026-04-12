using MediatR;
using Microsoft.AspNetCore.Mvc;
using Notes.Application.Commands.CreateNote;
using Notes.Application.Commands.DeleteNotes;
using Notes.Application.Commands.GetNote;
using Notes.Application.Commands.GetNotes;
using Notes.Application.Commands.UpdateNote;

namespace API.Controllers
{
    [ApiController]
    [Route("api/notes")]
    public class NotesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public NotesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateNoteCommand command, CancellationToken ct)
        {
            var id = await _mediator.Send(command, ct);
            return Ok(id);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var notes = await _mediator.Send(new GetNotesQuery(), ct);
            return Ok(notes);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetNoteById(Guid id, CancellationToken ct)
        {
            var note = await _mediator.Send(new GetNoteQuery(id), ct);
            if (note == null)
                return NotFound();
            return Ok(note);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new DeleteNoteCommand(id), ct);
            return NoContent();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, UpdateNoteCommand command, CancellationToken ct)
        {
            var updateCommand = new UpdateNoteCommand(id, command.Title, command.Content);
            await _mediator.Send(updateCommand, ct);
            return NoContent();
        }
    }
}
