#include "Arduino.h"
#include "hid_als.h"

const int pinLed = LED_BUILTIN;
const int pinButton = 2;

uint8_t rawhidData[USB_DATA_SIZE];

void setup() {
  pinMode(pinLed, OUTPUT);
  HidAls.begin(rawhidData, sizeof(rawhidData));
  digitalWrite(pinLed, LOW);
}

void loop() {
    uint8_t megabuff[USB_DATA_SIZE];
    for (uint8_t i = 0; i < sizeof(megabuff); i++) {
      megabuff[i] = i;
    }
    HidAls.write(megabuff, sizeof(megabuff));

    delay(300);

  auto bytesAvailable = HidAls.available();
  if (bytesAvailable)
  {
    // while (bytesAvailable--) {
    //   Serial.println(HidAls.read());
    // }
  }
}
