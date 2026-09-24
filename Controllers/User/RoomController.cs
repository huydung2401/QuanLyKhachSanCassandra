using Cassandra;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using QuanLyKhachSan.Models;
using QuanLyKhachSan.Services;

namespace QuanLyKhachSan.Controllers
{
    public class RoomController : Controller
    {
        // =====================================================
        // INDEX - DANH SÁCH PHÒNG
        // =====================================================

        public ActionResult Index(string hotelId)
        {
            try
            {
                using (var cassandra = new CassandraService())
                {
                    // Nếu chưa truyền hotelId
                    // lấy khách sạn đầu tiên
                    if (string.IsNullOrWhiteSpace(hotelId))
                    {
                        var hotels = cassandra.GetHotels();

                        var firstHotel = hotels.FirstOrDefault();

                        if (firstHotel != null)
                        {
                            hotelId =
                                firstHotel.GetValue<string>("hotel_id");
                        }
                    }

                    // Không có khách sạn
                    if (string.IsNullOrWhiteSpace(hotelId))
                    {
                        ViewBag.Error =
                            "Chưa có khách sạn trong hệ thống.";

                        return View(new List<Room>());
                    }

                    // Lấy danh sách phòng
                    var rows =
                        cassandra.GetRoomsByHotel(hotelId);

                    var rooms =
                        rows.Select(row => new Room
                        {
                            HotelId =
                                row.GetValue<string>("hotel_id"),

                            RoomNumber =
                                row.GetValue<int>("room_number"),

                            RoomType =
                                row.GetValue<string>("room_type"),

                            PricePerNight =
                                row.GetValue<decimal>("price_per_night"),

                            Status =
                                row.GetValue<string>("status"),

                            ImageUrl =
                                row.IsNull("image_url")
                                    ? null
                                    : row.GetValue<string>("image_url")

                        }).ToList();

                    ViewBag.HotelId = hotelId;

                    return View(rooms);
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error =
                    "Không thể tải danh sách phòng: "
                    + ex.Message;

                return View(new List<Room>());
            }
        }


        // =====================================================
        // DETAILS - CHI TIẾT PHÒNG
        // =====================================================

        public ActionResult Details(
            string hotelId,
            int? roomNumber)
        {
            try
            {
                // Kiểm tra hotelId
                if (string.IsNullOrWhiteSpace(hotelId))
                {
                    return HttpNotFound(
                        "Thiếu mã khách sạn.");
                }

                // Kiểm tra roomNumber
                if (!roomNumber.HasValue)
                {
                    return HttpNotFound(
                        "Thiếu số phòng.");
                }

                using (var cassandra =
                    new CassandraService())
                {
                    var row =
                        cassandra.GetRoom(
                            hotelId,
                            roomNumber.Value);

                    if (row == null)
                    {
                        return HttpNotFound(
                            "Không tìm thấy phòng "
                            + roomNumber.Value
                            + " của khách sạn "
                            + hotelId
                            + ".");
                    }

                    var room = MapRoom(row);

                    return View(room);
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error =
                    "Không thể tải thông tin phòng: "
                    + ex.Message;

                return View();
            }
        }


        // =====================================================
        // CREATE - GET
        // =====================================================

        public ActionResult Create(string hotelId)
        {
            if (string.IsNullOrWhiteSpace(hotelId))
            {
                try
                {
                    using (var cassandra =
                        new CassandraService())
                    {
                        var hotels =
                            cassandra.GetHotels();

                        var firstHotel =
                            hotels.FirstOrDefault();

                        if (firstHotel != null)
                        {
                            hotelId =
                                firstHotel.GetValue<string>(
                                    "hotel_id");
                        }
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.Error = ex.Message;
                }
            }

            var model = new Room
            {
                HotelId = hotelId,
                Status = "Available"
            };

            return View(model);
        }


        // =====================================================
        // CREATE - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(
            Room model,
            HttpPostedFileBase roomImage)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Kiểm tra hotelId
                if (string.IsNullOrWhiteSpace(model.HotelId))
                {
                    ModelState.AddModelError(
                        "",
                        "Mã khách sạn không được để trống.");

                    return View(model);
                }

                using (var cassandra =
                    new CassandraService())
                {
                    // -----------------------------------------
                    // 1. Kiểm tra phòng đã tồn tại
                    // -----------------------------------------

                    var existing =
                        cassandra.GetRoom(
                            model.HotelId,
                            model.RoomNumber);

                    if (existing != null)
                    {
                        ModelState.AddModelError(
                            "RoomNumber",
                            "Phòng này đã tồn tại.");

                        return View(model);
                    }

                    // -----------------------------------------
                    // 2. Upload ảnh
                    // -----------------------------------------

                    string imageUrl = null;

                    if (roomImage != null &&
                        roomImage.ContentLength > 0)
                    {
                        imageUrl =
                            SaveRoomImage(
                                roomImage,
                                model.HotelId,
                                model.RoomNumber);
                    }

                    // -----------------------------------------
                    // 3. Lưu phòng
                    // -----------------------------------------

                    cassandra.CreateRoom(
                        model.HotelId,
                        model.RoomNumber,
                        model.RoomType,
                        model.PricePerNight,
                        model.Status,
                        imageUrl);
                }

                TempData["Success"] =
                    "Thêm phòng thành công.";

                return RedirectToAction(
                    "Index",
                    new
                    {
                        hotelId = model.HotelId
                    });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    "Không thể thêm phòng: "
                    + ex.Message);

                return View(model);
            }
        }


