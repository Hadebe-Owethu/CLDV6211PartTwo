namespace EventEaseAppOwethuHadebeMVC.Models
{
    public class Booking
    {
        public int BookingID { get; set; } 

        public int EventID { get; set; }
        public Event? Event { get; set; }
        public int VenueID { get; set; }
        public Venue? Venue { get; set; }
        public DateTime BookingDate { get; set; }

    }
}
