using Cassandra;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;

namespace QuanLyKhachSan.Services
{
    /// <summary>
    /// Service dùng để giao tiếp với Astra DB thông qua Cassandra C# Driver.
    ///
    /// Tối ưu:
    /// - Cluster chỉ tạo 1 lần cho toàn bộ Application Domain.
    /// - Session chỉ tạo 1 lần.
    /// - Các Controller vẫn có thể dùng:
    ///       using (var db = new CassandraService())
    ///   nhưng Dispose() không đóng connection dùng chung.
    /// - Các truy vấn được giữ tương thích với schema hiện tại.
    /// </summary>
    public class CassandraService : IDisposable
    {
        // =========================================================
        // ASTRA DATABASE
        // =========================================================

        private const string DEFAULT_HOTEL_ID = "HOTEL001";


        // =========================================================
        // CONNECTION DÙNG CHUNG
        // =========================================================

        private static readonly object _connectionLock =
            new object();

        private static ICluster _cluster;

        private static ISession _session;


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public CassandraService()
        {
            EnsureConnection();
        }


        // =========================================================
        // KHỞI TẠO CONNECTION
        // =========================================================

        private static void EnsureConnection()
        {
            // -----------------------------------------------------
            // Nếu đã có Session thì sử dụng lại.
            // Không tạo connection mới.
            // -----------------------------------------------------

            if (_session != null)
            {
                return;
            }


            lock (_connectionLock)
            {
                // -------------------------------------------------
                // Double-check sau khi lấy lock.
                // -------------------------------------------------

                if (_session != null)
                {
                    return;
                }


                // =================================================
                // 1. ĐỌC CONFIG
                // =================================================

                string token =
                    ConfigurationManager.AppSettings[
                        "AstraApplicationToken"];


                string keyspace =
                    ConfigurationManager.AppSettings[
                        "AstraKeyspace"];


                string bundle =
                    ConfigurationManager.AppSettings[
                        "AstraSecureConnectBundle"];


                // =================================================
                // 2. KIỂM TRA TOKEN
                // =================================================

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new Exception(
                        "AstraApplicationToken đang rỗng. " +
                        "Hãy kiểm tra Web.config.");
                }


                token = token.Trim();


                if (!token.StartsWith(
                    "AstraCS:",
                    StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception(
                        "AstraApplicationToken không đúng định dạng. " +
                        "Application Token phải bắt đầu bằng AstraCS:.");
                }


                // =================================================
                // 3. KIỂM TRA KEYSPACE
                // =================================================

                if (string.IsNullOrWhiteSpace(keyspace))
                {
                    throw new Exception(
                        "AstraKeyspace đang rỗng. " +
                        "Hãy kiểm tra Web.config.");
                }


                keyspace = keyspace.Trim();


                // =================================================
                // 4. KIỂM TRA SECURE CONNECT BUNDLE
                // =================================================

                if (string.IsNullOrWhiteSpace(bundle))
                {
                    throw new Exception(
                        "AstraSecureConnectBundle đang rỗng. " +
                        "Hãy kiểm tra Web.config.");
                }


                bundle = bundle.Trim();


                // =================================================
                // 5. XÁC ĐỊNH ĐƯỜNG DẪN SCB
                // =================================================

                string bundlePath;


                if (Path.IsPathRooted(bundle))
                {
                    bundlePath = bundle;
                }
                else
                {
                    bundlePath =
                        Path.Combine(
                            AppDomain.CurrentDomain.BaseDirectory,
                            bundle);
                }


                bundlePath =
                    Path.GetFullPath(bundlePath);


                // =================================================
                // 6. KIỂM TRA FILE SCB
                // =================================================

                if (!File.Exists(bundlePath))
                {
                    throw new FileNotFoundException(
                        "Không tìm thấy Secure Connect Bundle của Astra DB.",
                        bundlePath);
                }


                // =================================================
                // 7. AUTHENTICATION
                // =================================================

                var authProvider =
                    new PlainTextAuthProvider(
                        "token",
                        token);


                // =================================================
                // 8. TẠO CLUSTER
                // =================================================

                ICluster newCluster = null;

                ISession newSession = null;


                try
                {
                    newCluster =
                        Cluster.Builder()
                            .WithCloudSecureConnectionBundle(
                                bundlePath)
                            .WithAuthProvider(
                                authProvider)
                            .Build();


                    // =================================================
                    // 9. TẠO SESSION
                    // =================================================

                    newSession =
                        newCluster.Connect(keyspace);


                    // =================================================
                    // 10. CHỈ GÁN VÀO STATIC SAU KHI KẾT NỐI
                    // THÀNH CÔNG
                    // =================================================

                    _cluster = newCluster;

                    _session = newSession;


                    // -------------------------------------------------
                    // Không Dispose ở đây.
                    // Connection sẽ được dùng chung.
                    // -------------------------------------------------
                }
                catch
                {
                    // -------------------------------------------------
                    // Nếu connection thất bại,
                    // giải phóng tài nguyên vừa tạo.
                    // -------------------------------------------------

                    if (newSession != null)
                    {
                        newSession.Dispose();
                    }


                    if (newCluster != null)
                    {
                        newCluster.Dispose();
                    }


                    throw;
                }
            }
        }


