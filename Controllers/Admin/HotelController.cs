using System;
using System.Linq;
using System.Web.Mvc;
using QuanLyKhachSan.Models;
using QuanLyKhachSan.Services;

namespace QuanLyKhachSan.Controllers
{
    public class HotelController : Controller
    {
        // =====================================================
        // KHÁCH SẠN DUY NHẤT
        // =====================================================

        private const string HOTEL_ID = "HOTEL001";


        // =====================================================
        // THÔNG TIN KHÁCH SẠN
        // GET: /Hotel
        // =====================================================

        public ActionResult Index()
        {
            try
            {
                using (var db = new CassandraService())
                {
                    var row = db.GetHotel(HOTEL_ID);

                    if (row == null)
                    {
                        return View(new Hotel());
                    }

                    var hotel = new Hotel
                    {
                        HotelId = row.GetValue<string>("hotel_id"),
                        HotelName = row.GetValue<string>("hotel_name"),
                        Address = row.GetValue<string>("address"),
                        City = row.GetValue<string>("city"),
                        StarRating = row.GetValue<int>("star_rating"),
                        Phone = row.GetValue<string>("phone")
                    };

                    return View(hotel);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Không thể tải thông tin khách sạn: "
                    + ex.Message;

                return View(new Hotel());
            }
        }


        // =====================================================
        // CHI TIẾT
        // GET: /Hotel/Details
        // =====================================================

        public ActionResult Details()
        {
            return RedirectToAction("Index");
        }


        // =====================================================
        // CREATE - KHÔNG CHO TẠO KHÁCH SẠN THỨ 2
        // =====================================================

        public ActionResult Create()
        {
            return RedirectToAction("Index");
        }


        // =====================================================
        // EDIT - GET
        // =====================================================

        [HttpGet]
        public ActionResult Edit()
        {
            try
            {
                using (var db = new CassandraService())
                {
                    var row = db.GetHotel(HOTEL_ID);

                    if (row == null)
                    {
                        return HttpNotFound();
                    }

                    var model = new Hotel
                    {
                        HotelId = row.GetValue<string>("hotel_id"),
                        HotelName = row.GetValue<string>("hotel_name"),
                        Address = row.GetValue<string>("address"),
                        City = row.GetValue<string>("city"),
                        StarRating = row.GetValue<int>("star_rating"),
                        Phone = row.GetValue<string>("phone")
                    };

                    return View(model);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Không thể tải thông tin chỉnh sửa: "
                    + ex.Message;

                return RedirectToAction("Index");
            }
        }


        // =====================================================
        // EDIT - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Hotel model)
        {
            // Không cho thay đổi HotelId
            model.HotelId = HOTEL_ID;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                using (var db = new CassandraService())
                {
                    db.UpdateHotel(
                        HOTEL_ID,
                        model.HotelName,
                        model.Address,
                        model.City,
                        model.StarRating,
                        model.Phone);
                }

                TempData["Success"] =
                    "Cập nhật thông tin khách sạn thành công.";

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    "Không thể cập nhật: " + ex.Message);

                return View(model);
            }
        }


        // =====================================================
        // DELETE - KHÔNG CHO XÓA KHÁCH SẠN
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete()
        {
            TempData["Error"] =
                "Hệ thống chỉ quản lý một khách sạn. " +
                "Không thể xóa khách sạn.";

            return RedirectToAction("Index");
        }
    }
}