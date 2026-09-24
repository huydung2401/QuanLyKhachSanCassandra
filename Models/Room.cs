using System.ComponentModel.DataAnnotations;

namespace QuanLyKhachSan.Models
{
    public class Room
    {
        [Required]
        [Display(Name = "Mã khách sạn")]
        public string HotelId { get; set; }

        [Required]
        [Display(Name = "Số phòng")]
        public int RoomNumber { get; set; }

        [Required]
        [Display(Name = "Loại phòng")]
        public string RoomType { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        [Display(Name = "Giá phòng / đêm")]
        public decimal PricePerNight { get; set; }

        [Required]
        [Display(Name = "Trạng thái")]
        public string Status { get; set; }

        [Display(Name = "Ảnh phòng")]
        public string ImageUrl { get; set; }
    }
}