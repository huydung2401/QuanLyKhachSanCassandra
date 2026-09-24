using Cassandra;
using System;
using System.Linq;
using System.Web.Mvc;
using QuanLyKhachSan.Services;
using QuanLyKhachSan.Models;

namespace QuanLyKhachSan.Controllers
{
    public class DashboardController : Controller
    {
        // =====================================================
        // KHÁCH SẠN DUY NHẤT
        // =====================================================

        private const string HOTEL_ID = "HOTEL001";


        // =====================================================
        // DASHBOARD
        // =====================================================

        public ActionResult Index()
        {
            try
            {
                using (var cassandra = new CassandraService())
                {
                    // =================================================
                    // 1. LẤY THÔNG TIN KHÁCH SẠN
                    // =================================================

                    var hotel =
                        cassandra.GetHotel(HOTEL_ID);


                    // =================================================
                    // 2. TẠO VIEW MODEL
                    // =================================================

                    var model =
                        new DashboardViewModel
                        {
                            HotelId = HOTEL_ID,

                            DatabaseName =
                                "Astra DB",

                            Keyspace =
                                "default_keyspace",

                            Framework =
                                "ASP.NET MVC 5"
                        };


                    // =================================================
                    // 3. ĐỌC THÔNG TIN KHÁCH SẠN
                    // =================================================

                    if (hotel != null)
                    {
                        model.HotelId =
                            hotel.GetValue<string>("hotel_id");

                        model.HotelName =
                            hotel.GetValue<string>("hotel_name");

                        model.HotelAddress =
                            hotel.GetValue<string>("address");

                        model.HotelCity =
                            hotel.GetValue<string>("city");

                        model.HotelStarRating =
                            hotel.GetValue<int>("star_rating");

                        model.HotelPhone =
                            hotel.GetValue<string>("phone");
                    }


                    // =================================================
                    // 4. LẤY DANH SÁCH PHÒNG
                    // =================================================

                    var roomRows =
                        cassandra
                            .GetRoomsByHotel(HOTEL_ID)
                            .ToList();


                    // =================================================
                    // 5. TỔNG SỐ PHÒNG
                    // =================================================

                    model.TotalRooms =
                        roomRows.Count;


                    // =================================================
                    // 6. ĐẾM PHÒNG ĐANG TRỐNG
                    // =================================================

                    model.AvailableRooms =
                        roomRows.Count(room =>
                        {
                            if (room == null ||
                                room.IsNull("status"))
                            {
                                return false;
                            }

                            string status =
                                room.GetValue<string>("status");

                            return string.Equals(
                                status,
                                "Available",
                                StringComparison.OrdinalIgnoreCase
                            );
                        });


                    // =================================================
                    // 7. TÍNH DOANH THU
                    // =================================================

                    model.TotalRevenue =
                        cassandra.GetRevenue(HOTEL_ID);


                    // =================================================
                    // 8. TRẢ VỀ DASHBOARD ADMIN
                    // =================================================

                    return View(
                        "~/Views/Admin/Dashboard/Index.cshtml",
                        model
                    );
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error =
                    "Không thể tải Dashboard: "
                    + ex.Message;


                // =================================================
                // FALLBACK
                // =================================================

                var model =
                    new DashboardViewModel
                    {
                        HotelId = HOTEL_ID,

                        TotalRooms = 0,

                        AvailableRooms = 0,

                        TotalRevenue = 0m,

                        DatabaseName =
                            "Astra DB",

                        Keyspace =
                            "default_keyspace",

                        Framework =
                            "ASP.NET MVC 5"
                    };


                return View(
                    "~/Views/Admin/Dashboard/Index.cshtml",
                    model
                );
            }
        }
    }
}