        // =========================================================
        // TEST CONNECTION
        // =========================================================

        public bool TestConnection()
        {
            try
            {
                EnsureConnection();


                _session.Execute(
                    "SELECT release_version FROM system.local");


                return true;
            }
            catch
            {
                return false;
            }
        }


        // =========================================================
        // DATABASE VERSION
        // =========================================================

        public string GetDatabaseVersion()
        {
            EnsureConnection();


            Row row =
                _session
                    .Execute(
                        "SELECT release_version FROM system.local")
                    .FirstOrDefault();


            if (row == null)
            {
                return "Unknown";
            }


            return row.GetValue<string>(
                "release_version");
        }


        // =========================================================
        // HOTEL
        // =========================================================


        // =========================================================
        // HOTEL - GET ALL
        // =========================================================

        public RowSet GetHotels()
        {
            EnsureConnection();


            string cql = @"
                SELECT
                    hotel_id,
                    hotel_name,
                    address,
                    city,
                    star_rating,
                    phone
                FROM hotels;
            ";


            return _session.Execute(
                new SimpleStatement(cql));
        }


        // =========================================================
        // HOTEL - GET BY ID
        // =========================================================

        public Row GetHotel(string hotelId)
        {
            EnsureConnection();


            if (string.IsNullOrWhiteSpace(hotelId))
            {
                return null;
            }


            string cql = @"
                SELECT
                    hotel_id,
                    hotel_name,
                    address,
                    city,
                    star_rating,
                    phone
                FROM hotels
                WHERE hotel_id = ?;
            ";


            var statement =
                new SimpleStatement(
                    cql,
                    hotelId);


            return _session
                .Execute(statement)
                .FirstOrDefault();
        }


        // =========================================================
        // HOTEL - CREATE
        // =========================================================

        public void CreateHotel(
            string hotelId,
            string hotelName,
            string address,
            string city,
            int starRating,
            string phone)
        {
            EnsureConnection();


            string cql = @"
                INSERT INTO hotels
                (
                    hotel_id,
                    hotel_name,
                    address,
                    city,
                    star_rating,
                    phone
                )
                VALUES (?, ?, ?, ?, ?, ?);
            ";


            var statement =
                new SimpleStatement(
                    cql,
                    hotelId,
                    hotelName,
                    address,
                    city,
                    starRating,
                    phone);


            _session.Execute(statement);
        }


        // =========================================================
        // HOTEL - UPDATE
        // =========================================================

        public void UpdateHotel(
            string hotelId,
            string hotelName,
            string address,
            string city,
            int starRating,
            string phone)
        {
            EnsureConnection();


            string cql = @"
                UPDATE hotels
                SET
                    hotel_name = ?,
                    address = ?,
                    city = ?,
                    star_rating = ?,
                    phone = ?
                WHERE hotel_id = ?;
            ";


            var statement =
                new SimpleStatement(
                    cql,
                    hotelName,
                    address,
                    city,
                    starRating,
                    phone,
                    hotelId);


            _session.Execute(statement);
        }


        // =========================================================
        // HOTEL - DELETE
        // =========================================================

        public void DeleteHotel(string hotelId)
        {
            EnsureConnection();


            if (string.IsNullOrWhiteSpace(hotelId))
            {
                return;
            }


            string cql = @"
                DELETE FROM hotels
                WHERE hotel_id = ?;
            ";


            var statement =
                new SimpleStatement(
                    cql,
                    hotelId);


            _session.Execute(statement);
        }


        // =========================================================
        // ROOM
        // =========================================================


        // =========================================================
        // ROOM - GET ALL BY HOTEL
        // =========================================================

