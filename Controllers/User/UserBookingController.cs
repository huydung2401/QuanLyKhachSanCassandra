using Cassandra;
using QuanLyKhachSan.Models;
using QuanLyKhachSan.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace QuanLyKhachSan.Controllers.User
{
    public class UserBookingController : Controller
    {
        private const string HOTEL_ID = "HOTEL001";


        // =====================================================
        // GET: /UserBooking/Search
        // HIỂN THỊ TRANG TÌM PHÒNG
        // =====================================================

        [HttpGet]
        public ActionResult Search()
        {
            var model = new UserBookingSearchViewModel
            {
                CheckIn = DateTime.Today.AddDays(1),
                CheckOut = DateTime.Today.AddDays(2),
                Guests = 1
            };

            return View(
                "~/Views/User/Booking/Search.cshtml",
                model
            );
        }


        // =====================================================
        // POST: /UserBooking/Search
        // TÌM PHÒNG CÒN TRỐNG
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Search(
            UserBookingSearchViewModel model)
        {
            if (model == null)
            {
                model = new UserBookingSearchViewModel();
            }


            // =====================================================
            // KIỂM TRA NGÀY NHẬN
            // =====================================================

            if (model.CheckIn.Date < DateTime.Today)
            {
                model.ErrorMessage =
                    "Ngày nhận phòng không được trước ngày hôm nay.";

                return View(
                    "~/Views/User/Booking/Search.cshtml",
                    model
                );
            }


            // =====================================================
            // KIỂM TRA NGÀY TRẢ
            // =====================================================

            if (model.CheckOut.Date <= model.CheckIn.Date)
            {
                model.ErrorMessage =
                    "Ngày trả phòng phải sau ngày nhận phòng.";

                return View(
                    "~/Views/User/Booking/Search.cshtml",
                    model
                );
            }


            // =====================================================
            // KIỂM TRA SỐ KHÁCH
            // =====================================================

            if (model.Guests < 1 || model.Guests > 10)
            {
                model.ErrorMessage =
                    "Số khách phải từ 1 đến 10.";

                return View(
                    "~/Views/User/Booking/Search.cshtml",
                    model
                );
            }


            try
            {
                using (var cassandra = new CassandraService())
                {
                    // =================================================
                    // 1. LẤY TẤT CẢ PHÒNG ĐANG AVAILABLE
                    // =================================================

                    var rooms =
                        cassandra
                            .GetRoomsByHotel(HOTEL_ID)
                            .Where(r =>
                                r != null &&
                                !r.IsNull("status") &&
                                string.Equals(
                                    r.GetValue<string>("status"),
                                    "Available",
                                    StringComparison.OrdinalIgnoreCase
                                )
                            )
                            .ToList();


                    // =================================================
                    // 2. LẤY PHÒNG ĐÃ CÓ BOOKING TRÙNG NGÀY
                    // =================================================

                    var bookedRooms =
                        GetBookedRooms(
                            cassandra,
                            model.CheckIn.Date,
                            model.CheckOut.Date
                        );


                    model.AvailableRooms =
                        new List<BookingRoomOption>();


                    // =================================================
                    // 3. LỌC PHÒNG CÒN TRỐNG
                    // =================================================

                    foreach (var room in rooms)
                    {
                        if (room.IsNull("room_number"))
                        {
                            continue;
                        }


                        int roomNumber =
                            room.GetValue<int>(
                                "room_number"
                            );


                        // Phòng đã có booking
                        if (bookedRooms.Contains(roomNumber))
                        {
                            continue;
                        }


                        string roomType =
                            room.IsNull("room_type")
                                ? ""
                                : room.GetValue<string>(
                                    "room_type"
                                );


                        decimal price =
                            room.IsNull("price_per_night")
                                ? 0m
                                : room.GetValue<decimal>(
                                    "price_per_night"
                                );


                        string imageUrl =
                            room.IsNull("image_url")
                                ? ""
                                : room.GetValue<string>(
                                    "image_url"
                                );


                        string status =
                            room.IsNull("status")
                                ? ""
                                : room.GetValue<string>(
                                    "status"
                                );


                        model.AvailableRooms.Add(
                            new BookingRoomOption
                            {
                                RoomNumber =
                                    roomNumber,

                                RoomType =
                                    roomType,

                                PricePerNight =
                                    price,

                                ImageUrl =
                                    imageUrl,

                                Status =
                                    status
                            }
                        );
                    }


                    // =================================================
                    // 4. KHÔNG CÓ PHÒNG
                    // =================================================

                    if (model.AvailableRooms.Count == 0)
                    {
                        model.ErrorMessage =
                            "Không tìm thấy phòng trống trong khoảng thời gian bạn đã chọn.";
                    }


                    return View(
                        "~/Views/User/Booking/Search.cshtml",
                        model
                    );
                }
            }
            catch (Exception ex)
            {
                model.ErrorMessage =
                    "Không thể tìm phòng: " +
                    ex.Message;

                return View(
                    "~/Views/User/Booking/Search.cshtml",
                    model
                );
            }
        }


        // =====================================================
        // LẤY CÁC PHÒNG ĐÃ ĐƯỢC BOOKING TRÙNG THỜI GIAN
        // =====================================================

        private HashSet<int> GetBookedRooms(
            CassandraService cassandra,
            DateTime checkIn,
            DateTime checkOut)
        {
            var bookedRooms =
                new HashSet<int>();


            string cql = @"
                SELECT
                    room_number,
                    check_in_date,
                    check_out_date,
                    status
                FROM bookings_by_hotel_date
                WHERE hotel_id = ?;
            ";


            var statement =
                new SimpleStatement(
                    cql,
                    HOTEL_ID
                );


            foreach (var row in cassandra.Execute(statement))
            {
                if (row == null)
                {
                    continue;
                }


                if (row.IsNull("room_number") ||
                    row.IsNull("check_in_date") ||
                    row.IsNull("check_out_date"))
                {
                    continue;
                }


                int roomNumber =
                    row.GetValue<int>(
                        "room_number"
                    );


                string status =
                    row.IsNull("status")
                        ? ""
                        : row.GetValue<string>(
                            "status"
                        );


                // Booking đã hủy không chiếm phòng
                if (string.Equals(
                        status,
                        "CANCELLED",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }


                var existingCheckIn =
                    row.GetValue<LocalDate>(
                        "check_in_date"
                    );


                var existingCheckOut =
                    row.GetValue<LocalDate>(
                        "check_out_date"
                    );


                DateTime existingIn =
                    existingCheckIn
                        .ToDateTimeOffset()
                        .Date;


                DateTime existingOut =
                    existingCheckOut
                        .ToDateTimeOffset()
                        .Date;


                /*
                 * Hai khoảng thời gian bị trùng khi:
                 *
                 * checkIn < existingOut
                 * &&
                 * checkOut > existingIn
                 */

                bool overlap =
                    checkIn < existingOut &&
                    checkOut > existingIn;


                if (overlap)
                {
                    bookedRooms.Add(
                        roomNumber
                    );
                }
            }


            return bookedRooms;
        }


        // =====================================================
        // GET: /UserBooking
        // FORM ĐẶT PHÒNG
        // =====================================================

        [HttpGet]
        public ActionResult Index(int? roomNumber)
        {
            try
            {
                using (var cassandra = new CassandraService())
                {
                    var rooms =
                        cassandra
                            .GetRoomsByHotel(HOTEL_ID)
                            .Where(r =>
                                r != null &&
                                !r.IsNull("status") &&
                                string.Equals(
                                    r.GetValue<string>("status"),
                                    "Available",
                                    StringComparison.OrdinalIgnoreCase
                                )
                            )
                            .Select(r => new BookingRoomOption
                            {
                                RoomNumber =
                                    r.IsNull("room_number")
                                        ? 0
                                        : r.GetValue<int>(
                                            "room_number"
                                        ),

                                RoomType =
                                    r.IsNull("room_type")
                                        ? ""
                                        : r.GetValue<string>(
                                            "room_type"
                                        ),

                                PricePerNight =
                                    r.IsNull("price_per_night")
                                        ? 0m
                                        : r.GetValue<decimal>(
                                            "price_per_night"
                                        ),

                                ImageUrl =
                                    r.IsNull("image_url")
                                        ? ""
                                        : r.GetValue<string>(
                                            "image_url"
                                        ),

                                Status =
                                    r.IsNull("status")
                                        ? ""
                                        : r.GetValue<string>(
                                            "status"
                                        )
                            })
                            .ToList();


                    // =================================================
                    // PHÒNG ĐƯỢC CHỌN TỪ TRANG SEARCH
                    // =================================================

                    var selectedRoom =
                        rooms.FirstOrDefault(
                            r =>
                                r.RoomNumber ==
                                roomNumber
                        );


                    var model =
                        new UserBookingViewModel
                        {
                            RoomNumber =
                                selectedRoom != null
                                    ? selectedRoom.RoomNumber
                                    : rooms.FirstOrDefault() != null
                                        ? rooms.First().RoomNumber
                                        : 0,

                            RoomType =
                                selectedRoom != null
                                    ? selectedRoom.RoomType
                                    : rooms.FirstOrDefault() != null
                                        ? rooms.First().RoomType
                                        : "",

                            PricePerNight =
                                selectedRoom != null
                                    ? selectedRoom.PricePerNight
                                    : rooms.FirstOrDefault() != null
                                        ? rooms.First().PricePerNight
                                        : 0m,

                            ImageUrl =
                                selectedRoom != null
                                    ? selectedRoom.ImageUrl
                                    : rooms.FirstOrDefault() != null
                                        ? rooms.First().ImageUrl
                                        : "",

                            CheckIn =
                                DateTime.Today.AddDays(1),

                            CheckOut =
                                DateTime.Today.AddDays(2),

                            Guests = 1
                        };


                    ViewBag.Rooms = rooms;


                    return View(
                        "~/Views/User/Booking/Index.cshtml",
                        model
                    );
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error =
                    "Không thể tải trang đặt phòng: "
                    + ex.Message;


                return View(
                    "~/Views/User/Booking/Index.cshtml",
                    new UserBookingViewModel
                    {
                        CheckIn =
                            DateTime.Today.AddDays(1),

                        CheckOut =
                            DateTime.Today.AddDays(2),

                        Guests = 1
                    }
                );
            }
        }


        // =====================================================
        // POST: /UserBooking/Confirm
        // XÁC NHẬN ĐẶT PHÒNG
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Confirm(
            UserBookingViewModel model)
        {
            try
            {
                // =================================================
                // KIỂM TRA DỮ LIỆU
                // =================================================

                if (model.CheckIn.Date < DateTime.Today)
                {
                    ModelState.AddModelError(
                        "CheckIn",
                        "Ngày nhận phòng không được ở quá khứ."
                    );
                }


                if (model.CheckOut.Date <= model.CheckIn.Date)
                {
                    ModelState.AddModelError(
                        "CheckOut",
                        "Ngày trả phòng phải sau ngày nhận phòng."
                    );
                }


                if (!ModelState.IsValid)
                {
                    LoadRooms();


                    return View(
                        "~/Views/User/Booking/Index.cshtml",
                        model
                    );
                }


                using (var cassandra = new CassandraService())
                {
                    // =================================================
                    // 1. LẤY PHÒNG
                    // =================================================

                    var room =
                        cassandra
                            .GetRoomsByHotel(HOTEL_ID)
                            .FirstOrDefault(r =>
                                r != null &&
                                !r.IsNull("room_number") &&
                                r.GetValue<int>("room_number")
                                    == model.RoomNumber
                            );


                    if (room == null)
                    {
                        ModelState.AddModelError(
                            "",
                            "Không tìm thấy phòng."
                        );


                        LoadRooms();


                        return View(
                            "~/Views/User/Booking/Index.cshtml",
                            model
                        );
                    }


                    // =================================================
                    // 2. KIỂM TRA TRẠNG THÁI PHÒNG
                    // =================================================

                    string roomStatus =
                        room.IsNull("status")
                            ? ""
                            : room.GetValue<string>(
                                "status"
                            );


                    if (!string.Equals(
                            roomStatus,
                            "Available",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        ModelState.AddModelError(
                            "",
                            "Phòng này hiện không thể đặt."
                        );


                        LoadRooms();


                        return View(
                            "~/Views/User/Booking/Index.cshtml",
                            model
                        );
                    }


                    // =================================================
                    // 3. LẤY GIÁ THỰC TẾ TỪ CASSANDRA
                    // =================================================

                    decimal price =
                        room.IsNull("price_per_night")
                            ? 0m
                            : room.GetValue<decimal>(
                                "price_per_night"
                            );


                    model.PricePerNight =
                        price;


                    // =================================================
                    // 4. LẤY ẢNH PHÒNG
                    // =================================================

                    model.ImageUrl =
                        room.IsNull("image_url")
                            ? ""
                            : room.GetValue<string>(
                                "image_url"
                            );


                    // =================================================
                    // 5. KIỂM TRA TRÙNG LỊCH
                    // =================================================

                    bool roomAlreadyBooked =
                        IsRoomBooked(
                            cassandra,
                            model.RoomNumber,
                            model.CheckIn.Date,
                            model.CheckOut.Date
                        );


                    if (roomAlreadyBooked)
                    {
                        ModelState.AddModelError(
                            "",
                            "Phòng đã được đặt trong khoảng thời gian bạn chọn. Vui lòng chọn ngày khác hoặc phòng khác."
                        );


                        LoadRooms();


                        return View(
                            "~/Views/User/Booking/Index.cshtml",
                            model
                        );
                    }


                    // =================================================
                    // 6. TẠO GUEST ID
                    // =================================================

                    string guestId =
                        "GUEST" +
                        Guid.NewGuid()
                            .ToString("N")
                            .Substring(
                                0,
                                8
                            )
                            .ToUpper();


                    // =================================================
                    // 7. TẠO BOOKING ID
                    // =================================================

                    Guid bookingId =
                        Guid.NewGuid();


                    // =================================================
                    // 8. LẤY TÊN KHÁCH SẠN
                    // =================================================

                    var hotel =
                        cassandra.GetHotel(
                            HOTEL_ID
                        );


                    string hotelName =
                        hotel != null &&
                        !hotel.IsNull("hotel_name")
                            ? hotel.GetValue<string>(
                                "hotel_name"
                            )
                            : "Sunrise Hotel";


                    // =================================================
                    // 9. TỔNG TIỀN
                    // =================================================

                    int nights =
                        (
                            model.CheckOut.Date -
                            model.CheckIn.Date
                        ).Days;


                    decimal totalAmount =
                        nights * price;


                    // =================================================
                    // 10. CHUYỂN DATE SANG LOCALDATE
                    // =================================================

                    var checkIn =
                        new LocalDate(
                            model.CheckIn.Year,
                            model.CheckIn.Month,
                            model.CheckIn.Day
                        );


                    var checkOut =
                        new LocalDate(
                            model.CheckOut.Year,
                            model.CheckOut.Month,
                            model.CheckOut.Day
                        );


                    // =================================================
                    // 11. TẠO KHÁCH HÀNG
                    // =================================================

                    cassandra.CreateGuest(
                        guestId,
                        model.FullName.Trim(),
                        model.Phone.Trim(),
                        model.Email.Trim(),
                        model.NationalId.Trim()
                    );


                    // =================================================
                    // 12. BOOKING BY GUEST
                    // =================================================

                    string cqlGuest = @"
                        INSERT INTO bookings_by_guest
                        (
                            guest_id,
                            check_in_date,
                            booking_id,
                            hotel_id,
                            hotel_name,
                            room_number,
                            check_out_date,
                            total_amount,
                            status
                        )
                        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?);
                    ";


                    // =================================================
                    // 13. BOOKING BY HOTEL DATE
                    // =================================================

                    string cqlHotel = @"
                        INSERT INTO bookings_by_hotel_date
                        (
                            hotel_id,
                            check_in_date,
                            booking_id,
                            guest_id,
                            guest_name,
                            room_number,
                            check_out_date,
                            total_amount,
                            status
                        )
                        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?);
                    ";


                    // =================================================
                    // 14. BOOKING BY PHONE
                    // =================================================

                    string cqlPhone = @"
                        INSERT INTO bookings_by_phone
                        (
                            phone,
                            check_in_date,
                            booking_id,
                            guest_id,
                            guest_name,
                            hotel_id,
                            hotel_name,
                            room_number,
                            check_out_date,
                            total_amount,
                            status
                        )
                        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);
                    ";


                    // =================================================
                    // 15. LOGGED BATCH
                    // =================================================

                    var batch =
                        new BatchStatement()
                            .SetBatchType(
                                BatchType.Logged
                            );


                    // =================================================
                    // BOOKING BY GUEST
                    // =================================================

                    batch.Add(
                        new SimpleStatement(
                            cqlGuest,

                            guestId,

                            checkIn,

                            bookingId,

                            HOTEL_ID,

                            hotelName,

                            model.RoomNumber,

                            checkOut,

                            totalAmount,

                            "CONFIRMED"
                        )
                    );


                    // =================================================
                    // BOOKING BY HOTEL DATE
                    // =================================================

                    batch.Add(
                        new SimpleStatement(
                            cqlHotel,

                            HOTEL_ID,

                            checkIn,

                            bookingId,

                            guestId,

                            model.FullName.Trim(),

                            model.RoomNumber,

                            checkOut,

                            totalAmount,

                            "CONFIRMED"
                        )
                    );


                    // =================================================
                    // BOOKING BY PHONE
                    // =================================================

                    batch.Add(
                        new SimpleStatement(
                            cqlPhone,

                            model.Phone.Trim(),

                            checkIn,

                            bookingId,

                            guestId,

                            model.FullName.Trim(),

                            HOTEL_ID,

                            hotelName,

                            model.RoomNumber,

                            checkOut,

                            totalAmount,

                            "CONFIRMED"
                        )
                    );


                    // =================================================
                    // THỰC THI BATCH
                    // =================================================

                    cassandra.ExecuteBatch(
                        batch
                    );


                    // =================================================
                    // 16. TRANG THÁI THÀNH CÔNG
                    // =================================================

                    return View(
                        "~/Views/User/Booking/Success.cshtml",

                        new UserBookingViewModel
                        {
                            RoomNumber =
                                model.RoomNumber,

                            RoomType =
                                room.IsNull("room_type")
                                    ? ""
                                    : room.GetValue<string>(
                                        "room_type"
                                    ),

                            PricePerNight =
                                price,

                            ImageUrl =
                                room.IsNull("image_url")
                                    ? ""
                                    : room.GetValue<string>(
                                        "image_url"
                                    ),

                            FullName =
                                model.FullName,

                            Phone =
                                model.Phone,

                            Email =
                                model.Email,

                            NationalId =
                                model.NationalId,

                            CheckIn =
                                model.CheckIn,

                            CheckOut =
                                model.CheckOut,

                            Guests =
                                model.Guests,

                            ErrorMessage =
                                bookingId.ToString()
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    "Không thể tạo đặt phòng: "
                    + ex.Message
                );


                LoadRooms();


                return View(
                    "~/Views/User/Booking/Index.cshtml",
                    model
                );
            }
        }


        // =====================================================
        // KIỂM TRA PHÒNG ĐÃ ĐƯỢC ĐẶT
        // =====================================================

        private bool IsRoomBooked(
            CassandraService cassandra,
            int roomNumber,
            DateTime checkIn,
            DateTime checkOut)
        {
            string cql = @"
                SELECT
                    room_number,
                    check_in_date,
                    check_out_date,
                    status
                FROM bookings_by_hotel_date
                WHERE hotel_id = ?;
            ";


            var statement =
                new SimpleStatement(
                    cql,
                    HOTEL_ID
                );


            foreach (
                var row
                in cassandra.Execute(statement))
            {
                if (row == null)
                {
                    continue;
                }


                if (row.IsNull("room_number") ||
                    row.IsNull("check_in_date") ||
                    row.IsNull("check_out_date"))
                {
                    continue;
                }


                int bookedRoom =
                    row.GetValue<int>(
                        "room_number"
                    );


                if (bookedRoom != roomNumber)
                {
                    continue;
                }


                string status =
                    row.IsNull("status")
                        ? ""
                        : row.GetValue<string>(
                            "status"
                        );


                // CANCELLED không chiếm phòng
                if (string.Equals(
                        status,
                        "CANCELLED",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }


                var existingCheckIn =
                    row.GetValue<LocalDate>(
                        "check_in_date"
                    );


                var existingCheckOut =
                    row.GetValue<LocalDate>(
                        "check_out_date"
                    );


                DateTime existingIn =
                    existingCheckIn
                        .ToDateTimeOffset()
                        .Date;


                DateTime existingOut =
                    existingCheckOut
                        .ToDateTimeOffset()
                        .Date;


                bool overlap =
                    checkIn < existingOut &&
                    checkOut > existingIn;


                if (overlap)
                {
                    return true;
                }
            }


            return false;
        }


        // =====================================================
        // GET: /UserBooking/Lookup
        // TRANG TRA CỨU ĐẶT PHÒNG
        // =====================================================

        [HttpGet]
        public ActionResult Lookup()
        {
            return View(
                "~/Views/User/Booking/Lookup.cshtml"
            );
        }

        // =====================================================
        // POST: /UserBooking/Lookup
        // TRA CỨU ĐẶT PHÒNG THEO SỐ ĐIỆN THOẠI
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Lookup(string phone)
        {
            var results = new List<BookingLookupResult>();

            try
            {
                // Kiểm tra số điện thoại
                if (string.IsNullOrWhiteSpace(phone))
                {
                    ViewBag.LookupError =
                        "Vui lòng nhập số điện thoại.";

                    return View("~/Views/User/Booking/Lookup.cshtml");
                }

                phone = phone.Trim();

                // =============================================
                // TRUY VẤN CASSANDRA
                // =============================================

                string cql = @"
            SELECT
                phone,
                check_in_date,
                booking_id,
                guest_id,
                guest_name,
                hotel_id,
                hotel_name,
                room_number,
                check_out_date,
                total_amount,
                status
            FROM bookings_by_phone
            WHERE phone = ?;
        ";

                using (var cassandra = new CassandraService())
                {
                    var statement = new SimpleStatement(
                        cql,
                        phone
                    );

                    foreach (var row in cassandra.Execute(statement))
                    {
                        if (row == null)
                        {
                            continue;
                        }

                        DateTime checkIn = DateTime.MinValue;
                        DateTime checkOut = DateTime.MinValue;

                        if (!row.IsNull("check_in_date"))
                        {
                            var localDate =
                                row.GetValue<LocalDate>(
                                    "check_in_date"
                                );

                            checkIn =
                                localDate
                                    .ToDateTimeOffset()
                                    .Date;
                        }

                        if (!row.IsNull("check_out_date"))
                        {
                            var localDate =
                                row.GetValue<LocalDate>(
                                    "check_out_date"
                                );

                            checkOut =
                                localDate
                                    .ToDateTimeOffset()
                                    .Date;
                        }

                        results.Add(
                            new BookingLookupResult
                            {
                                Phone = phone,

                                BookingId =
                                    row.IsNull("booking_id")
                                        ? ""
                                        : row.GetValue<Guid>(
                                            "booking_id"
                                        ).ToString(),

                                GuestName =
                                    row.IsNull("guest_name")
                                        ? ""
                                        : row.GetValue<string>(
                                            "guest_name"
                                        ),

                                RoomNumber =
                                    row.IsNull("room_number")
                                        ? 0
                                        : row.GetValue<int>(
                                            "room_number"
                                        ),

                                CheckIn = checkIn,

                                CheckOut = checkOut,

                                TotalAmount =
                                    row.IsNull("total_amount")
                                        ? 0m
                                        : row.GetValue<decimal>(
                                            "total_amount"
                                        ),

                                Status =
                                    row.IsNull("status")
                                        ? ""
                                        : row.GetValue<string>(
                                            "status"
                                        ),

                                HotelName =
                                    row.IsNull("hotel_name")
                                        ? ""
                                        : row.GetValue<string>(
                                            "hotel_name"
                                        )
                            }
                        );
                    }
                }

                ViewBag.LookupPhone = phone;

                if (results.Count == 0)
                {
                    ViewBag.LookupError =
                        "Không tìm thấy đặt phòng nào với số điện thoại này.";
                }

                ViewBag.LookupResults = results;

                return View(
                    "~/Views/User/Booking/Lookup.cshtml"
                );
            }
            catch (Exception ex)
            {
                ViewBag.LookupError =
                    "Không thể tra cứu đặt phòng: " +
                    ex.Message;

                return View(
                    "~/Views/User/Booking/Lookup.cshtml"
                );
            }
        }

        // =====================================================
        // LOAD ROOMS
        // =====================================================

        private void LoadRooms()
        {
            using (var cassandra =
                new CassandraService())
            {
                ViewBag.Rooms =
                    cassandra
                        .GetRoomsByHotel(HOTEL_ID)
                        .Where(r =>
                            r != null &&
                            !r.IsNull("status") &&
                            string.Equals(
                                r.GetValue<string>("status"),
                                "Available",
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                        .Select(r =>
                            new BookingRoomOption
                            {
                                RoomNumber =
                                    r.GetValue<int>(
                                        "room_number"
                                    ),

                                RoomType =
                                    r.GetValue<string>(
                                        "room_type"
                                    ),

                                PricePerNight =
                                    r.GetValue<decimal>(
                                        "price_per_night"
                                    ),

                                ImageUrl =
                                    r.IsNull("image_url")
                                        ? ""
                                        : r.GetValue<string>(
                                            "image_url"
                                        ),

                                Status =
                                    r.GetValue<string>(
                                        "status"
                                    )
                            }
                        )
                        .ToList();
            }
        }
    }
}