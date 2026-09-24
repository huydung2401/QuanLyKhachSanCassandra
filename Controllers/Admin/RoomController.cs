using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Cassandra;
using QuanLyKhachSan.Models;
using QuanLyKhachSan.Services;

namespace QuanLyKhachSan.Controllers.Admin
{
    public class RoomController : Controller
    {
        private const string HOTEL_ID = "HOTEL001";

        // =====================================================
        // INDEX
        // GET: /Admin/Room
        // =====================================================

        public ActionResult Index(string hotelId = HOTEL_ID)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(hotelId))
                {
                    hotelId = HOTEL_ID;
                }

                using (var cassandra = new CassandraService())
                {
                    var rows = cassandra.GetRoomsByHotel(hotelId);

                    var rooms = rows
                        .Select(MapRoom)
                        .ToList();

                    ViewBag.HotelId = hotelId;

                    return View(
                        "~/Views/Admin/Room/Index.cshtml",
                        rooms
                    );
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error =
                    "Không thể tải danh sách phòng: " + ex.Message;

                ViewBag.HotelId = hotelId;

                return View(
                    "~/Views/Admin/Room/Index.cshtml",
                    new List<Room>()
                );
            }
        }


        // =====================================================
        // DETAILS
        // GET: /Admin/Room/Details
        // =====================================================

        public ActionResult Details(
            string hotelId,
            int? roomNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(hotelId))
                {
                    hotelId = HOTEL_ID;
                }

                if (!roomNumber.HasValue)
                {
                    return HttpNotFound();
                }

                using (var cassandra = new CassandraService())
                {
                    var row = cassandra.GetRoom(
                        hotelId,
                        roomNumber.Value
                    );

                    if (row == null)
                    {
                        return HttpNotFound();
                    }

                    var room = MapRoom(row);

                    return View(
                        "~/Views/Admin/Room/Details.cshtml",
                        room
                    );
                }
            }
            catch (Exception ex)
            {
                return Content(
                    "Không thể tải thông tin phòng: " +
                    ex.Message
                );
            }
        }


        // =====================================================
        // CREATE - GET
        // GET: /Admin/Room/Create
        // =====================================================

        [HttpGet]
        public ActionResult Create(string hotelId = HOTEL_ID)
        {
            if (string.IsNullOrWhiteSpace(hotelId))
            {
                hotelId = HOTEL_ID;
            }

            var model = new Room
            {
                HotelId = hotelId,
                Status = "Available"
            };

            return View(
                "~/Views/Admin/Room/Create.cshtml",
                model
            );
        }


        // =====================================================
        // CREATE - POST
        // POST: /Admin/Room/Create
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(
            Room model,
            HttpPostedFileBase roomImage)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.HotelId))
                {
                    model.HotelId = HOTEL_ID;
                }

                if (model.RoomNumber <= 0)
                {
                    ModelState.AddModelError(
                        "RoomNumber",
                        "Số phòng phải lớn hơn 0."
                    );
                }

                if (string.IsNullOrWhiteSpace(model.RoomType))
                {
                    ModelState.AddModelError(
                        "RoomType",
                        "Vui lòng chọn loại phòng."
                    );
                }

                if (model.PricePerNight < 0)
                {
                    ModelState.AddModelError(
                        "PricePerNight",
                        "Giá phòng không được âm."
                    );
                }

                if (!ModelState.IsValid)
                {
                    return View(
                        "~/Views/Admin/Room/Create.cshtml",
                        model
                    );
                }

                string imageUrl = null;

                if (roomImage != null &&
                    roomImage.ContentLength > 0)
                {
                    imageUrl = SaveRoomImage(
                        roomImage,
                        model.HotelId,
                        model.RoomNumber
                    );
                }

                using (var cassandra = new CassandraService())
                {
                    var existing = cassandra.GetRoom(
                        model.HotelId,
                        model.RoomNumber
                    );

                    if (existing != null)
                    {
                        ModelState.AddModelError(
                            "",
                            "Phòng này đã tồn tại."
                        );

                        if (!string.IsNullOrWhiteSpace(imageUrl))
                        {
                            DeleteRoomImageFile(imageUrl);
                        }

                        return View(
                            "~/Views/Admin/Room/Create.cshtml",
                            model
                        );
                    }

                    cassandra.CreateRoom(
                        model.HotelId,
                        model.RoomNumber,
                        model.RoomType,
                        model.PricePerNight,
                        model.Status,
                        imageUrl
                    );
                }

                TempData["Success"] =
                    "Thêm phòng thành công.";

                return RedirectToAction(
                    "Index",
                    new
                    {
                        hotelId = model.HotelId
                    }
                );
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    "Không thể thêm phòng: " +
                    ex.Message
                );

                return View(
                    "~/Views/Admin/Room/Create.cshtml",
                    model
                );
            }
        }


        // =====================================================
        // EDIT - GET
        // GET: /Admin/Room/Edit
        // =====================================================

        [HttpGet]
        public ActionResult Edit(
            string hotelId,
            int? roomNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(hotelId))
                {
                    hotelId = HOTEL_ID;
                }

                if (!roomNumber.HasValue)
                {
                    return HttpNotFound();
                }

                using (var cassandra = new CassandraService())
                {
                    var row = cassandra.GetRoom(
                        hotelId,
                        roomNumber.Value
                    );

                    if (row == null)
                    {
                        return HttpNotFound();
                    }

                    var room = MapRoom(row);

                    return View(
                        "~/Views/Admin/Room/Edit.cshtml",
                        room
                    );
                }
            }
            catch (Exception ex)
            {
                return Content(
                    "Không thể tải phòng để sửa: " +
                    ex.Message
                );
            }
        }


        // =====================================================
        // EDIT - POST
        // POST: /Admin/Room/Edit
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(
            Room model,
            HttpPostedFileBase roomImage)
        {
            string oldImageUrl = null;
            string newImageUrl = model.ImageUrl;

            try
            {
                if (string.IsNullOrWhiteSpace(model.HotelId))
                {
                    model.HotelId = HOTEL_ID;
                }

                if (!ModelState.IsValid)
                {
                    return View(
                        "~/Views/Admin/Room/Edit.cshtml",
                        model
                    );
                }

                using (var cassandra = new CassandraService())
                {
                    var existing = cassandra.GetRoom(
                        model.HotelId,
                        model.RoomNumber
                    );

                    if (existing == null)
                    {
                        return HttpNotFound();
                    }

                    oldImageUrl =
                        existing.IsNull("image_url")
                            ? null
                            : existing.GetValue<string>(
                                "image_url"
                            );

                    newImageUrl = oldImageUrl;

                    if (roomImage != null &&
                        roomImage.ContentLength > 0)
                    {
                        newImageUrl = SaveRoomImage(
                            roomImage,
                            model.HotelId,
                            model.RoomNumber
                        );
                    }

                    cassandra.UpdateRoom(
                        model.HotelId,
                        model.RoomNumber,
                        model.RoomType,
                        model.PricePerNight,
                        model.Status,
                        newImageUrl
                    );
                }

                if (roomImage != null &&
                    roomImage.ContentLength > 0 &&
                    !string.IsNullOrWhiteSpace(
                        oldImageUrl))
                {
                    DeleteRoomImageFile(
                        oldImageUrl
                    );
                }

                TempData["Success"] =
                    "Cập nhật phòng thành công.";

                return RedirectToAction(
                    "Index",
                    new
                    {
                        hotelId = model.HotelId
                    }
                );
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    "Không thể cập nhật phòng: " +
                    ex.Message
                );

                return View(
                    "~/Views/Admin/Room/Edit.cshtml",
                    model
                );
            }
        }


        // =====================================================
        // DELETE - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(
            string hotelId,
            int roomNumber)
        {
            string imageUrl = null;

            try
            {
                if (string.IsNullOrWhiteSpace(hotelId))
                {
                    hotelId = HOTEL_ID;
                }

                using (var cassandra = new CassandraService())
                {
                    var row = cassandra.GetRoom(
                        hotelId,
                        roomNumber
                    );

                    if (row == null)
                    {
                        TempData["Error"] =
                            "Không tìm thấy phòng.";

                        return RedirectToAction(
                            "Index",
                            new
                            {
                                hotelId = hotelId
                            }
                        );
                    }

                    imageUrl =
                        row.IsNull("image_url")
                            ? null
                            : row.GetValue<string>(
                                "image_url"
                            );

                    cassandra.DeleteRoom(
                        hotelId,
                        roomNumber
                    );
                }

                if (!string.IsNullOrWhiteSpace(imageUrl))
                {
                    DeleteRoomImageFile(
                        imageUrl
                    );
                }

                TempData["Success"] =
                    "Xóa phòng thành công.";
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Không thể xóa phòng: " +
                    ex.Message;
            }

            return RedirectToAction(
                "Index",
                new
                {
                    hotelId = hotelId
                }
            );
        }


        // =====================================================
        // MAP CASSANDRA ROW → ROOM
        // =====================================================

        private Room MapRoom(Row row)
        {
            return new Room
            {
                HotelId =
                    row.GetValue<string>(
                        "hotel_id"
                    ),

                RoomNumber =
                    row.GetValue<int>(
                        "room_number"
                    ),

                RoomType =
                    row.IsNull("room_type")
                        ? ""
                        : row.GetValue<string>(
                            "room_type"
                        ),

                PricePerNight =
                    row.IsNull("price_per_night")
                        ? 0m
                        : row.GetValue<decimal>(
                            "price_per_night"
                        ),

                Status =
                    row.IsNull("status")
                        ? ""
                        : row.GetValue<string>(
                            "status"
                        ),

                ImageUrl =
                    row.IsNull("image_url")
                        ? ""
                        : row.GetValue<string>(
                            "image_url"
                        )
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

            const int maxFileSize =
                5 * 1024 * 1024;

            if (image.ContentLength >
                maxFileSize)
            {
                throw new Exception(
                    "Ảnh không được vượt quá 5 MB."
                );
            }

            string extension =
                Path.GetExtension(
                    image.FileName
                );

            if (string.IsNullOrWhiteSpace(extension))
            {
                throw new Exception(
                    "File ảnh không có phần mở rộng."
                );
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

            if (!allowedExtensions.Contains(
                extension))
            {
                throw new Exception(
                    "Chỉ cho phép ảnh JPG, JPEG, PNG hoặc WEBP."
                );
            }

            string folder =
                Server.MapPath(
                    "~/Content/Images/Rooms"
                );

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            string fileName =
                hotelId +
                "_" +
                roomNumber +
                extension;

            string filePath =
                Path.Combine(
                    folder,
                    fileName
                );

            image.SaveAs(filePath);

            return
                "/Content/Images/Rooms/" +
                fileName;
        }


        // =====================================================
        // DELETE ROOM IMAGE
        // =====================================================

        private void DeleteRoomImageFile(
            string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return;
            }

            string relativePath =
                imageUrl.TrimStart(
                    '/'
                ).Replace(
                    '/',
                    Path.DirectorySeparatorChar
                );

            string fullPath =
                Server.MapPath(
                    "~/" + relativePath
                );

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }
    }
}