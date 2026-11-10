using SchoolAdmission.Domain.Enums;
namespace SchoolAdmission.Domain.Entities
{
    public class SeatHold
    {
        public string Id { get; set; } = string.Empty;
        public string ClassId { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public HoldStatus Status { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
        public DateTimeOffset? ConfirmedAt { get; set; }
        public DateTimeOffset? CancelledAt { get; set; }
        public DateTimeOffset? ExpiredAt { get; set; }

        public AdmissionClass? AdmissionClass { get; set; }
    }
}
