using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuanLyKhachSan.Models
{
    public class UserBookingViewModel
    {
        // =====================================================
        // PHÒNG
        // =====================================================

        [Required]
        public int RoomNumber { get; set; }

        public string RoomType { get; set; }

        public decimal PricePerNight { get; set; }

        public string ImageUrl { get; set; }


        // =====================================================
        // THÔNG TIN KHÁCH HÀNG
        // =====================================================

        [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }


        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [Display(Name = "Số điện thoại")]
        [RegularExpression(
            @"^(0|\+84)[0-9]{9,10}$",
            ErrorMessage = "Số điện thoại không hợp lệ."
        )]
        public string Phone { get; set; }


        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [Display(Name = "Email")]
        public string Email { get; set; }


        [Required(ErrorMessage = "Vui lòng nhập số CCCD/CMND.")]
        [Display(Name = "CCCD/CMND")]
        [RegularExpression(
            @"^[0-9]{9,12}$",
            ErrorMessage = "CCCD/CMND phải gồm 9 đến 12 chữ số."
        )]
        public string NationalId { get; set; }


        // =====================================================
        // THÔNG TIN ĐẶT PHÒNG
        // =====================================================

        [Required(ErrorMessage = "Vui lòng chọn ngày nhận phòng.")]
        [Display(Name = "Ngày nhận phòng")]
        public DateTime CheckIn { get; set; }


        [Required(ErrorMessage = "Vui lòng chọn ngày trả phòng.")]
        [Display(Name = "Ngày trả phòng")]
        public DateTime CheckOut { get; set; }


        [Range(
            1,
            10,
            ErrorMessage = "Số khách phải từ 1 đến 10."
        )]
        [Display(Name = "Số khách")]
        public int Guests { get; set; } = 1;


        // =====================================================
        // TÍNH TOÁN
        // =====================================================

        public int NumberOfNights
        {
            get
            {
                if (CheckOut <= CheckIn)
                {
                    return 0;
                }

                return (CheckOut.Date - CheckIn.Date).Days;
            }
        }


        public decimal TotalAmount
        {
            get
            {
                return NumberOfNights * PricePerNight;
            }
        }


        // =====================================================
        // THÔNG BÁO
        // =====================================================

        public string ErrorMessage { get; set; }
    }


    // =========================================================
    // PHÒNG CHO COMBOBOX
    // =========================================================

    public class BookingRoomOption
    {
        public int RoomNumber { get; set; }

        public string RoomType { get; set; }

        public decimal PricePerNight { get; set; }

        public string ImageUrl { get; set; }

        public string Status { get; set; }

        public string DisplayName
        {
            get
            {
                return
                    "Phòng " +
                    RoomNumber +
                    " - " +
                    RoomType +
                    " - " +
                    PricePerNight.ToString("N0") +
                    " ₫/đêm";
            }
        }
    }
}