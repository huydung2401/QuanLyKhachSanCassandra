using System;

namespace QuanLyKhachSan.Models
{
    public class BookingLookupResult
    {
        public string Phone { get; set; }

        public string BookingId { get; set; }

        public string GuestName { get; set; }

        public int RoomNumber { get; set; }

        public DateTime CheckIn { get; set; }

        public DateTime CheckOut { get; set; }

        public decimal TotalAmount { get; set; }

        public string Status { get; set; }

        public string HotelName { get; set; }
    }
}