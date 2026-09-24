using System.ComponentModel.DataAnnotations;

namespace QuanLyKhachSan.Models
{
    public class Guest
    {
        [Required]
        [Display(Name = "Mã khách hàng")]
        public string GuestId { get; set; }

        [Required]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        [Required]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; }

        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "CCCD/CMND")]
        public string NationalId { get; set; }
    }
}