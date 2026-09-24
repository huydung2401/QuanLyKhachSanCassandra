using System.Collections.Generic;

namespace QuanLyKhachSan.Models
{
    public class UserHomeViewModel
    {
        // =====================================================
        // HOTEL
        // =====================================================

        public string HotelId { get; set; }

        public string HotelName { get; set; }

        public string Address { get; set; }

        public string City { get; set; }

        public string Phone { get; set; }

        public int StarRating { get; set; }


        // =====================================================
        // ROOMS
        // =====================================================

        public List<UserRoomViewModel> Rooms { get; set; }


        public UserHomeViewModel()
        {
            Rooms = new List<UserRoomViewModel>();
        }
    }


    public class UserRoomViewModel
    {
        public string HotelId { get; set; }

        public int RoomNumber { get; set; }

        public string RoomType { get; set; }

        public decimal PricePerNight { get; set; }

        public string Status { get; set; }

        public string ImageUrl { get; set; }
    }
}