namespace QuanLyKhachSan.Models
{
    public class UserRoomDetailViewModel
    {
        // =====================================================
        // THÔNG TIN CƠ BẢN
        // =====================================================

        public string HotelId { get; set; }

        public int RoomNumber { get; set; }

        public string RoomType { get; set; }

        public decimal PricePerNight { get; set; }

        public string Status { get; set; }

        public string ImageUrl { get; set; }


        // =====================================================
        // THÔNG TIN CHI TIẾT
        // =====================================================

        public int Capacity { get; set; }

        public int Area { get; set; }

        public string Description { get; set; }


        // =====================================================
        // ĐÁNH GIÁ
        // =====================================================

        public decimal Rating { get; set; }

        public int ReviewCount { get; set; }
    }
}