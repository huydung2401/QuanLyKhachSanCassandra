using System;
using System.Collections.Generic;

namespace QuanLyKhachSan.Models
{
    public class UserBookingSearchViewModel
    {
        public DateTime CheckIn { get; set; }

        public DateTime CheckOut { get; set; }

        public int Guests { get; set; }

        public List<BookingRoomOption> AvailableRooms { get; set; }

        public string ErrorMessage { get; set; }

        public UserBookingSearchViewModel()
        {
            AvailableRooms = new List<BookingRoomOption>();
            Guests = 1;
            CheckIn = DateTime.Today.AddDays(1);
            CheckOut = DateTime.Today.AddDays(2);
        }
    }
}