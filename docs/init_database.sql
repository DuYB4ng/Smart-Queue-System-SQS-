-- ============================================================
--  SMART QUEUE SYSTEM (SQS) — Database Initialization Script
--  Database : MySQL 8.0+
--  Encoding : UTF-8
--  Created  : 2026-07-05
-- ============================================================

CREATE DATABASE IF NOT EXISTS sqs_db
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE sqs_db;

-- ============================================================
-- 0. CLEANUP (for re-run)
-- ============================================================
SET FOREIGN_KEY_CHECKS = 0;

DROP TABLE IF EXISTS daily_sequence;
DROP TABLE IF EXISTS tickets;
DROP TABLE IF EXISTS counter_services;
DROP TABLE IF EXISTS counters;
DROP TABLE IF EXISTS services;
DROP TABLE IF EXISTS admins;
DROP TABLE IF EXISTS staffs;
DROP TABLE IF EXISTS customers;
DROP TABLE IF EXISTS users;

SET FOREIGN_KEY_CHECKS = 1;

-- ============================================================
-- 1. users — Bảng gốc cho toàn bộ người dùng
-- ============================================================
CREATE TABLE users (
    id           INT UNSIGNED    NOT NULL AUTO_INCREMENT,
    name         NVARCHAR(100)   NOT NULL,
    email        VARCHAR(150)    NOT NULL,
    password_hash VARCHAR(255)   NOT NULL,          -- BCrypt hash
    birthday     DATE            NULL,
    address      NVARCHAR(255)   NULL,
    role         ENUM('Customer','Staff','Admin')
                                 NOT NULL DEFAULT 'Customer',
    created_at   DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at   DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP
                                 ON UPDATE CURRENT_TIMESTAMP,
    is_active    TINYINT(1)      NOT NULL DEFAULT 1,

    PRIMARY KEY (id),
    UNIQUE KEY uq_users_email (email)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Bảng gốc chứa thông tin tất cả người dùng';

-- ============================================================
-- 2. customers — Liên kết 1-1 với users
-- ============================================================
CREATE TABLE customers (
    user_id      INT UNSIGNED    NOT NULL,

    PRIMARY KEY (user_id),
    CONSTRAINT fk_customers_user
        FOREIGN KEY (user_id) REFERENCES users(id)
        ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Mở rộng users cho role Customer';

-- ============================================================
-- 3. staffs — Liên kết 1-1 với users, thêm position + kpi
-- ============================================================
CREATE TABLE staffs (
    user_id      INT UNSIGNED    NOT NULL,
    position     NVARCHAR(100)   NOT NULL DEFAULT N'Nhân viên',
    kpi          INT UNSIGNED    NOT NULL DEFAULT 0  COMMENT 'Tổng phiên đã hoàn thành',

    PRIMARY KEY (user_id),
    CONSTRAINT fk_staffs_user
        FOREIGN KEY (user_id) REFERENCES users(id)
        ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Mở rộng users cho role Staff';

-- ============================================================
-- 4. admins — Liên kết 1-1 với users
-- ============================================================
CREATE TABLE admins (
    user_id      INT UNSIGNED    NOT NULL,

    PRIMARY KEY (user_id),
    CONSTRAINT fk_admins_user
        FOREIGN KEY (user_id) REFERENCES users(id)
        ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Mở rộng users cho role Admin';

-- ============================================================
-- 5. services — Danh sách loại dịch vụ
-- ============================================================
CREATE TABLE services (
    id           INT UNSIGNED    NOT NULL AUTO_INCREMENT,
    name         NVARCHAR(150)   NOT NULL,
    code         VARCHAR(5)      NOT NULL            COMMENT 'Mã viết tắt VD: DK, HP, HS',
    description  NVARCHAR(500)   NULL,
    is_active    TINYINT(1)      NOT NULL DEFAULT 1,
    created_at   DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (id),
    UNIQUE KEY uq_services_code (code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Danh mục các loại dịch vụ tại quầy';

-- ============================================================
-- 6. counters — Các quầy phục vụ
-- ============================================================
CREATE TABLE counters (
    id           INT UNSIGNED    NOT NULL AUTO_INCREMENT,
    name         NVARCHAR(100)   NOT NULL            COMMENT 'VD: Quầy 1, Quầy A',
    location     NVARCHAR(200)   NULL                COMMENT 'VD: Tầng 1, Phòng 101',
    is_active    TINYINT(1)      NOT NULL DEFAULT 1,
    created_at   DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Các quầy phục vụ';

-- ============================================================
-- 7. counter_services — Phân công dịch vụ cho quầy (N-N)
-- ============================================================
CREATE TABLE counter_services (
    counter_id   INT UNSIGNED    NOT NULL,
    service_id   INT UNSIGNED    NOT NULL,

    PRIMARY KEY (counter_id, service_id),
    CONSTRAINT fk_cs_counter
        FOREIGN KEY (counter_id) REFERENCES counters(id)
        ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT fk_cs_service
        FOREIGN KEY (service_id) REFERENCES services(id)
        ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Quầy nào phục vụ dịch vụ nào';

-- ============================================================
-- 8. daily_sequence — Bộ đếm số thứ tự theo ngày
-- ============================================================
CREATE TABLE daily_sequence (
    id           INT UNSIGNED    NOT NULL AUTO_INCREMENT,
    seq_date     DATE            NOT NULL            COMMENT 'Ngày của chuỗi số',
    last_number  SMALLINT UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Số cuối cùng đã cấp (0-999)',

    PRIMARY KEY (id),
    UNIQUE KEY uq_daily_seq_date (seq_date)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Bộ đếm số thứ tự theo ngày, reset mỗi ngày';

-- ============================================================
-- 9. tickets — Phiên xếp hàng (bảng nghiệp vụ chính)
-- ============================================================
CREATE TABLE tickets (
    id              INT UNSIGNED    NOT NULL AUTO_INCREMENT,

    -- Số thứ tự dạng "001", "002", ... "999"
    ticket_number   CHAR(3)         NOT NULL            COMMENT 'Số thứ tự trong ngày, VD: 007',

    -- Người lấy số (nullable: khách vãng lai không đăng nhập)
    id_customer     INT UNSIGNED    NULL,
    guest_name      NVARCHAR(100)   NULL                COMMENT 'Tên khách vãng lai (Kiosk)',

    -- Dịch vụ và quầy
    id_service      INT UNSIGNED    NOT NULL,
    id_counter      INT UNSIGNED    NULL                COMMENT 'Gán quầy khi Staff gọi số',
    id_staff        INT UNSIGNED    NULL                COMMENT 'Staff xử lý',

    -- Thời gian
    ticket_date     DATE            NOT NULL            COMMENT 'Ngày tạo phiên',
    created_at      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    called_at       DATETIME        NULL                COMMENT 'Thời điểm gọi số',
    completed_at    DATETIME        NULL                COMMENT 'Thời điểm hoàn thành',

    -- Trạng thái
    status          ENUM('Waiting','Calling','Completed','Canceled')
                                    NOT NULL DEFAULT 'Waiting',

    PRIMARY KEY (id),

    -- Ràng buộc: guest_name bắt buộc nếu không có id_customer
    CONSTRAINT chk_customer_or_guest
        CHECK (id_customer IS NOT NULL OR guest_name IS NOT NULL),

    -- Foreign keys
    CONSTRAINT fk_tickets_customer
        FOREIGN KEY (id_customer) REFERENCES customers(user_id)
        ON DELETE SET NULL ON UPDATE CASCADE,
    CONSTRAINT fk_tickets_service
        FOREIGN KEY (id_service) REFERENCES services(id)
        ON UPDATE CASCADE,
    CONSTRAINT fk_tickets_counter
        FOREIGN KEY (id_counter) REFERENCES counters(id)
        ON UPDATE CASCADE,
    CONSTRAINT fk_tickets_staff
        FOREIGN KEY (id_staff) REFERENCES staffs(user_id)
        ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Phiên xếp hàng chính — mỗi row là 1 lượt lấy số';

-- ============================================================
-- 10. INDEXES — Tối ưu truy vấn
-- ============================================================

-- Staff gọi số: tìm Waiting cũ nhất theo service
CREATE INDEX idx_tickets_service_status_created
    ON tickets (id_service, status, created_at);

-- Khách theo dõi trạng thái của mình theo ngày
CREATE INDEX idx_tickets_customer_date
    ON tickets (id_customer, ticket_date);

-- Dashboard: thống kê theo ngày
CREATE INDEX idx_tickets_date_status
    ON tickets (ticket_date, status);

-- Màn hình tổng: lấy ticket đang Calling
CREATE INDEX idx_tickets_status_counter
    ON tickets (status, id_counter);

-- ============================================================
-- 11. STORED PROCEDURE — Lấy số thứ tự tiếp theo (thread-safe)
-- ============================================================
DELIMITER $$

CREATE PROCEDURE sp_get_next_ticket_number(
    IN  p_date  DATE,
    OUT p_number SMALLINT UNSIGNED
)
BEGIN
    DECLARE v_number SMALLINT UNSIGNED DEFAULT 0;

    -- Upsert: nếu chưa có bản ghi cho ngày này thì tạo mới
    INSERT INTO daily_sequence (seq_date, last_number)
    VALUES (p_date, 0)
    ON DUPLICATE KEY UPDATE last_number = last_number;  -- không thay đổi, chỉ lock row

    -- Lock row và lấy số tiếp theo
    SELECT last_number INTO v_number
    FROM daily_sequence
    WHERE seq_date = p_date
    FOR UPDATE;

    SET v_number = v_number + 1;

    UPDATE daily_sequence
    SET last_number = v_number
    WHERE seq_date = p_date;

    SET p_number = v_number;
END$$

DELIMITER ;

-- ============================================================
-- 12. SEED DATA — Dữ liệu mẫu khởi tạo
-- ============================================================

-- 12.1 Dịch vụ (5 loại cho môi trường đại học)
INSERT INTO services (name, code, description) VALUES
    (N'Đăng ký học phần',   'DK', N'Đăng ký, hủy và điều chỉnh học phần'),
    (N'Nộp hồ sơ',          'HS', N'Nộp đơn xin học bổng, miễn giảm học phí, nghỉ học...'),
    (N'Thanh toán học phí', 'HP', N'Đóng học phí theo kỳ, thanh toán ký túc xá'),
    (N'Tư vấn tuyển sinh',  'TV', N'Tư vấn ngành học, điểm chuẩn, chương trình đào tạo'),
    (N'Nhận bằng & giấy tờ','BG', N'Nhận bằng tốt nghiệp, bảng điểm, giấy xác nhận sinh viên');

-- 12.2 Quầy phục vụ (5 quầy)
INSERT INTO counters (name, location) VALUES
    (N'Quầy 1 — Đăng ký học phần', N'Phòng 101, Tầng 1'),
    (N'Quầy 2 — Nộp hồ sơ',        N'Phòng 102, Tầng 1'),
    (N'Quầy 3 — Học phí',           N'Phòng 103, Tầng 1'),
    (N'Quầy 4 — Tư vấn',            N'Phòng 104, Tầng 1'),
    (N'Quầy 5 — Bằng & Giấy tờ',   N'Phòng 105, Tầng 1');

-- 12.3 Phân công: mỗi quầy phục vụ 1 dịch vụ tương ứng
INSERT INTO counter_services (counter_id, service_id) VALUES
    (1, 1),  -- Quầy 1 ← Đăng ký học phần
    (2, 2),  -- Quầy 2 ← Nộp hồ sơ
    (3, 3),  -- Quầy 3 ← Thanh toán học phí
    (4, 4),  -- Quầy 4 ← Tư vấn tuyển sinh
    (5, 5);  -- Quầy 5 ← Nhận bằng & giấy tờ

-- 12.4 Tài khoản Admin
INSERT INTO users (name, email, password_hash, role, birthday, address) VALUES
    (
        N'Quản trị viên SQS',
        'admin@sqs.edu.vn',
        -- BCrypt hash của 'Admin@123' (rounds=10) — sẽ được verify bởi ASP.NET Identity
        '$2a$10$TzP9dkd7h8XZoD3j5I7StuHnjJvYbvj7i.Fz7oFWmMVEzQXlnHOCq',
        'Admin',
        '1985-01-01',
        N'Trường Đại học'
    );
INSERT INTO admins (user_id) VALUES (LAST_INSERT_ID());

-- 12.5 Tài khoản Staff (5 nhân viên)
INSERT INTO users (name, email, password_hash, role, birthday, address) VALUES
    (N'Nguyễn Thị Lan',    'staff1@sqs.edu.vn', '$2a$10$TzP9dkd7h8XZoD3j5I7StuHnjJvYbvj7i.Fz7oFWmMVEzQXlnHOCq', 'Staff', '1995-03-10', N'TP.HCM'),
    (N'Trần Văn Minh',     'staff2@sqs.edu.vn', '$2a$10$TzP9dkd7h8XZoD3j5I7StuHnjJvYbvj7i.Fz7oFWmMVEzQXlnHOCq', 'Staff', '1993-07-22', N'TP.HCM'),
    (N'Lê Thị Hoa',        'staff3@sqs.edu.vn', '$2a$10$TzP9dkd7h8XZoD3j5I7StuHnjJvYbvj7i.Fz7oFWmMVEzQXlnHOCq', 'Staff', '1997-11-05', N'Bình Dương'),
    (N'Phạm Quốc Dũng',    'staff4@sqs.edu.vn', '$2a$10$TzP9dkd7h8XZoD3j5I7StuHnjJvYbvj7i.Fz7oFWmMVEzQXlnHOCq', 'Staff', '1994-05-18', N'Đồng Nai'),
    (N'Hoàng Thị Mai',     'staff5@sqs.edu.vn', '$2a$10$TzP9dkd7h8XZoD3j5I7StuHnjJvYbvj7i.Fz7oFWmMVEzQXlnHOCq', 'Staff', '1996-09-30', N'TP.HCM');

-- Lấy id của 5 staff vừa insert
SET @s1 = (SELECT id FROM users WHERE email = 'staff1@sqs.edu.vn');
SET @s2 = (SELECT id FROM users WHERE email = 'staff2@sqs.edu.vn');
SET @s3 = (SELECT id FROM users WHERE email = 'staff3@sqs.edu.vn');
SET @s4 = (SELECT id FROM users WHERE email = 'staff4@sqs.edu.vn');
SET @s5 = (SELECT id FROM users WHERE email = 'staff5@sqs.edu.vn');

INSERT INTO staffs (user_id, position, kpi) VALUES
    (@s1, N'Nhân viên đăng ký học phần', 0),
    (@s2, N'Nhân viên hành chính',       0),
    (@s3, N'Nhân viên tài chính',        0),
    (@s4, N'Nhân viên tư vấn',           0),
    (@s5, N'Nhân viên hành chính',       0);

-- 12.6 Tài khoản Customer mẫu (3 sinh viên)
INSERT INTO users (name, email, password_hash, role, birthday, address) VALUES
    (N'Nguyễn Văn An',  'sv001@sqs.edu.vn', '$2a$10$TzP9dkd7h8XZoD3j5I7StuHnjJvYbvj7i.Fz7oFWmMVEzQXlnHOCq', 'Customer', '2003-02-14', N'TP.HCM'),
    (N'Trần Thị Bích',  'sv002@sqs.edu.vn', '$2a$10$TzP9dkd7h8XZoD3j5I7StuHnjJvYbvj7i.Fz7oFWmMVEzQXlnHOCq', 'Customer', '2004-06-20', N'Hà Nội'),
    (N'Lê Minh Cường',  'sv003@sqs.edu.vn', '$2a$10$TzP9dkd7h8XZoD3j5I7StuHnjJvYbvj7i.Fz7oFWmMVEzQXlnHOCq', 'Customer', '2002-12-01', N'Đà Nẵng');

SET @c1 = (SELECT id FROM users WHERE email = 'sv001@sqs.edu.vn');
SET @c2 = (SELECT id FROM users WHERE email = 'sv002@sqs.edu.vn');
SET @c3 = (SELECT id FROM users WHERE email = 'sv003@sqs.edu.vn');

INSERT INTO customers (user_id) VALUES (@c1), (@c2), (@c3);

-- ============================================================
-- 13. VIEWS — Truy vấn nhanh cho Frontend
-- ============================================================

-- View: Hàng đợi hiện tại (chỉ Waiting)
CREATE VIEW v_waiting_queue AS
SELECT
    t.id,
    t.ticket_number,
    t.id_service,
    s.name          AS service_name,
    s.code          AS service_code,
    COALESCE(u.name, t.guest_name) AS customer_name,
    t.created_at,
    t.ticket_date,
    -- Vị trí trong hàng đợi (cùng service, cùng ngày)
    RANK() OVER (
        PARTITION BY t.id_service, t.ticket_date
        ORDER BY t.created_at
    ) AS queue_position
FROM tickets t
JOIN services s ON t.id_service = s.id
LEFT JOIN customers c ON t.id_customer = c.user_id
LEFT JOIN users u ON c.user_id = u.id
WHERE t.status = 'Waiting';

-- View: Thống kê KPI Staff hôm nay
CREATE VIEW v_staff_kpi_today AS
SELECT
    u.id            AS staff_id,
    u.name          AS staff_name,
    st.position,
    st.kpi          AS total_kpi,
    COUNT(CASE WHEN t.status = 'Completed'
               AND t.ticket_date = CURDATE() THEN 1 END) AS kpi_today,
    COUNT(CASE WHEN t.ticket_date = CURDATE() THEN 1 END) AS tickets_today
FROM staffs st
JOIN users u ON st.user_id = u.id
LEFT JOIN tickets t ON t.id_staff = st.user_id
GROUP BY u.id, u.name, st.position, st.kpi;

-- View: Màn hình tổng — số đang gọi
CREATE VIEW v_currently_calling AS
SELECT
    t.id,
    t.ticket_number,
    s.name          AS service_name,
    co.name         AS counter_name,
    u.name          AS staff_name,
    t.called_at
FROM tickets t
JOIN services s  ON t.id_service = s.id
JOIN counters co ON t.id_counter = co.id
JOIN staffs st   ON t.id_staff   = st.user_id
JOIN users u     ON st.user_id   = u.id
WHERE t.status = 'Calling'
  AND t.ticket_date = CURDATE();

-- ============================================================
-- 14. EVENTS — Tự động dọn dẹp & báo cáo
-- ============================================================

-- Kích hoạt event scheduler
SET GLOBAL event_scheduler = ON;

-- Event: Xóa daily_sequence cũ hơn 30 ngày (chạy mỗi ngày 00:01)
CREATE EVENT IF NOT EXISTS evt_cleanup_old_sequences
    ON SCHEDULE EVERY 1 DAY
    STARTS CURRENT_TIMESTAMP
    DO
        DELETE FROM daily_sequence
        WHERE seq_date < DATE_SUB(CURDATE(), INTERVAL 30 DAY);

-- ============================================================
-- DONE — Kiểm tra kết quả
-- ============================================================
SELECT 'Database SQS khởi tạo thành công!' AS status;

SELECT
    'users'            AS tbl, COUNT(*) AS rows FROM users    UNION ALL
SELECT 'customers',            COUNT(*)         FROM customers UNION ALL
SELECT 'staffs',               COUNT(*)         FROM staffs    UNION ALL
SELECT 'admins',               COUNT(*)         FROM admins    UNION ALL
SELECT 'services',             COUNT(*)         FROM services  UNION ALL
SELECT 'counters',             COUNT(*)         FROM counters  UNION ALL
SELECT 'counter_services',     COUNT(*)         FROM counter_services;
