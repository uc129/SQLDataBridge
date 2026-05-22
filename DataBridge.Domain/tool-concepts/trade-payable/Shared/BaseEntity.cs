namespace Domain.Shared
{
    public class BaseEntity
    {
        public string CreatedBy { get; set; } = null!;
        public string ModifiedBy { get; set; } = null!;
        public DateTime DateCreated { get; set; } = new DateTime(1800, 1, 1);
        public DateTime? DateModified { get; set; } = null;
        public bool IsActive { get; set; }
    }
}
