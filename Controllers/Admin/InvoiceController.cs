using Cassandra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using QuanLyKhachSan.Models;
using QuanLyKhachSan.Services;

namespace QuanLyKhachSan.Controllers.Admin
{
    public class InvoiceController : Controller
    {
        private const string HOTEL_ID = "HOTEL001";


        // =====================================================
        // INDEX
        // Danh sách hóa đơn
        // =====================================================

        public ActionResult Index()
        {
            try
            {
                using (var cassandra = new CassandraService())
                {
                    /*
                     * invoices_by_booking có partition key:
                     *
                     * booking_id
                     *
                     * Vì vậy không thể lấy toàn bộ hóa đơn
                     * bằng cách SELECT trực tiếp theo hotel_id.
                     *
                     * Lấy danh sách booking trước,
                     * sau đó lấy invoice tương ứng.
                     */

                    string bookingCql = @"
                        SELECT
                            hotel_id,
                            check_in_date,
                            booking_id,
                            guest_id,
                            guest_name,
                            room_number,
                            check_out_date,
                            total_amount,
                            status
                        FROM bookings_by_hotel_date
                        WHERE hotel_id = ?;
                    ";


                    var bookingRows =
                        cassandra.Execute(
                            new SimpleStatement(
                                bookingCql,
                                HOTEL_ID
                            )
                        );


                    var invoices =
                        new List<Invoice>();


                    foreach (var bookingRow in bookingRows)
                    {
                        Guid bookingId =
                            bookingRow.GetValue<Guid>(
                                "booking_id"
                            );


                        string invoiceCql = @"
                            SELECT
                                booking_id,
                                invoice_id,
                                guest_id,
                                hotel_id,
                                room_charge,
                                service_charge,
                                tax,
                                total_amount,
                                payment_status,
                                issued_at
                            FROM invoices_by_booking
                            WHERE booking_id = ?;
                        ";


                        var invoiceRows =
                            cassandra.Execute(
                                new SimpleStatement(
                                    invoiceCql,
                                    bookingId
                                )
                            );


                        foreach (var invoiceRow in invoiceRows)
                        {
                            var invoice =
                                MapInvoice(
                                    invoiceRow
                                );


                            /*
                             * =====================================
                             * BỔ SUNG THÔNG TIN BOOKING
                             * =====================================
                             */

                            invoice.GuestName =
                                bookingRow.GetValue<string>(
                                    "guest_name"
                                );


                            invoice.RoomNumber =
                                bookingRow.GetValue<int>(
                                    "room_number"
                                );


                            invoice.CheckInDate =
                                ConvertLocalDate(
                                    bookingRow,
                                    "check_in_date"
                                );


                            invoice.CheckOutDate =
                                ConvertLocalDate(
                                    bookingRow,
                                    "check_out_date"
                                );


                            /*
                             * =====================================
                             * TÍNH GIÁ PHÒNG / ĐÊM
                             * =====================================
                             */

                            if (invoice.NumberOfNights > 0)
                            {
                                invoice.PricePerNight =
                                    invoice.RoomCharge /
                                    invoice.NumberOfNights;
                            }


                            /*
                             * =====================================
                             * HÓA ĐƠN CHỈ TÍNH TIỀN PHÒNG
                             *
                             * Không có:
                             * - Service Charge
                             * - Tax
                             *
                             * Vì vậy tổng tiền phải bằng tiền phòng.
                             *
                             * Điều này cũng chuẩn hóa các hóa đơn
                             * cũ đang có total_amount khác với
                             * room_charge.
                             * =====================================
                             */

                            invoice.ServiceCharge = 0m;

                            invoice.Tax = 0m;

                            invoice.TotalAmount =
                                invoice.RoomCharge;


                            invoices.Add(invoice);
                        }
                    }


                    /*
                     * =====================================
                     * HÓA ĐƠN MỚI NHẤT HIỂN THỊ TRƯỚC
                     * =====================================
                     */

                    invoices =
                        invoices
                            .OrderByDescending(
                                x => x.IssuedAt
                            )
                            .ToList();


                    return View(
                        "~/Views/Admin/Invoice/Index.cshtml",
                        invoices
                    );
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error =
                    "Không thể tải danh sách hóa đơn: "
                    + ex.Message;


                return View(
                    "~/Views/Admin/Invoice/Index.cshtml",
                    new List<Invoice>()
                );
            }
        }


        // =====================================================
        // DETAILS
        // Chi tiết hóa đơn
        // =====================================================

        public ActionResult Details(
            string bookingId,
            string invoiceId)
        {
            if (string.IsNullOrWhiteSpace(
                bookingId))
            {
                return HttpNotFound(
                    "Thiếu mã booking."
                );
            }


            if (string.IsNullOrWhiteSpace(
                invoiceId))
            {
                return HttpNotFound(
                    "Thiếu mã hóa đơn."
                );
            }


            Guid bookingGuid;
            Guid invoiceGuid;


            if (!Guid.TryParse(
                bookingId,
                out bookingGuid))
            {
                return HttpNotFound(
                    "Mã booking không hợp lệ."
                );
            }


            if (!Guid.TryParse(
                invoiceId,
                out invoiceGuid))
            {
                return HttpNotFound(
                    "Mã hóa đơn không hợp lệ."
                );
            }


            try
            {
                using (var cassandra =
                    new CassandraService())
                {
                    /*
                     * =====================================
                     * LẤY HÓA ĐƠN
                     * =====================================
                     */

                    string invoiceCql = @"
                        SELECT
                            booking_id,
                            invoice_id,
                            guest_id,
                            hotel_id,
                            room_charge,
                            service_charge,
                            tax,
                            total_amount,
                            payment_status,
                            issued_at
                        FROM invoices_by_booking
                        WHERE booking_id = ?
                        AND invoice_id = ?;
                    ";


                    var invoiceRow =
                        cassandra.Execute(
                            new SimpleStatement(
                                invoiceCql,
                                bookingGuid,
                                invoiceGuid
                            )
                        ).FirstOrDefault();


                    if (invoiceRow == null)
                    {
                        return HttpNotFound(
                            "Không tìm thấy hóa đơn."
                        );
                    }


                    var invoice =
                        MapInvoice(
                            invoiceRow
                        );


                    /*
                     * =====================================
                     * LẤY BOOKING
                     * =====================================
                     */

                    string bookingCql = @"
                        SELECT
                            hotel_id,
                            check_in_date,
                            booking_id,
                            guest_id,
                            guest_name,
                            room_number,
                            check_out_date,
                            total_amount,
                            status
                        FROM bookings_by_hotel_date
                        WHERE hotel_id = ?;
                    ";


                    var bookingRow =
                        cassandra.Execute(
                            new SimpleStatement(
                                bookingCql,
                                HOTEL_ID
                            )
                        )
                        .FirstOrDefault(
                            row =>
                                row.GetValue<Guid>(
                                    "booking_id"
                                ) == bookingGuid
                        );


                    if (bookingRow != null)
                    {
                        invoice.GuestName =
                            bookingRow.GetValue<string>(
                                "guest_name"
                            );


                        invoice.RoomNumber =
                            bookingRow.GetValue<int>(
                                "room_number"
                            );


                        invoice.CheckInDate =
                            ConvertLocalDate(
                                bookingRow,
                                "check_in_date"
                            );


                        invoice.CheckOutDate =
                            ConvertLocalDate(
                                bookingRow,
                                "check_out_date"
                            );


                        /*
                         * =================================
                         * TÍNH LẠI GIÁ PHÒNG / ĐÊM
                         * =================================
                         */

                        if (invoice.NumberOfNights > 0)
                        {
                            invoice.PricePerNight =
                                invoice.RoomCharge /
                                invoice.NumberOfNights;
                        }


                        /*
                         * =================================
                         * CHỈ TÍNH TIỀN PHÒNG
                         * =================================
                         */

                        invoice.ServiceCharge = 0m;

                        invoice.Tax = 0m;

                        invoice.TotalAmount =
                            invoice.RoomCharge;
                    }


                    return View(
                        "~/Views/Admin/Invoice/Details.cshtml",
                        invoice
                    );
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error =
                    "Không thể tải hóa đơn: "
                    + ex.Message;


                return View(
                    "~/Views/Admin/Invoice/Details.cshtml",
                    null
                );
            }
        }


        // =====================================================
        // CREATE - GET
        // Tạo hóa đơn từ booking
        // =====================================================

        [HttpGet]
        public ActionResult Create(
            string bookingId)
        {
            if (string.IsNullOrWhiteSpace(
                bookingId))
            {
                ViewBag.Error =
                    "Vui lòng chọn booking cần lập hóa đơn.";


                return View(
                    "~/Views/Admin/Invoice/Create.cshtml",
                    new Invoice()
                );
            }


            Guid bookingGuid;


            if (!Guid.TryParse(
                bookingId,
                out bookingGuid))
            {
                ViewBag.Error =
                    "Mã booking không hợp lệ.";


                return View(
                    "~/Views/Admin/Invoice/Create.cshtml",
                    new Invoice()
                );
            }


            try
            {
                using (var cassandra =
                    new CassandraService())
                {
                    /*
                     * =====================================
                     * KIỂM TRA BOOKING
                     * =====================================
                     */

                    string bookingCql = @"
                        SELECT
                            hotel_id,
                            check_in_date,
                            booking_id,
                            guest_id,
                            guest_name,
                            room_number,
                            check_out_date,
                            total_amount,
                            status
                        FROM bookings_by_hotel_date
                        WHERE hotel_id = ?;
                    ";


                    var bookingRow =
                        cassandra.Execute(
                            new SimpleStatement(
                                bookingCql,
                                HOTEL_ID
                            )
                        )
                        .FirstOrDefault(
                            row =>
                                row.GetValue<Guid>(
                                    "booking_id"
                                ) == bookingGuid
                        );


                    if (bookingRow == null)
                    {
                        ViewBag.Error =
                            "Không tìm thấy booking.";


                        return View(
                            "~/Views/Admin/Invoice/Create.cshtml",
                            new Invoice()
                        );
                    }


                    /*
                     * =====================================
                     * LẤY THÔNG TIN BOOKING
                     * =====================================
                     */

                    var invoice =
                        new Invoice
                        {
                            BookingId =
                                bookingGuid,

                            GuestId =
                                bookingRow.GetValue<string>(
                                    "guest_id"
                                ),

                            GuestName =
                                bookingRow.GetValue<string>(
                                    "guest_name"
                                ),

                            HotelId =
                                HOTEL_ID,

                            RoomNumber =
                                bookingRow.GetValue<int>(
                                    "room_number"
                                ),

                            CheckInDate =
                                ConvertLocalDate(
                                    bookingRow,
                                    "check_in_date"
                                ),

                            CheckOutDate =
                                ConvertLocalDate(
                                    bookingRow,
                                    "check_out_date"
                                ),

                            ServiceCharge = 0m,

                            Tax = 0m,

                            TotalAmount = 0m,

                            PaymentStatus =
                                "UNPAID"
                        };


                    /*
                     * =====================================
                     * LẤY GIÁ PHÒNG
                     * =====================================
                     */

                    string roomCql = @"
                        SELECT
                            room_number,
                            room_type,
                            price_per_night,
                            status,
                            image_url
                        FROM rooms_by_hotel
                        WHERE hotel_id = ?
                        AND room_number = ?;
                    ";


                    var roomRow =
                        cassandra.Execute(
                            new SimpleStatement(
                                roomCql,
                                HOTEL_ID,
                                invoice.RoomNumber
                            )
                        ).FirstOrDefault();


                    if (roomRow == null)
                    {
                        ViewBag.Error =
                            "Không tìm thấy phòng của booking.";


                        return View(
                            "~/Views/Admin/Invoice/Create.cshtml",
                            invoice
                        );
                    }


                    invoice.PricePerNight =
                        roomRow.GetValue<decimal>(
                            "price_per_night"
                        );


                    /*
                     * =====================================
                     * SỐ ĐÊM
                     * =====================================
                     */

                    int numberOfNights =
                        (
                            invoice.CheckOutDate.Date -
                            invoice.CheckInDate.Date
                        ).Days;


                    if (numberOfNights <= 0)
                    {
                        ViewBag.Error =
                            "Ngày trả phòng phải sau ngày nhận phòng.";


                        return View(
                            "~/Views/Admin/Invoice/Create.cshtml",
                            invoice
                        );
                    }


                    /*
                     * =====================================
                     * TÍNH HÓA ĐƠN
                     *
                     * CHỈ CÓ TIỀN PHÒNG
                     * =====================================
                     */

                    invoice.RoomCharge =
                        numberOfNights *
                        invoice.PricePerNight;


                    invoice.ServiceCharge =
                        0m;


                    invoice.Tax =
                        0m;


                    invoice.TotalAmount =
                        invoice.RoomCharge;


                    return View(
                        "~/Views/Admin/Invoice/Create.cshtml",
                        invoice
                    );
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error =
                    "Không thể chuẩn bị hóa đơn: "
                    + ex.Message;


                return View(
                    "~/Views/Admin/Invoice/Create.cshtml",
                    new Invoice()
                );
            }
        }


        // =====================================================
        // CREATE - POST
        // Lưu hóa đơn
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(
            Invoice model)
        {
            if (model.BookingId == Guid.Empty)
            {
                ModelState.AddModelError(
                    "",
                    "Booking không hợp lệ."
                );
            }


            if (!ModelState.IsValid)
            {
                return View(
                    "~/Views/Admin/Invoice/Create.cshtml",
                    model
                );
            }


            try
            {
                using (var cassandra =
                    new CassandraService())
                {
                    /*
                     * =====================================
                     * KIỂM TRA BOOKING
                     * =====================================
                     */

                    string bookingCql = @"
                        SELECT
                            hotel_id,
                            check_in_date,
                            booking_id,
                            guest_id,
                            guest_name,
                            room_number,
                            check_out_date,
                            status
                        FROM bookings_by_hotel_date
                        WHERE hotel_id = ?;
                    ";


                    var bookingRow =
                        cassandra.Execute(
                            new SimpleStatement(
                                bookingCql,
                                HOTEL_ID
                            )
                        )
                        .FirstOrDefault(
                            row =>
                                row.GetValue<Guid>(
                                    "booking_id"
                                ) == model.BookingId
                        );


                    if (bookingRow == null)
                    {
                        ModelState.AddModelError(
                            "",
                            "Không tìm thấy booking."
                        );


                        return View(
                            "~/Views/Admin/Invoice/Create.cshtml",
                            model
                        );
                    }


                    /*
                     * =====================================
                     * LẤY PHÒNG
                     * =====================================
                     */

                    int roomNumber =
                        bookingRow.GetValue<int>(
                            "room_number"
                        );


                    var roomRow =
                        cassandra.Execute(
                            new SimpleStatement(
                                @"
                                    SELECT
                                        price_per_night
                                    FROM rooms_by_hotel
                                    WHERE hotel_id = ?
                                    AND room_number = ?;
                                ",
                                HOTEL_ID,
                                roomNumber
                            )
                        ).FirstOrDefault();


                    if (roomRow == null)
                    {
                        ModelState.AddModelError(
                            "",
                            "Không tìm thấy phòng của booking."
                        );


                        return View(
                            "~/Views/Admin/Invoice/Create.cshtml",
                            model
                        );
                    }


                    decimal pricePerNight =
                        roomRow.GetValue<decimal>(
                            "price_per_night"
                        );


                    /*
                     * =====================================
                     * NGÀY NHẬN / TRẢ PHÒNG
                     * =====================================
                     */

                    DateTime checkInDate =
                        ConvertLocalDate(
                            bookingRow,
                            "check_in_date"
                        );


                    DateTime checkOutDate =
                        ConvertLocalDate(
                            bookingRow,
                            "check_out_date"
                        );


                    int numberOfNights =
                        (
                            checkOutDate.Date -
                            checkInDate.Date
                        ).Days;


                    if (numberOfNights <= 0)
                    {
                        ModelState.AddModelError(
                            "",
                            "Ngày trả phòng phải sau ngày nhận phòng."
                        );


                        return View(
                            "~/Views/Admin/Invoice/Create.cshtml",
                            model
                        );
                    }


                    /*
                     * =====================================
                     * TÍNH HÓA ĐƠN
                     *
                     * Hệ thống chỉ thu tiền phòng.
                     *
                     * RoomCharge    = số đêm × giá phòng
                     * ServiceCharge = 0
                     * Tax           = 0
                     * TotalAmount   = RoomCharge
                     * =====================================
                     */

                    decimal roomCharge =
                        numberOfNights *
                        pricePerNight;


                    decimal serviceCharge =
                        0m;


                    decimal tax =
                        0m;


                    decimal totalAmount =
                        roomCharge;


                    Guid invoiceId =
                        Guid.NewGuid();


                    /*
                     * Cassandra timestamp:
                     *
                     * Lưu UTC.
                     *
                     * View sẽ chuyển sang
                     * giờ địa phương khi hiển thị.
                     */

                    DateTime issuedAt =
                        DateTime.UtcNow;


                    /*
                     * =====================================
                     * INSERT INVOICE
                     * =====================================
                     */

                    string insertCql = @"
                        INSERT INTO invoices_by_booking
                        (
                            booking_id,
                            invoice_id,
                            guest_id,
                            hotel_id,
                            room_charge,
                            service_charge,
                            tax,
                            total_amount,
                            payment_status,
                            issued_at
                        )
                        VALUES
                        (?, ?, ?, ?, ?, ?, ?, ?, ?, ?);
                    ";


                    cassandra.Execute(
                        new SimpleStatement(
                            insertCql,

                            model.BookingId,

                            invoiceId,

                            bookingRow.GetValue<string>(
                                "guest_id"
                            ),

                            HOTEL_ID,

                            roomCharge,

                            serviceCharge,

                            tax,

                            totalAmount,

                            "UNPAID",

                            issuedAt
                        )
                    );


                    TempData["Success"] =
                        "Tạo hóa đơn tiền phòng thành công.";


                    /*
                     * =====================================
                     * CHUYỂN SANG CHI TIẾT HÓA ĐƠN
                     * =====================================
                     */

                    return RedirectToAction(
                        "Details",
                        new
                        {
                            bookingId =
                                model.BookingId,

                            invoiceId =
                                invoiceId
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    "Không thể tạo hóa đơn: "
                    + ex.Message
                );


                return View(
                    "~/Views/Admin/Invoice/Create.cshtml",
                    model
                );
            }
        }


        // =====================================================
        // MAP INVOICE
        // Cassandra Row -> Invoice
        // =====================================================

        private Invoice MapInvoice(Row row)
        {
            decimal roomCharge =
                row.GetValue<decimal>(
                    "room_charge"
                );


            /*
             * =====================================
             * HỆ THỐNG CHỈ TÍNH TIỀN PHÒNG
             *
             * Không lấy service_charge,
             * tax và total_amount cũ để quyết định
             * tổng tiền hiển thị.
             *
             * Điều này xử lý cả dữ liệu hóa đơn cũ
             * đang có total_amount = 2.860.000
             * nhưng room_charge = 2.400.000.
             * =====================================
             */

            return new Invoice
            {
                BookingId =
                    row.GetValue<Guid>(
                        "booking_id"
                    ),

                InvoiceId =
                    row.GetValue<Guid>(
                        "invoice_id"
                    ),

                GuestId =
                    row.GetValue<string>(
                        "guest_id"
                    ),

                HotelId =
                    row.GetValue<string>(
                        "hotel_id"
                    ),

                RoomCharge =
                    roomCharge,

                ServiceCharge =
                    0m,

                Tax =
                    0m,

                TotalAmount =
                    roomCharge,

                PaymentStatus =
                    row.GetValue<string>(
                        "payment_status"
                    ),

                IssuedAt =
                    row.GetValue<DateTime>(
                        "issued_at"
                    )
            };
        }


        // =====================================================
        // CASSANDRA DATE -> DATETIME
        // =====================================================

        private DateTime ConvertLocalDate(
            Row row,
            string columnName)
        {
            LocalDate localDate =
                row.GetValue<LocalDate>(
                    columnName
                );


            return new DateTime(
                localDate.Year,
                localDate.Month,
                localDate.Day
            );
        }
    }
}