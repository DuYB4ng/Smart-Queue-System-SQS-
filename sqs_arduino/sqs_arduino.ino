#include <LiquidCrystal.h>

/*
 * SMART QUEUE SYSTEM (SQS) - ARDUINO FIRMWARE
 * 
 * Hardware Connections (Proteus Simulation):
 * - LCD 16x2 (RS=12, EN=11, D4=5, D5=4, D6=3, D7=2)
 * - Push Button 1 (Next): Pin 8 (INPUT_PULLUP)
 * - Push Button 2 (Reset): Pin 9 (INPUT_PULLUP)
 * - Buzzer: Pin 7
 * - Virtual Serial (COMPIM): TX=1, RX=0 (Baud 9600)
 */

// --- PIN DEFINITIONS ---
const int rs = 12, en = 11, d4 = 5, d5 = 4, d6 = 3, d7 = 2;
LiquidCrystal lcd(rs, en, d4, d5, d6, d7);

// --- STATE VARIABLES ---
String inputBuffer = "";
bool newCommandReceived = false;

void setup() {
  // Initialize Serial
  Serial.begin(9600);
  
  // Initialize Pins
  pinMode(7, OUTPUT); // BUZZER_PIN
  
  // Initialize LCD
  lcd.begin(16, 2);
  lcd.clear();
  lcd.setCursor(0, 0);
  lcd.print(" SQS SYSTEM ");
  lcd.setCursor(0, 1);
  lcd.print("  READY...  ");
  
  // Beep to indicate startup
  beep(100);
  delay(100);
  beep(100);
  delay(1000);
  
  lcd.clear();
  lcd.setCursor(0, 0);
  lcd.print("QUAY SO: 01"); // Giả sử là quầy 1
  lcd.setCursor(0, 1);
  lcd.print("Chua goi khach");
}

void loop() {
  // 1. READ SERIAL COMMANDS
  while (Serial.available() > 0) {
    char c = (char)Serial.read();
    if (c == '\n') {
      newCommandReceived = true;
      break; // End of command
    } else {
      inputBuffer += c;
    }
  }

  // 2. PROCESS SERIAL COMMANDS
  if (newCommandReceived) {
    processCommand(inputBuffer);
    inputBuffer = "";
    newCommandReceived = false;
  }

  // 3. NO BUTTONS IN THIS VERSION
}

// --- HELPER FUNCTIONS ---

void processCommand(String cmd) {
  cmd.trim();
  
  if (cmd.startsWith("CALL:")) {
    // Lệnh: CALL:001
    String ticketNum = cmd.substring(5);
    
    // Bật LCD
    lcd.clear();
    lcd.setCursor(0, 0);
    lcd.print("XIN MOI KHACH:");
    lcd.setCursor(0, 1);
    lcd.print("   >> ");
    lcd.print(ticketNum);
    lcd.print(" <<");
    
    // Kêu còi báo hiệu
    beep(500);
    
    // Phản hồi về cho PC
    Serial.println("ACK:OK");
    
  } else if (cmd.startsWith("MSG:")) {
    // Lệnh hiển thị chuỗi bất kỳ
    String msg = cmd.substring(4);
    lcd.clear();
    lcd.setCursor(0, 0);
    lcd.print(msg);
    Serial.println("ACK:OK");
    
  } else if (cmd == "RESET") {
    lcd.clear();
    lcd.setCursor(0, 0);
    lcd.print("QUAY SO: 01");
    lcd.setCursor(0, 1);
    lcd.print("Chua goi khach");
    Serial.println("ACK:OK");
  }
}

void beep(int durationMs) {
  digitalWrite(7, HIGH); // BUZZER_PIN
  delay(durationMs);
  digitalWrite(7, LOW);
}
