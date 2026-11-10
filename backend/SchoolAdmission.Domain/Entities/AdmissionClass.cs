namespace SchoolAdmission.Domain.Entities
{
    public class AdmissionClass
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int TotalSeats { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        public ICollection<SeatHold> Holds { get; set; } = new List<SeatHold>();
    }
}
