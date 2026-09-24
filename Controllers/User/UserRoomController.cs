using Cassandra;
using QuanLyKhachSan.Services;
using QuanLyKhachSan.Models;
using System;
using System.Web.Mvc;

namespace QuanLyKhachSan.Controllers
{
    public class UserRoomController : Controller
    {
        // =====================================================
        // KHÁCH SẠN DUY NHẤT
        // =====================================================

        private const string HOTEL_ID = "HOTEL001";


        // =====================================================
        // CHI TIẾT PHÒNG
        //
        // Ví dụ:
        //
        // /UserRoom/Details/101
        // /UserRoom/Details/102
        // /UserRoom/Details/201
        // /UserRoom/Details/301
        //
        // =====================================================

        public ActionResult Details(int? id)
        {
            // =================================================
            // KIỂM TRA ROOM NUMBER
            // =================================================

            if (!id.HasValue)
            {
                return HttpNotFound();
            }


            try
            {
                // =================================================
                // KẾT NỐI CASSANDRA
                // =================================================

                using (var cassandra = new CassandraService())
                {
                    // =================================================
                    // LẤY PHÒNG TỪ CASSANDRA
                    //
                    // CassandraService của bạn đã có sẵn:
                    //
                    // GetRoom(hotelId, roomNumber)
                    // =================================================

                    Row room =
                        cassandra.GetRoom(
                            HOTEL_ID,
                            id.Value
                        );


                    // =================================================
                    // KHÔNG TÌM THẤY PHÒNG
                    // =================================================

                    if (room == null)
                    {
                        return HttpNotFound();
                    }


                    // =================================================
                    // TẠO VIEW MODEL
                    // =================================================

                    var model =
                        new UserRoomDetailViewModel
                        {
                            // -----------------------------------------
                            // HOTEL
                            // -----------------------------------------

                            HotelId = HOTEL_ID,


                            // -----------------------------------------
                            // ROOM NUMBER
                            // -----------------------------------------

                            RoomNumber =
                                room.IsNull("room_number")
                                    ? 0
                                    : room.GetValue<int>(
                                        "room_number"
                                    ),


                            // -----------------------------------------
                            // ROOM TYPE
                            // -----------------------------------------

                            RoomType =
                                room.IsNull("room_type")
                                    ? ""
                                    : room.GetValue<string>(
                                        "room_type"
                                    ),


                            // -----------------------------------------
                            // PRICE
                            // -----------------------------------------

                            PricePerNight =
                                room.IsNull("price_per_night")
                                    ? 0m
                                    : room.GetValue<decimal>(
                                        "price_per_night"
                                    ),


                            // -----------------------------------------
                            // STATUS
                            // -----------------------------------------

                            Status =
                                room.IsNull("status")
                                    ? ""
                                    : room.GetValue<string>(
                                        "status"
                                    ),


                            // -----------------------------------------
                            // IMAGE
                            // -----------------------------------------

                            ImageUrl =
                                room.IsNull("image_url")
                                    ? ""
                                    : room.GetValue<string>(
                                        "image_url"
                                    ),


                            // -----------------------------------------
                            // THÔNG TIN BỔ SUNG
                            // -----------------------------------------
                            //
                            // Tạm thời chưa có trong Cassandra.
                            // Sau này sẽ đưa vào database.
                            //

                            Capacity = 2,

                            Area = 28,

                            Description =
                                "Không gian nghỉ dưỡng hiện đại, " +
                                "tiện nghi và thoải mái, được thiết kế " +
                                "để mang đến trải nghiệm lưu trú thư giãn " +
                                "cho khách hàng.",


                            // -----------------------------------------
                            // ĐÁNH GIÁ
                            // -----------------------------------------
                            //
                            // Tạm thời chưa có bảng review.
                            //

                            Rating = 4.8m,

                            ReviewCount = 24
                        };


                    // =================================================
                    // TRẢ VỀ VIEW
                    // =================================================

                    return View(
                        "~/Views/User/Room/Details.cshtml",
                        model
                    );
                }
            }
            catch (Exception ex)
            {
                // =================================================
                // XỬ LÝ LỖI
                // =================================================

                ViewBag.Error =
                    "Không thể tải thông tin phòng: "
                    + ex.Message;


                // =================================================
                // MODEL RỖNG
                // =================================================

                var model =
                    new UserRoomDetailViewModel
                    {
                        HotelId = HOTEL_ID,

                        RoomNumber = id.Value,

                        RoomType = "",

                        PricePerNight = 0m,

                        Status = "",

                        ImageUrl = "",

                        Capacity = 2,

                        Area = 28,

                        Description = "",

                        Rating = 0m,

                        ReviewCount = 0
                    };


                return View(
                    "~/Views/User/Room/Details.cshtml",
                    model
                );
            }
        }
    }
}