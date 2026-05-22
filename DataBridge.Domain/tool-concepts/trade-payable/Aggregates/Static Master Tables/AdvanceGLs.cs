

namespace Domain.Aggregates
{
    public class AdvanceGLs
    {
        public Guid? Id { get; set; }
        public string GL_Code { get; set; } = null!;
        public bool IsActive { get; set; } = true;
    }
}