        // =====================================================
        // EDIT - GET
        // =====================================================

        public ActionResult Edit(
            string hotelId,
            int? roomNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(hotelId))
                {
                    return HttpNotFound(
                        "Thiếu mã khách sạn.");
                }

                if (!roomNumber.HasValue)
                {
                    return HttpNotFound(
                        "Thiếu số phòng.");
                }

                using (var cassandra =
                    new CassandraService())
                {
                    var row =
                        cassandra.GetRoom(
                            hotelId,
                            roomNumber.Value);

                    if (row == null)
                    {
                        return HttpNotFound(
                            "Không tìm thấy phòng.");
                    }

                    var room = MapRoom(row);

                    return View(room);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Không thể tải phòng: "
                    + ex.Message;

                return RedirectToAction(
                    "Index",
                    new
                    {
                        hotelId = hotelId
                    });
            }
        }


        // =====================================================
        // EDIT - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(
            Room model,
            HttpPostedFileBase roomImage)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string oldImageUrl = null;
            string newImageUrl = null;

            try
            {
                using (var cassandra =
                    new CassandraService())
                {
                    // -----------------------------------------
                    // 1. Lấy phòng hiện tại
                    // -----------------------------------------

                    var existing =
                        cassandra.GetRoom(
                            model.HotelId,
                            model.RoomNumber);

                    if (existing == null)
                    {
                        return HttpNotFound(
                            "Không tìm thấy phòng.");
                    }

                    oldImageUrl =
                        existing.IsNull("image_url")
                            ? null
                            : existing.GetValue<string>(
                                "image_url");

                    // -----------------------------------------
                    // 2. Mặc định giữ ảnh cũ
                    // -----------------------------------------

                    newImageUrl = oldImageUrl;

                    // -----------------------------------------
                    // 3. Nếu chọn ảnh mới
                    // -----------------------------------------

                    if (roomImage != null &&
                        roomImage.ContentLength > 0)
                    {
                        newImageUrl =
                            SaveRoomImage(
                                roomImage,
                                model.HotelId,
                                model.RoomNumber);
                    }

                    // -----------------------------------------
                    // 4. Cập nhật Cassandra
                    // -----------------------------------------

                    cassandra.UpdateRoom(
                        model.HotelId,
                        model.RoomNumber,
                        model.RoomType,
                        model.PricePerNight,
                        model.Status,
                        newImageUrl);
                }

                // ---------------------------------------------
                // 5. Nếu có ảnh mới thì xóa ảnh cũ
                // ---------------------------------------------

                if (roomImage != null &&
                    roomImage.ContentLength > 0 &&
                    !string.IsNullOrWhiteSpace(oldImageUrl))
                {
                    DeleteRoomImageFile(oldImageUrl);
                }

                TempData["Success"] =
                    "Cập nhật phòng thành công.";

                return RedirectToAction(
                    "Index",
                    new
                    {
                        hotelId = model.HotelId
                    });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    "Không thể cập nhật phòng: "
                    + ex.Message);

                return View(model);
            }
        }


        // =====================================================
        // DELETE - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(
            string hotelId,
            int? roomNumber)
        {
            if (string.IsNullOrWhiteSpace(hotelId))
            {
                TempData["Error"] =
                    "Thiếu mã khách sạn.";

                return RedirectToAction("Index");
            }

            if (!roomNumber.HasValue)
            {
                TempData["Error"] =
                    "Thiếu số phòng.";

                return RedirectToAction(
                    "Index",
                    new
                    {
                        hotelId = hotelId
                    });
            }

            string imageUrl = null;

            try
            {
                using (var cassandra =
                    new CassandraService())
                {
                    // -----------------------------------------
                    // 1. Lấy phòng
                    // -----------------------------------------

                    var row =
                        cassandra.GetRoom(
                            hotelId,
                            roomNumber.Value);

                    if (row == null)
                    {
                        TempData["Error"] =
                            "Không tìm thấy phòng.";

                        return RedirectToAction(
                            "Index",
                            new
                            {
                                hotelId = hotelId
                            });
                    }

                    imageUrl =
                        row.IsNull("image_url")
                            ? null
                            : row.GetValue<string>(
                                "image_url");

                    // -----------------------------------------
                    // 2. Xóa phòng Cassandra
                    // -----------------------------------------

                    cassandra.DeleteRoom(
                        hotelId,
                        roomNumber.Value);
                }

                // ---------------------------------------------
                // 3. Xóa ảnh
                // ---------------------------------------------

                if (!string.IsNullOrWhiteSpace(imageUrl))
                {
                    DeleteRoomImageFile(imageUrl);
                }

                TempData["Success"] =
                    "Xóa phòng thành công.";
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Không thể xóa phòng: "
                    + ex.Message;
            }

            return RedirectToAction(
                "Index",
                new
                {
                    hotelId = hotelId
                });
        }


        // =====================================================
        // MAP CASSANDRA ROW -> ROOM
        // =====================================================

        private Room MapRoom(Row row)
        {
            return new Room
            {
                HotelId =
                    row.GetValue<string>(
                        "hotel_id"),

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
                    row.GetValue<string>(
                        "status"),

                ImageUrl =
                    row.IsNull("image_url")
                        ? null
                        : row.GetValue<string>(
                            "image_url")
            };
        }


        // =====================================================
        // SAVE ROOM IMAGE
        // =====================================================

        private string SaveRoomImage(
            HttpPostedFileBase image,
            string hotelId,
            int roomNumber)
        {
            if (image == null ||
                image.ContentLength <= 0)
            {
                return null;
            }

            // ---------------------------------------------
            // Giới hạn 5 MB
            // ---------------------------------------------

            const int maxFileSize =
                5 * 1024 * 1024;

            if (image.ContentLength >
                maxFileSize)
            {
                throw new Exception(
                    "Ảnh không được vượt quá 5 MB.");
            }

            // ---------------------------------------------
            // Extension
            // ---------------------------------------------

            string extension =
                Path.GetExtension(
                    image.FileName);

            if (string.IsNullOrWhiteSpace(extension))
            {
                throw new Exception(
                    "File ảnh không có phần mở rộng.");
            }

            extension =
                extension.ToLowerInvariant();

            string[] allowedExtensions =
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp"
            };

            if (!allowedExtensions.Contains(extension))
            {
                throw new Exception(
                    "Chỉ cho phép ảnh JPG, JPEG, PNG hoặc WEBP.");
            }

            // ---------------------------------------------
            // Tạo thư mục
            // ---------------------------------------------

            string folder =
                Server.MapPath(
                    "~/Content/Images/Rooms");

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            // ---------------------------------------------
            // Tên file
            // ---------------------------------------------

            string fileName =
                hotelId
                + "_"
                + roomNumber
                + extension;

            string filePath =
                Path.Combine(
                    folder,
                    fileName);

            // ---------------------------------------------
            // Xóa file cũ nếu có
            // ---------------------------------------------

            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            // ---------------------------------------------
            // Lưu file
            // ---------------------------------------------

            image.SaveAs(filePath);

            // ---------------------------------------------
            // URL lưu Cassandra
            // ---------------------------------------------

            return
                "/Content/Images/Rooms/"
                + fileName;
        }


        // =====================================================
        // DELETE ROOM IMAGE FILE
        // =====================================================

        private void DeleteRoomImageFile(
            string imageUrl)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(imageUrl))
                {
                    return;
                }

                const string prefix =
                    "/Content/Images/Rooms/";

                if (!imageUrl.StartsWith(
                    prefix,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                string fileName =
                    Path.GetFileName(imageUrl);

                if (string.IsNullOrWhiteSpace(fileName))
                {
                    return;
                }

                string folder =
                    Server.MapPath(
                        "~/Content/Images/Rooms");

                string filePath =
                    Path.Combine(
                        folder,
                        fileName);

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
            catch
            {
                // Không để lỗi xóa ảnh
                // làm hỏng thao tác chính.
            }
        }
    }
}