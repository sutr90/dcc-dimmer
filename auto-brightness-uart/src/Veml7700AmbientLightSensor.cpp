#include "Veml7700AmbientLightSensor.h"

#include <Arduino.h>
#include <SoftWire.h>

namespace {

const uint8_t VEML7700_ADDRESS = 0x10;
const uint8_t CONFIG_REGISTER = 0x00;
const uint8_t POWER_SAVE_REGISTER = 0x03;
const uint8_t ALS_REGISTER = 0x04;
const uint8_t SDA_PIN = 11;
const uint8_t SCL_PIN = 12;

const float MAX_RESOLUTION = 0.0036f;
const float MAX_GAIN = 2.0f;
const float MAX_INTEGRATION_MS = 800.0f;

uint8_t txBuffer[8];
uint8_t rxBuffer[8];
SoftWire vemlWire(SDA_PIN, SCL_PIN);
bool wireConfigured = false;
uint8_t currentGain = 2;       // VEML7700 gain code: 1/8x
uint8_t currentIntegration = 0; // VEML7700 integration code: 100 ms
unsigned long lastReadMs = 0;

int integrationTimeMs(uint8_t integration) {
  switch (integration) {
    case 0x0C: return 25;
    case 0x08: return 50;
    case 0x00: return 100;
    case 0x01: return 200;
    case 0x02: return 400;
    case 0x03: return 800;
    default: return -1;
  }
}

bool writeRegister(uint8_t reg, uint16_t value) {
  vemlWire.beginTransmission(VEML7700_ADDRESS);
  vemlWire.write(reg);
  vemlWire.write(static_cast<uint8_t>(value & 0xFF));
  vemlWire.write(static_cast<uint8_t>(value >> 8));
  return vemlWire.endTransmission() == 0;
}

bool readRegister(uint8_t reg, uint16_t &value) {
  vemlWire.beginTransmission(VEML7700_ADDRESS);
  vemlWire.write(reg);
  if (vemlWire.endTransmission(false) != 0) {
    return false;
  }

  if (vemlWire.requestFrom(VEML7700_ADDRESS, static_cast<uint8_t>(2)) != 2) {
    while (vemlWire.available() > 0) {
      vemlWire.read();
    }
    return false;
  }

  const uint8_t low = vemlWire.read();
  const uint8_t high = vemlWire.read();
  value = (static_cast<uint16_t>(high) << 8) | low;
  return true;
}

bool configure(uint8_t gain, uint8_t integration, bool enabled) {
  const uint16_t config =
      (static_cast<uint16_t>(gain) << 11) |
      (static_cast<uint16_t>(integration) << 6) |
      (enabled ? 0 : 1);

  currentGain = gain;
  currentIntegration = integration;
  lastReadMs = millis();
  return writeRegister(CONFIG_REGISTER, config);
}

bool readAls(uint16_t &als, bool wait) {
  const int integrationMs = integrationTimeMs(currentIntegration);
  if (integrationMs < 0) {
    return false;
  }

  if (wait) {
    const unsigned long requiredWait = static_cast<unsigned long>(integrationMs) * 2;
    const unsigned long elapsed = millis() - lastReadMs;
    if (elapsed < requiredWait) {
      delay(requiredWait - elapsed);
    }
  }

  if (!readRegister(ALS_REGISTER, als)) {
    return false;
  }

  lastReadMs = millis();
  return true;
}

float resolution() {
  return MAX_RESOLUTION * (MAX_INTEGRATION_MS / integrationTimeMs(currentIntegration)) *
         (MAX_GAIN / (currentGain == 0 ? 1.0f :
                      currentGain == 1 ? 2.0f :
                      currentGain == 2 ? 0.125f : 0.25f));
}

float correctedLux(uint16_t als, bool correction) {
  float lux = resolution() * als;
  if (correction) {
    lux = (((6.0135e-13f * lux - 9.3924e-9f) * lux + 8.1488e-5f) * lux + 1.0023f) * lux;
  }
  return lux;
}

float autoLux() {
  const uint8_t gains[] = {2, 3, 0, 1};       // 1/8, 1/4, 1, 2
  const uint8_t integrations[] = {0x0C, 0x08, 0x00, 0x01, 0x02, 0x03};
  uint8_t gainIndex = 0;
  uint8_t integrationIndex = 2;
  bool useCorrection = false;

  configure(gains[gainIndex], integrations[integrationIndex], true);

  uint16_t als = 0;
  if (!readAls(als, true)) {
    return -1.0f;
  }

  if (als <= 100) {
    while (als <= 100 && !(gainIndex == 3 && integrationIndex == 5)) {
      if (gainIndex < 3) {
        gainIndex++;
      } else if (integrationIndex < 5) {
        integrationIndex++;
      }
      if (!configure(gains[gainIndex], integrations[integrationIndex], true) ||
          !readAls(als, true)) {
        return -1.0f;
      }
    }
  } else {
    useCorrection = true;
    while (als > 10000 && integrationIndex > 0) {
      integrationIndex--;
      if (!configure(gains[gainIndex], integrations[integrationIndex], true) ||
          !readAls(als, true)) {
        return -1.0f;
      }
    }
  }

  return correctedLux(als, useCorrection);
}

}  // namespace

bool Veml7700AmbientLightSensor::begin() {
#ifdef DDC_DEBUG
  Serial.println("trace=veml_softwire_begin_enter");
#endif
  if (!wireConfigured) {
    vemlWire.setTxBuffer(txBuffer, sizeof(txBuffer));
    vemlWire.setRxBuffer(rxBuffer, sizeof(rxBuffer));
    vemlWire.begin();
    vemlWire.setClock(100000);
    wireConfigured = true;
  }

  uint16_t ignored = 0;
  sensorInitialized = readRegister(CONFIG_REGISTER, ignored) &&
                      configure(2, 0x00, false) &&
                      writeRegister(POWER_SAVE_REGISTER, 0x0000) &&
                      configure(2, 0x00, true);
  delay(5);

#ifdef DDC_DEBUG
  Serial.print("trace=veml_softwire_begin_exit initialized=");
  Serial.println(sensorInitialized ? 1 : 0);
#endif
  return sensorInitialized;
}

bool Veml7700AmbientLightSensor::readLux(float &lux) {
  if (!sensorInitialized) {
    return false;
  }

  lux = autoLux();
  return lux >= 0.0f;
}
