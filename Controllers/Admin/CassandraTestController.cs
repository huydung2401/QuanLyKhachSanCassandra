using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Cassandra;
using QuanLyKhachSan.Services;

namespace QuanLyKhachSan.Controllers
{
    public class CassandraTestController : Controller
    {
        public ActionResult Index()
        {
            try
            {
                using (var cassandra = new CassandraService())
                {
                    bool connected = cassandra.TestConnection();

                    ViewBag.Connected = connected;
                    ViewBag.Version = cassandra.GetDatabaseVersion();

                    var hotels = cassandra
                        .GetHotels()
                        .Select(row => new HotelTestViewModel
                        {
                            HotelId = row.GetValue<string>("hotel_id"),
                            HotelName = row.GetValue<string>("hotel_name"),
                            Address = row.GetValue<string>("address"),
                            City = row.GetValue<string>("city"),
                            StarRating = row.GetValue<int>("star_rating"),
                            Phone = row.GetValue<string>("phone")
                        })
                        .ToList();

                    return View(hotels);
                }
            }
            catch (Exception ex)
            {
                ViewBag.Connected = false;
                ViewBag.Error = ex.ToString();

                return View(new List<HotelTestViewModel>());
            }
        }
    }

    public class HotelTestViewModel
    {
        public string HotelId { get; set; }

        public string HotelName { get; set; }

        public string Address { get; set; }

        public string City { get; set; }

        public int StarRating { get; set; }

        public string Phone { get; set; }
    }
}