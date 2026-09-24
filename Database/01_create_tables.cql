USE hotel_ks;

CREATE TABLE IF NOT EXISTS hotels (
    hotel_id TEXT,
    hotel_name TEXT,
    city TEXT,
    address TEXT,
    star_rating INT,
    phone TEXT,
    PRIMARY KEY (hotel_id)
);

CREATE TABLE IF NOT EXISTS guests (
    guest_id TEXT,
    full_name TEXT,
    phone TEXT,
    email TEXT,
    national_id TEXT,
    PRIMARY KEY (guest_id)
);

CREATE TABLE IF NOT EXISTS rooms_by_hotel (
    hotel_id TEXT,
    room_number INT,
    room_type TEXT,
    price_per_night DECIMAL,
    status TEXT,
    PRIMARY KEY (hotel_id, room_number)
);

CREATE TABLE IF NOT EXISTS rooms_by_hotel_status (
    hotel_id TEXT,
    status TEXT,
    room_number INT,
    room_type TEXT,
    price_per_night DECIMAL,
    PRIMARY KEY ((hotel_id, status), room_number)
);

CREATE TABLE IF NOT EXISTS bookings_by_guest (
    guest_id TEXT,
    check_in_date DATE,
    booking_id UUID,
    hotel_id TEXT,
    hotel_name TEXT,
    room_number INT,
    check_out_date DATE,
    total_amount DECIMAL,
    status TEXT,
    PRIMARY KEY (guest_id, check_in_date, booking_id)
) WITH CLUSTERING ORDER BY (
    check_in_date DESC,
    booking_id DESC
);

CREATE TABLE IF NOT EXISTS bookings_by_hotel_date (
    hotel_id TEXT,
    check_in_date DATE,
    booking_id UUID,
    guest_id TEXT,
    guest_name TEXT,
    room_number INT,
    check_out_date DATE,
    total_amount DECIMAL,
    status TEXT,
    PRIMARY KEY ((hotel_id), check_in_date, booking_id)
) WITH CLUSTERING ORDER BY (
    check_in_date ASC,
    booking_id ASC
);

CREATE TABLE IF NOT EXISTS invoices_by_booking (
    booking_id UUID,
    invoice_id UUID,
    guest_id TEXT,
    hotel_id TEXT,
    room_charge DECIMAL,
    service_charge DECIMAL,
    tax DECIMAL,
    total_amount DECIMAL,
    payment_status TEXT,
    issued_at TIMESTAMP,
    PRIMARY KEY (booking_id, invoice_id)
);

CREATE TABLE IF NOT EXISTS revenue_by_hotel_month (
    hotel_id TEXT,
    year_month TEXT,
    revenue_date DATE,
    revenue_amount DECIMAL,
    PRIMARY KEY ((hotel_id, year_month), revenue_date)
);