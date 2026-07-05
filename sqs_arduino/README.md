# Hướng dẫn thiết lập Proteus (Mô phỏng IoT)

Trong Phase 11, chúng ta sử dụng Proteus để mô phỏng mạch cứng Arduino.

## 1. Thành phần mạch cần có trong Proteus
- **Arduino UNO R3**: Vi điều khiển chính.
- **LM016L (LCD 16x2)**: Màn hình hiển thị số đang gọi.
- **BUZZER**: Còi chíp báo hiệu khi có số mới.
- **BUTTON**: 2 nút nhấn (Next và Reset).
- **COMPIM**: Module mô phỏng cổng COM ảo.

## 2. Cách kết nối dây (Wiring)
- **LCD**: 
  - RS -> Chân 12 (Arduino)
  - E -> Chân 11 (Arduino)
  - D4 -> Chân 5, D5 -> Chân 4, D6 -> Chân 3, D7 -> Chân 2.
- **BUTTON 1 (Gọi số tiếp)**:
  - Một đầu nối vào Chân 8 của Arduino.
  - Đầu kia nối GND. (Arduino cấu hình `INPUT_PULLUP`).
- **BUTTON 2 (Reset màn hình)**:
  - Nối vào Chân 9, đầu kia nối GND.
- **BUZZER**:
  - Chân dương (+) nối vào Chân 7 (Arduino).
  - Chân âm (-) nối GND.
- **COMPIM (Serial)**:
  - TXD -> Chân RX (0) của Arduino.
  - RXD -> Chân TX (1) của Arduino.
  - Chỉnh Baud rate của COMPIM: 9600.
  - Chỉnh Physical Port: COM4 (hoặc port tương ứng tạo bằng Virtual Serial Port Emulator).

## 3. Cách nạp code và chạy
1. Cài đặt phần mềm tạo cổng COM ảo (VD: **Virtual Serial Port Emulator - VSPE**).
2. Tạo 1 cặp COM port (Pair): `COM3` <-> `COM4`.
3. Trong file cấu hình Backend (`SQS.API/appsettings.json`) hoặc WinForms Desktop, set `PortName` là `COM3`.
4. Trong Proteus COMPIM, set `Physical Port` là `COM4`.
5. Mở Arduino IDE, biên dịch file `sqs_arduino.ino` lấy đường dẫn file `.hex`.
6. Click đúp vào Arduino trong Proteus -> Nạp file `.hex` vào mục *Program File*.
7. Bấm **Play** (Run) trên Proteus.
8. Khi nhấn nút trên Proteus, nó sẽ gửi tín hiệu qua COM port, Backend nhận được sẽ tự động trigger API Gọi số (Call Next).
