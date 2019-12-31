#include "Arduino.h"
#include "hid_als.h"

const int pinLed = LED_BUILTIN;
const int pinButton = 2;
const int analogInPin = A0;

uint8_t rawhidData[USB_DATA_SIZE];

void setup() {
  pinMode(pinLed, OUTPUT);
  HidAls.begin(rawhidData, sizeof(rawhidData));
  digitalWrite(pinLed, LOW);
}

void loop() {
    uint8_t buffer[USB_DATA_SIZE];
    auto sensorValue = analogRead(analogInPin);
    buffer[0] = lowByte(sensorValue);
    buffer[1] = highByte(sensorValue);
    HidAls.write(buffer, sizeof(buffer));

    delay(300);

  // auto bytesAvailable = HidAls.available();
  // if (bytesAvailable)
  // {
  //   // while (bytesAvailable--) {
  //   //   Serial.println(HidAls.read());
  //   // }
  // }
}
