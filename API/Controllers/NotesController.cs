using Microsoft.AspNetCore.Mvc;
using Notes.Application.Commands.CreateNote;
using Notes.Application.Commands.DeleteNotes;
using Notes.Application.Commands.GetNotes;
using Notes.Application.Commands.UpdateNote;

namespace API.Controllers
{
    [ApiController]
    [Route("api/notes")]
    public class NotesController : ControllerBase
    {
        private readonly CreateNoteHandler _createHandler;
        private readonly GetNotesHandler _getNotesHandler;
        private readonly DeleteNoteHandler _deleteHandler;
        private readonly UpdateNoteHandler _updateHandler;

        public NotesController(
            CreateNoteHandler createHandler,
            GetNotesHandler getNotesHandler,
            DeleteNoteHandler deleteHandler,
            UpdateNoteHandler updateHandler)
        {
            _createHandler = createHandler;
            _getNotesHandler = getNotesHandler;
            _deleteHandler = deleteHandler;
            _updateHandler = updateHandler;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateNoteCommand command, CancellationToken ct)
        {
            var id = await _createHandler.Handle(command, ct);
            return Ok(id);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var notes = await _getNotesHandler.Handle(new GetNotesQuery(), ct);
            return Ok(notes);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _deleteHandler.Handle(new DeleteNoteCommand(id), ct);
            return NoContent();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, UpdateNoteCommand command, CancellationToken ct)
        {
            var updateCommand = new UpdateNoteCommand(id, command.Title, command.Content);
            await _updateHandler.Handle(updateCommand, ct);
            return NoContent();
        }
    }
}
