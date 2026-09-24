using System;

namespace QuanLyKhachSan.Models
{
    public class Invoice
    {
        /// <summary>
        /// Mã booking liên kết với hóa đơn.
        /// </summary>
        public Guid BookingId { get; set; }


        /// <summary>
        /// Mã hóa đơn.
        /// </summary>
        public Guid InvoiceId { get; set; }


        /// <summary>
        /// Mã khách hàng.
        /// </summary>
        public string GuestId { get; set; }


        /// <summary>
        /// Mã khách sạn.
        /// Hệ thống hiện tại sử dụng HOTEL001.
        /// </summary>
        public string HotelId { get; set; }


        /// <summary>
        /// Tiền phòng.
        /// </summary>
        public decimal RoomCharge { get; set; }


        /// <summary>
        /// Tiền dịch vụ.
        /// Với hệ thống hiện tại luôn bằng 0
        /// vì chỉ quản lý hóa đơn tiền phòng.
        /// </summary>
        public decimal ServiceCharge { get; set; }


        /// <summary>
        /// Thuế.
        /// </summary>
        public decimal Tax { get; set; }


        /// <summary>
        /// Tổng tiền hóa đơn.
        /// </summary>
        public decimal TotalAmount { get; set; }


        /// <summary>
        /// Trạng thái thanh toán.
        /// Ví dụ: UNPAID, PAID, CANCELLED.
        /// </summary>
        public string PaymentStatus { get; set; }


        /// <summary>
        /// Thời điểm phát hành hóa đơn.
        /// </summary>
        public DateTime IssuedAt { get; set; }


        // =====================================================
        // THÔNG TIN HIỂN THỊ
        // =====================================================
        // Các thuộc tính dưới đây dùng cho giao diện,
        // không nhất thiết phải lưu vào invoices_by_booking.


        /// <summary>
        /// Tên khách hàng.
        /// </summary>
        public string GuestName { get; set; }


        /// <summary>
        /// Số phòng.
        /// </summary>
        public int RoomNumber { get; set; }


        /// <summary>
        /// Ngày nhận phòng.
        /// </summary>
        public DateTime CheckInDate { get; set; }


        /// <summary>
        /// Ngày trả phòng.
        /// </summary>
        public DateTime CheckOutDate { get; set; }


        /// <summary>
        /// Số đêm lưu trú.
        /// </summary>
        public int NumberOfNights
        {
            get
            {
                int nights =
                    (CheckOutDate.Date - CheckInDate.Date).Days;

                return nights < 0 ? 0 : nights;
            }
        }


        /// <summary>
        /// Giá phòng mỗi đêm.
        /// Dùng để hiển thị khi xem hóa đơn.
        /// </summary>
        public decimal PricePerNight { get; set; }
    }
}