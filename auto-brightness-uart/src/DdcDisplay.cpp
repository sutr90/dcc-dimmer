#include "DdcDisplay.h"

#include <Arduino.h>
#include <Wire.h>

namespace {

const uint8_t I2C_SDA_PIN = A4;
const uint8_t I2C_SCL_PIN = A5;

unsigned long busIdleSinceMs = 0;
DdcDebugLogger debugLogger = nullptr;

void logDdcDebug(DdcDebugEvent event, uint8_t value, const uint8_t *data = nullptr, uint8_t length = 0) {
  if (debugLogger != nullptr) {
    debugLogger(event, value, data, length);
  }
}

bool isBusIdle(unsigned long now) {
  // In a multi-master DDC setup, another master may already be using the bus.
  // Only start our transaction after both open-drain lines have been released
  // high for a short guard period.
  if (digitalRead(I2C_SDA_PIN) == LOW || digitalRead(I2C_SCL_PIN) == LOW) {
    busIdleSinceMs = now;
    logDdcDebug(DdcDebugEvent::BusBusy, (digitalRead(I2C_SDA_PIN) == LOW ? 0x01 : 0x00) |
                                           (digitalRead(I2C_SCL_PIN) == LOW ? 0x02 : 0x00));
    return false;
  }

  return now - busIdleSinceMs >= DDC_BUS_IDLE_GUARD_MS;
}

uint8_t ddcChecksum(const uint8_t *message, uint8_t length) {
  // DDC/CI checksum is the XOR of the display write address and every payload
  // byte, excluding the checksum byte itself.
  uint8_t checksum = DDC_DISPLAY_WRITE_ADDRESS;

  for (uint8_t i = 0; i < length; i++) {
    checksum ^= message[i];
  }

  return checksum;
}

bool writeDdcMessage(const uint8_t *message, uint8_t length, unsigned long now) {
  if (!isBusIdle(now)) {
    return false;
  }

  Wire.beginTransmission(DDC_DISPLAY_ADDRESS);

  for (uint8_t i = 0; i < length; i++) {
    Wire.write(message[i]);
  }

  Wire.write(ddcChecksum(message, length));
  const uint8_t result = Wire.endTransmission();
  busIdleSinceMs = millis();
  logDdcDebug(DdcDebugEvent::WriteResult, result, message, length);
  return result == 0;
}

}  // namespace

void beginDdcDisplay() {
  pinMode(I2C_SDA_PIN, INPUT);
  pinMode(I2C_SCL_PIN, INPUT);
  Wire.begin();
  busIdleSinceMs = millis();
}

void setDdcDebugLogger(DdcDebugLogger logger) {
  debugLogger = logger;
}

bool sendDdcBrightness(uint8_t brightness, unsigned long now) {
  const uint8_t message[] = {
    DDC_SOURCE_ADDRESS,
    DDC_SET_VCP_LENGTH,
    DDC_SET_VCP_FEATURE,
    DDC_VCP_BRIGHTNESS,
    0x00,
    brightness
  };

  return writeDdcMessage(message, sizeof(message), now);
}

bool requestDdcBrightness(unsigned long now) {
  const uint8_t message[] = {
    DDC_SOURCE_ADDRESS,
    DDC_GET_VCP_LENGTH,
    DDC_GET_VCP_FEATURE,
    DDC_VCP_BRIGHTNESS
  };

  return writeDdcMessage(message, sizeof(message), now);
}

DdcReadStatus readDdcBrightness(uint8_t &brightness, unsigned long now) {
  if (!isBusIdle(now)) {
    return DdcReadStatus::Deferred;
  }

  uint8_t response[DDC_GET_VCP_REPLY_LENGTH];
  const uint8_t received = Wire.requestFrom(DDC_DISPLAY_ADDRESS, DDC_GET_VCP_REPLY_LENGTH);
  busIdleSinceMs = millis();

  if (received != DDC_GET_VCP_REPLY_LENGTH) {
    while (Wire.available() > 0) {
      Wire.read();
    }

    logDdcDebug(DdcDebugEvent::ShortRead, received);
    return DdcReadStatus::Failed;
  }

  for (uint8_t i = 0; i < DDC_GET_VCP_REPLY_LENGTH; i++) {
    response[i] = Wire.read();
  }

  logDdcDebug(DdcDebugEvent::Reply, DDC_GET_VCP_REPLY_LENGTH, response, DDC_GET_VCP_REPLY_LENGTH);

  if (response[2] != DDC_GET_VCP_REPLY || response[3] != 0x00 || response[4] != DDC_VCP_BRIGHTNESS) {
    logDdcDebug(DdcDebugEvent::InvalidReply, response[2], response, DDC_GET_VCP_REPLY_LENGTH);
    return DdcReadStatus::Failed;
  }

  const uint16_t currentValue = (static_cast<uint16_t>(response[8]) << 8) | response[9];

  if (currentValue > 100) {
    brightness = 100;
  } else {
    brightness = static_cast<uint8_t>(currentValue);
  }

  return DdcReadStatus::Success;
}
