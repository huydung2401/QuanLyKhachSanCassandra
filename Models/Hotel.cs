using System.ComponentModel.DataAnnotations;

namespace QuanLyKhachSan.Models
{
    public class Hotel
    {
        [Required]
        [Display(Name = "Mã khách sạn")]
        public string HotelId { get; set; }

        [Required]
        [Display(Name = "Tên khách sạn")]
        public string HotelName { get; set; }

        [Required]
        [Display(Name = "Địa chỉ")]
        public string Address { get; set; }

        [Required]
        [Display(Name = "Thành phố")]
        public string City { get; set; }

        [Range(1, 5)]
        [Display(Name = "Số sao")]
        public int StarRating { get; set; }

        [Display(Name = "Điện thoại")]
        public string Phone { get; set; }
    }
}