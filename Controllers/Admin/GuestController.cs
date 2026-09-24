using Cassandra;
using System;
using System.Linq;
using System.Web.Mvc;
using QuanLyKhachSan.Models;
using QuanLyKhachSan.Services;

namespace QuanLyKhachSan.Controllers.Admin
{
    public class GuestController : Controller
    {
        // =====================================================
        // INDEX
        // =====================================================

        public ActionResult Index()
        {
            try
            {
                using (var cassandra = new CassandraService())
                {
                    var rows =
                        cassandra.GetGuests();

                    var guests =
                        rows.Select(row => new Guest
                        {
                            GuestId =
                                row.GetValue<string>(
                                    "guest_id"),

                            FullName =
                                row.GetValue<string>(
                                    "full_name"),

                            Phone =
                                row.GetValue<string>(
                                    "phone"),

                            Email =
                                row.IsNull("email")
                                    ? null
                                    : row.GetValue<string>(
                                        "email"),

                            NationalId =
                                row.IsNull("national_id")
                                    ? null
                                    : row.GetValue<string>(
                                        "national_id")

                        }).ToList();

                    return View(
                        "~/Views/Admin/Guest/Index.cshtml",
                        guests
                    );
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error =
                    "Không thể tải danh sách khách hàng: "
                    + ex.Message;

                return View(
                    "~/Views/Admin/Guest/Index.cshtml",
                    new System.Collections.Generic.List<Guest>()
                );
            }
        }


        // =====================================================
        // DETAILS
        // =====================================================

        public ActionResult Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return HttpNotFound(
                    "Thiếu mã khách hàng."
                );
            }

            try
            {
                using (var cassandra =
                    new CassandraService())
                {
                    var row =
                        cassandra.GetGuest(id);

                    if (row == null)
                    {
                        return HttpNotFound(
                            "Không tìm thấy khách hàng."
                        );
                    }

                    var guest =
                        MapGuest(row);

                    return View(
                        "~/Views/Admin/Guest/Details.cshtml",
                        guest
                    );
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error =
                    "Không thể tải thông tin khách hàng: "
                    + ex.Message;

                return View(
                    "~/Views/Admin/Guest/Details.cshtml"
                );
            }
        }


        // =====================================================
        // CREATE - GET
        // =====================================================

        [HttpGet]
        public ActionResult Create()
        {
            return View(
                "~/Views/Admin/Guest/Create.cshtml"
            );
        }


        // =====================================================
        // CREATE - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Guest model)
        {
            if (!ModelState.IsValid)
            {
                return View(
                    "~/Views/Admin/Guest/Create.cshtml",
                    model
                );
            }

            try
            {
                using (var cassandra =
                    new CassandraService())
                {
                    // Kiểm tra khách hàng đã tồn tại
                    var existing =
                        cassandra.GetGuest(
                            model.GuestId
                        );

                    if (existing != null)
                    {
                        ModelState.AddModelError(
                            "GuestId",
                            "Mã khách hàng đã tồn tại."
                        );

                        return View(
                            "~/Views/Admin/Guest/Create.cshtml",
                            model
                        );
                    }

                    cassandra.CreateGuest(
                        model.GuestId,
                        model.FullName,
                        model.Phone,
                        model.Email,
                        model.NationalId
                    );
                }

                TempData["Success"] =
                    "Thêm khách hàng thành công.";

                return RedirectToAction(
                    "Index"
                );
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    "Không thể thêm khách hàng: "
                    + ex.Message
                );

                return View(
                    "~/Views/Admin/Guest/Create.cshtml",
                    model
                );
            }
        }


        // =====================================================
        // EDIT - GET
        // =====================================================

        [HttpGet]
        public ActionResult Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return HttpNotFound(
                    "Thiếu mã khách hàng."
                );
            }

            try
            {
                using (var cassandra =
                    new CassandraService())
                {
                    var row =
                        cassandra.GetGuest(id);

                    if (row == null)
                    {
                        return HttpNotFound(
                            "Không tìm thấy khách hàng."
                        );
                    }

                    var guest =
                        MapGuest(row);

                    return View(
                        "~/Views/Admin/Guest/Edit.cshtml",
                        guest
                    );
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Không thể tải khách hàng: "
                    + ex.Message;

                return RedirectToAction(
                    "Index"
                );
            }
        }


        // =====================================================
        // EDIT - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Guest model)
        {
            if (!ModelState.IsValid)
            {
                return View(
                    "~/Views/Admin/Guest/Edit.cshtml",
                    model
                );
            }

            try
            {
                using (var cassandra =
                    new CassandraService())
                {
                    var existing =
                        cassandra.GetGuest(
                            model.GuestId
                        );

                    if (existing == null)
                    {
                        return HttpNotFound(
                            "Không tìm thấy khách hàng."
                        );
                    }

                    cassandra.UpdateGuest(
                        model.GuestId,
                        model.FullName,
                        model.Phone,
                        model.Email,
                        model.NationalId
                    );
                }

                TempData["Success"] =
                    "Cập nhật khách hàng thành công.";

                return RedirectToAction(
                    "Index"
                );
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    "Không thể cập nhật khách hàng: "
                    + ex.Message
                );

                return View(
                    "~/Views/Admin/Guest/Edit.cshtml",
                    model
                );
            }
        }


        // =====================================================
        // DELETE - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["Error"] =
                    "Thiếu mã khách hàng.";

                return RedirectToAction(
                    "Index"
                );
            }

            try
            {
                using (var cassandra =
                    new CassandraService())
                {
                    var existing =
                        cassandra.GetGuest(id);

                    if (existing == null)
                    {
                        TempData["Error"] =
                            "Không tìm thấy khách hàng.";

                        return RedirectToAction(
                            "Index"
                        );
                    }

                    cassandra.DeleteGuest(id);
                }

                TempData["Success"] =
                    "Xóa khách hàng thành công.";
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Không thể xóa khách hàng: "
                    + ex.Message;
            }

            return RedirectToAction(
                "Index"
            );
        }


        // =====================================================
        // MAP ROW -> GUEST
        // =====================================================

        private Guest MapGuest(Row row)
        {
            return new Guest
            {
                GuestId =
                    row.GetValue<string>(
                        "guest_id"),

                FullName =
                    row.GetValue<string>(
                        "full_name"),

                Phone =
                    row.GetValue<string>(
                        "phone"),

                Email =
                    row.IsNull("email")
                        ? null
                        : row.GetValue<string>(
                            "email"),

                NationalId =
                    row.IsNull("national_id")
                        ? null
                        : row.GetValue<string>(
                            "national_id")
            };
        }
    }
}