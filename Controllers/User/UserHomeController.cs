using System;
using System.Linq;
using System.Web.Mvc;
using QuanLyKhachSan.Models;
using QuanLyKhachSan.Services;

namespace QuanLyKhachSan.Controllers
{
    public class UserHomeController : Controller
    {
        private const string HOTEL_ID = "HOTEL001";


        // =====================================================
        // TRANG CHỦ USER
        // =====================================================

        public ActionResult Index()
        {
            try
            {
                using (var cassandra =
                    new CassandraService())
                {
                    // =============================================
                    // LẤY KHÁCH SẠN
                    // =============================================

                    var hotel =
                        cassandra.GetHotel(HOTEL_ID);


                    // =============================================
                    // TẠO MODEL
                    // =============================================

                    var model =
                        new UserHomeViewModel
                        {
                            HotelId = HOTEL_ID,
                            HotelName = "Sunrise Hotel",
                            City = "TP. Hồ Chí Minh",
                            Phone = "0900 123 456",
                            StarRating = 4
                        };


                    // =============================================
                    // ĐỌC HOTEL TỪ CASSANDRA
                    // =============================================

                    if (hotel != null)
                    {
                        model.HotelId =
                            hotel.GetValue<string>(
                                "hotel_id");

                        model.HotelName =
                            hotel.GetValue<string>(
                                "hotel_name");

                        model.Address =
                            hotel.GetValue<string>(
                                "address");

                        model.City =
                            hotel.GetValue<string>(
                                "city");

                        model.Phone =
                            hotel.GetValue<string>(
                                "phone");

                        model.StarRating =
                            hotel.GetValue<int>(
                                "star_rating");
                    }


                    // =============================================
                    // LẤY PHÒNG
                    // =============================================

                    var roomRows =
                        cassandra
                            .GetRoomsByHotel(HOTEL_ID)
                            .ToList();


                    foreach (var row in roomRows)
                    {
                        if (row == null)
                        {
                            continue;
                        }

                        var room =
                            new UserRoomViewModel
                            {
                                HotelId = HOTEL_ID,

                                RoomNumber =
                                    row.GetValue<int>(
                                        "room_number"),

                                RoomType =
                                    row.GetValue<string>(
                                        "room_type"),

                                PricePerNight =
                                    row.GetValue<decimal>(
                                        "price_per_night"),

                                Status =
                                    row.IsNull("status")
                                        ? ""
                                        : row.GetValue<string>(
                                            "status"),

                                ImageUrl =
                                    row.IsNull("image_url")
                                        ? null
                                        : row.GetValue<string>(
                                            "image_url")
                            };

                        model.Rooms.Add(room);
                    }


                    return View(
                        "~/Views/User/Home/Index.cshtml",
                        model
                    );
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error =
                    "Không thể tải trang khách sạn: "
                    + ex.Message;

                return View(
                    "~/Views/User/Home/Index.cshtml",
                    new UserHomeViewModel
                    {
                        HotelId = HOTEL_ID,
                        HotelName = "Sunrise Hotel",
                        City = "TP. Hồ Chí Minh",
                        Phone = "0900 123 456",
                        StarRating = 4
                    }
                );
            }
        }
    }
}