        public RowSet GetRoomsByHotel(
            string hotelId)
        {
            EnsureConnection();


            if (string.IsNullOrWhiteSpace(hotelId))
            {
                return null;
            }


            string cql = @"
                SELECT
                    hotel_id,
                    room_number,
                    room_type,
                    price_per_night,
                    status,
                    image_url
                FROM rooms_by_hotel
                WHERE hotel_id = ?;
            ";


            var statement =
                new SimpleStatement(
                    cql,
                    hotelId);


            return _session.Execute(statement);
        }


        // =========================================================
        // ROOM - GET BY HOTEL + ROOM NUMBER
        // =========================================================

        public Row GetRoom(
            string hotelId,
            int roomNumber)
        {
            EnsureConnection();


            if (string.IsNullOrWhiteSpace(hotelId))
            {
                return null;
            }


            string cql = @"
                SELECT
                    hotel_id,
                    room_number,
                    room_type,
                    price_per_night,
                    status,
                    image_url
                FROM rooms_by_hotel
                WHERE hotel_id = ?
                  AND room_number = ?;
            ";


            var statement =
                new SimpleStatement(
                    cql,
                    hotelId,
                    roomNumber);


            return _session
                .Execute(statement)
                .FirstOrDefault();
        }


        // =========================================================
        // ROOM - CREATE
        // =========================================================

        public void CreateRoom(
            string hotelId,
            int roomNumber,
            string roomType,
            decimal pricePerNight,
            string status,
            string imageUrl)
        {
            EnsureConnection();


            string cql = @"
                INSERT INTO rooms_by_hotel
                (
                    hotel_id,
                    room_number,
                    room_type,
                    price_per_night,
                    status,
                    image_url
                )
                VALUES (?, ?, ?, ?, ?, ?);
            ";


            var statement =
                new SimpleStatement(
                    cql,
                    hotelId,
                    roomNumber,
                    roomType,
                    pricePerNight,
                    status,
                    imageUrl);


            _session.Execute(statement);
        }


        // =========================================================
        // ROOM - UPDATE
        // =========================================================

        public void UpdateRoom(
            string hotelId,
            int roomNumber,
            string roomType,
            decimal pricePerNight,
            string status,
            string imageUrl)
        {
            EnsureConnection();


            string cql = @"
                UPDATE rooms_by_hotel
                SET
                    room_type = ?,
                    price_per_night = ?,
                    status = ?,
                    image_url = ?
                WHERE hotel_id = ?
                  AND room_number = ?;
            ";


            var statement =
                new SimpleStatement(
                    cql,
                    roomType,
                    pricePerNight,
                    status,
                    imageUrl,
                    hotelId,
                    roomNumber);


            _session.Execute(statement);
        }


        // =========================================================
        // ROOM - DELETE
        // =========================================================

        public void DeleteRoom(
            string hotelId,
            int roomNumber)
        {
            EnsureConnection();


            if (string.IsNullOrWhiteSpace(hotelId))
            {
                return;
            }


            string cql = @"
                DELETE FROM rooms_by_hotel
                WHERE hotel_id = ?
                  AND room_number = ?;
            ";


            var statement =
                new SimpleStatement(
                    cql,
                    hotelId,
                    roomNumber);


            _session.Execute(statement);
        }


        // =========================================================
        // GUEST
        // =========================================================


        // =========================================================
        // GUEST - GET ALL
        // =========================================================

        public IEnumerable<Row> GetGuests()
        {
            EnsureConnection();


            string cql = @"
                SELECT
                    guest_id,
                    full_name,
                    phone,
                    email,
                    national_id
                FROM guests;
            ";


            return _session.Execute(
                new SimpleStatement(cql));
        }


        // =========================================================
        // GUEST - GET BY ID
        // =========================================================

        public Row GetGuest(
            string guestId)
        {
            EnsureConnection();


            if (string.IsNullOrWhiteSpace(guestId))
            {
                return null;
            }


            string cql = @"
                SELECT
                    guest_id,
                    full_name,
                    phone,
                    email,
                    national_id
                FROM guests
                WHERE guest_id = ?;
            ";


            var statement =
                new SimpleStatement(
                    cql,
                    guestId);


            return _session
                .Execute(statement)
                .FirstOrDefault();
        }


        // =========================================================
        // GUEST - CREATE
        // =========================================================

