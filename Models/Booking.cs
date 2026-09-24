using System;

namespace QuanLyKhachSan.Models
{
    public class Booking
    {
        /// <summary>
        /// Mã khách sạn.
        /// Hệ thống hiện tại quản lý HOTEL001.
        /// </summary>
        public string HotelId { get; set; }


        /// <summary>
        /// Ngày nhận phòng.
        /// </summary>
        public DateTime CheckInDate { get; set; }


        /// <summary>
        /// Mã booking.
        /// </summary>
        public Guid BookingId { get; set; }


        /// <summary>
        /// Mã khách hàng.
        /// </summary>
        public string GuestId { get; set; }


        /// <summary>
        /// Tên khách hàng.
        /// </summary>
        public string GuestName { get; set; }


        /// <summary>
        /// Số phòng.
        /// </summary>
        public int RoomNumber { get; set; }


        /// <summary>
        /// Ngày trả phòng.
        /// </summary>
        public DateTime CheckOutDate { get; set; }


        /// <summary>
        /// Tổng tiền booking.
        /// </summary>
        public decimal TotalAmount { get; set; }


        /// <summary>
        /// Trạng thái booking.
        /// Ví dụ: CONFIRMED, CHECKED_IN, CHECKED_OUT, CANCELLED.
        /// </summary>
        public string Status { get; set; }
    }
}