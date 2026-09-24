namespace QuanLyKhachSan.Models
{
    public class DashboardViewModel
    {
        // =====================================================
        // THÔNG TIN KHÁCH SẠN DUY NHẤT
        // =====================================================

        public string HotelId { get; set; }

        public string HotelName { get; set; }

        public string HotelAddress { get; set; }

        public string HotelCity { get; set; }

        public int HotelStarRating { get; set; }

        public string HotelPhone { get; set; }


        // =====================================================
        // THỐNG KÊ PHÒNG
        // =====================================================

        public int TotalRooms { get; set; }

        public int AvailableRooms { get; set; }

        public int OccupiedRooms
        {
            get
            {
                return TotalRooms - AvailableRooms;
            }
        }


        // =====================================================
        // BOOKING
        // =====================================================

        public int TotalBookings { get; set; }


        // =====================================================
        // HỆ THỐNG
        // =====================================================

        public string DatabaseName { get; set; }

        public string Keyspace { get; set; }

        public string Framework { get; set; }

        public decimal TotalRevenue { get; set; }
    }
}