#include "Arduino.h"
#include "hid_als.h"

const int pinLed = LED_BUILTIN;
const int analogInPin = A0;

uint8_t rawhidData[USB_DATA_SIZE];

const long interval = 1000;
unsigned long previousMillis = 0;

void setup()
{
  pinMode(pinLed, OUTPUT);
  HidAls.begin(rawhidData, sizeof(rawhidData));
  digitalWrite(pinLed, LOW);
}

void loop()
{
  unsigned long currentMillis = millis();

  if (currentMillis - previousMillis >= interval)
  {
    previousMillis = currentMillis;

    uint8_t buffer[USB_DATA_SIZE];
    buffer[0] = 0xef;
    buffer[1] = 0xbe;
    buffer[2] = 0xad;
    buffer[3] = 0xde;
    HidAls.write(buffer, sizeof(buffer));
  }
}
