namespace Notes.Domain.Exceptions
{
    public class NoteNotFoundException : Exception
    {
        public NoteNotFoundException(Guid id) 
            : base($"Note with ID {id} was not found.")
        {
        }
    }
}