        public void CreateGuest(
            string guestId,
            string fullName,
            string phone,
            string email,
            string nationalId)
        {
            EnsureConnection();


            string cql = @"
                INSERT INTO guests
                (
                    guest_id,
                    full_name,
                    phone,
                    email,
                    national_id
                )
                VALUES (?, ?, ?, ?, ?);
            ";


            var statement =
                new SimpleStatement(
                    cql,
                    guestId,
                    fullName,
                    phone,
                    email,
                    nationalId);


            _session.Execute(statement);
        }


        // =========================================================
        // GUEST - UPDATE
        // =========================================================

        public void UpdateGuest(
            string guestId,
            string fullName,
            string phone,
            string email,
            string nationalId)
        {
            EnsureConnection();


            string cql = @"
                UPDATE guests
                SET
                    full_name = ?,
                    phone = ?,
                    email = ?,
                    national_id = ?
                WHERE guest_id = ?;
            ";


            var statement =
                new SimpleStatement(
                    cql,
                    fullName,
                    phone,
                    email,
                    nationalId,
                    guestId);


            _session.Execute(statement);
        }


        // =========================================================
        // GUEST - DELETE
        // =========================================================

        public void DeleteGuest(
            string guestId)
        {
            EnsureConnection();


            if (string.IsNullOrWhiteSpace(guestId))
            {
                return;
            }


            string cql = @"
                DELETE FROM guests
                WHERE guest_id = ?;
            ";


            var statement =
                new SimpleStatement(
                    cql,
                    guestId);


            _session.Execute(statement);
        }


        // =========================================================
        // BOOKING
        // =========================================================


        // =========================================================
        // BOOKING - COUNT
        //
        // Hệ thống chỉ quản lý một khách sạn:
        // HOTEL001
        // =========================================================

        public int GetBookingCount()
        {
            return GetBookingCount(
                DEFAULT_HOTEL_ID);
        }


        // =========================================================
        // BOOKING - COUNT BY HOTEL
        // =========================================================

        public int GetBookingCount(
            string hotelId)
        {
            EnsureConnection();


            if (string.IsNullOrWhiteSpace(hotelId))
            {
                return 0;
            }


            string cql = @"
                SELECT COUNT(*) AS booking_count
                FROM bookings_by_hotel_date
                WHERE hotel_id = ?;
            ";


            var statement =
                new SimpleStatement(
                    cql,
                    hotelId);


            var row =
                _session
                    .Execute(statement)
                    .FirstOrDefault();


            if (row == null)
            {
                return 0;
            }


            if (row.IsNull("booking_count"))
            {
                return 0;
            }


            long count =
                row.GetValue<long>(
                    "booking_count");


            if (count > int.MaxValue)
            {
                return int.MaxValue;
            }


            return (int)count;
        }
        // =========================================================
        // BOOKING - REVENUE
        // =========================================================

        public decimal GetRevenue(string hotelId)
        {
            EnsureConnection();

            if (string.IsNullOrWhiteSpace(hotelId))
            {
                return 0m;
            }

            string cql = @"
        SELECT total_amount, status
        FROM bookings_by_hotel_date
        WHERE hotel_id = ?;
    ";

            var statement =
                new SimpleStatement(
                    cql,
                    hotelId
                );

            decimal totalRevenue = 0m;

            foreach (var row in _session.Execute(statement))
            {
                if (row == null)
                {
                    continue;
                }

                if (row.IsNull("total_amount"))
                {
                    continue;
                }

                string status = "";

                if (!row.IsNull("status"))
                {
                    status =
                        row.GetValue<string>("status");
                }

                // Không tính booking đã hủy
                if (string.Equals(
                        status,
                        "CANCELLED",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                decimal amount =
                    row.GetValue<decimal>("total_amount");

                totalRevenue += amount;
            }

            return totalRevenue;
        }

        public RowSet Execute(IStatement statement)
        {
            EnsureConnection();
            return _session.Execute(statement);
        }

        public void ExecuteBatch(BatchStatement batch)
        {
            EnsureConnection();
            _session.Execute(batch);
        }

        // =========================================================
        // DISPOSE
        //
        // QUAN TRỌNG:
        //
        // CassandraService được tạo bằng using ở Controller,
        // nhưng Cluster + Session là connection dùng chung.
        //
        // Vì vậy KHÔNG Dispose tại đây.
        // =========================================================

        public void Dispose()
        {
            // Intentionally empty.
            //
            // _cluster và _session được dùng chung
            // trong toàn bộ Application Domain.
        }
    }
}