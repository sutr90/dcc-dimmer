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
    auto sensorValue = analogRead(analogInPin);
    buffer[0] = 1;
    buffer[1] = 0;
    HidAls.write(buffer, sizeof(buffer));
  }
}
