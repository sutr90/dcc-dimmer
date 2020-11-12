#include "Arduino.h"
#include "hid_als.h"
#include <Wire.h>
#include <ClosedCube_OPT3001.h>

ClosedCube_OPT3001 opt3001;

#define OPT3001_ADDRESS 0x44

void configureSensor()
{
  OPT3001_Config newConfig;

  newConfig.RangeNumber = B1100;
  newConfig.ConvertionTime = B1;
  newConfig.Latch = B1;
  newConfig.ModeOfConversionOperation = B11;
  opt3001.writeConfig(newConfig);
}

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

  opt3001.begin(OPT3001_ADDRESS);
  configureSensor();
}

void loop()
{
  unsigned long currentMillis = millis();

  if (currentMillis - previousMillis >= interval)
  {
    previousMillis = currentMillis;

    OPT3001 result = opt3001.readResult();
    if (result.error == NO_ERROR)
    {
      uint8_t buffer[USB_DATA_SIZE];
      buffer[0] = lowByte(result.raw.rawData);
      buffer[1] = highByte(result.raw.rawData);

      HidAls.write(buffer, sizeof(buffer));
    }
  }
}
