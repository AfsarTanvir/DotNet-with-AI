using BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Notes.Application.Commands.CreateNote;
using Notes.Application.Commands.DeleteNotes;
using Notes.Application.Commands.UpdateNote;
using Notes.Application.Queries.GetNote;
using Notes.Application.Queries.GetNotes;

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
            var note = await _mediator.Send(command, ct);
            var response = ApiResponse<object>.SuccessResponse(note, "Note created successfully");
            return CreatedAtAction(nameof(GetNoteById), new { id = note.Id }, response);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var notes = await _mediator.Send(new GetNotesQuery(), ct);
            var response = ApiResponse<object>.SuccessResponse(notes, "Notes retrieved successfully");
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetNoteById(Guid id, CancellationToken ct)
        {
            var note = await _mediator.Send(new GetNoteQuery(id), ct);
            if (note == null)
            {
                var errorResponse = ApiResponse<object>.ErrorResponse("Note not found", "Not Found");
                return NotFound(errorResponse);
            }
            var response = ApiResponse<object>.SuccessResponse(note, "Note retrieved successfully");
            return Ok(response);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new DeleteNoteCommand(id), ct);
            var response = ApiResponse.SuccessResponse("Note deleted successfully");
            return Ok(response);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, UpdateNoteCommand command, CancellationToken ct)
        {
            var updateCommand = new UpdateNoteCommand(id, command.Title, command.Content);
            await _mediator.Send(updateCommand, ct);
            var response = ApiResponse.SuccessResponse("Note updated successfully");
            return Ok(response);
        }
    }
}
