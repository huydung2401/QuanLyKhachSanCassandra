using Cassandra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using QuanLyKhachSan.Models;
using QuanLyKhachSan.Services;

namespace QuanLyKhachSan.Controllers.Admin
{
    public class BookingController : Controller
    {
        private const string HOTEL_ID = "HOTEL001";


        // =====================================================
        // INDEX
        // =====================================================

        public ActionResult Index()
        {
            try
            {
                using (var cassandra = new CassandraService())
                {
                    string cql = @"
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

                    var statement =
                        new SimpleStatement(
                            cql,
                            HOTEL_ID
                        );

                    var rows =
                        cassandra.Execute(statement);

                    var bookings =
                        rows.Select(MapBooking)
                            .ToList();

                    return View(
                        "~/Views/Admin/Booking/Index.cshtml",
                        bookings
                    );
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error =
                    "Không thể tải danh sách đặt phòng: "
                    + ex.Message;

                return View(
                    "~/Views/Admin/Booking/Index.cshtml",
                    new List<Booking>()
                );
            }
        }


        // =====================================================
        // DETAILS
        // =====================================================

        public ActionResult Details(
            string bookingId,
            string checkInDate)
        {
            if (string.IsNullOrWhiteSpace(bookingId))
            {
                return HttpNotFound(
                    "Thiếu mã đặt phòng."
                );
            }

            if (string.IsNullOrWhiteSpace(checkInDate))
            {
                return HttpNotFound(
                    "Thiếu ngày nhận phòng."
                );
            }


            Guid bookingGuid;

            if (!Guid.TryParse(
                bookingId,
                out bookingGuid))
            {
                return HttpNotFound(
                    "Mã đặt phòng không hợp lệ."
                );
            }


            DateTime checkInDateValue;

            if (!DateTime.TryParse(
                checkInDate,
                out checkInDateValue))
            {
                return HttpNotFound(
                    "Ngày nhận phòng không hợp lệ."
                );
            }


            try
            {
                using (var cassandra =
                    new CassandraService())
                {
                    /*
                     * Cassandra DATE
                     * không truyền DateTime trực tiếp.
                     *
                     * Chuyển sang LocalDate.
                     */
                    var localDate =
                        new LocalDate(
                            checkInDateValue.Year,
                            checkInDateValue.Month,
                            checkInDateValue.Day
                        );


                    string cql = @"
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
                        WHERE hotel_id = ?
                        AND check_in_date = ?
                        AND booking_id = ?;
                    ";


                    var statement =
                        new SimpleStatement(
                            cql,
                            HOTEL_ID,
                            localDate,
                            bookingGuid
                        );


                    var row =
                        cassandra
                            .Execute(statement)
                            .FirstOrDefault();


                    if (row == null)
                    {
                        return HttpNotFound(
                            "Không tìm thấy đặt phòng."
                        );
                    }


                    var booking =
                        MapBooking(row);


                    return View(
                        "~/Views/Admin/Booking/Details.cshtml",
                        booking
                    );
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error =
                    "Không thể tải thông tin đặt phòng: "
                    + ex.Message;

                return View(
                    "~/Views/Admin/Booking/Details.cshtml",
                    null
                );
            }
        }


        // =====================================================
        // MAP ROW -> BOOKING
        // =====================================================

        private Booking MapBooking(Row row)
        {
            /*
             * ================================================
             * CHECK-IN DATE
             * ================================================
             */

            LocalDate checkInLocalDate =
                row.GetValue<LocalDate>(
                    "check_in_date"
                );


            DateTime checkInDate =
                new DateTime(
                    checkInLocalDate.Year,
                    checkInLocalDate.Month,
                    checkInLocalDate.Day
                );


            /*
             * ================================================
             * CHECK-OUT DATE
             * ================================================
             */

            LocalDate checkOutLocalDate =
                row.GetValue<LocalDate>(
                    "check_out_date"
                );


            DateTime checkOutDate =
                new DateTime(
                    checkOutLocalDate.Year,
                    checkOutLocalDate.Month,
                    checkOutLocalDate.Day
                );


            /*
             * ================================================
             * BOOKING
             * ================================================
             */

            return new Booking
            {
                HotelId =
                    row.GetValue<string>(
                        "hotel_id"
                    ),

                CheckInDate =
                    checkInDate,

                BookingId =
                    row.GetValue<Guid>(
                        "booking_id"
                    ),

                GuestId =
                    row.GetValue<string>(
                        "guest_id"
                    ),

                GuestName =
                    row.GetValue<string>(
                        "guest_name"
                    ),

                RoomNumber =
                    row.GetValue<int>(
                        "room_number"
                    ),

                CheckOutDate =
                    checkOutDate,

                TotalAmount =
                    row.GetValue<decimal>(
                        "total_amount"
                    ),

                Status =
                    row.GetValue<string>(
                        "status"
                    )
            };
        }
    }
}