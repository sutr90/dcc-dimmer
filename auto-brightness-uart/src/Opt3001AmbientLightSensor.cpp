#include "Opt3001AmbientLightSensor.h"

#include <ClosedCube_OPT3001.h>

namespace {
const uint8_t OPT3001_ADDRESS = 0x44;
}

bool Opt3001AmbientLightSensor::begin() {
  static ClosedCube_OPT3001 opt3001;
  sensor = &opt3001;
  sensor->begin(OPT3001_ADDRESS);

  OPT3001_Config config;
  config.rawData = 0;
  config.RangeNumber = B1100;
  config.ConvertionTime = B0;
  config.Latch = B1;
  config.ModeOfConversionOperation = B11;

  sensorInitialized = sensor->writeConfig(config) == NO_ERROR;
  return sensorInitialized;
}

bool Opt3001AmbientLightSensor::readLux(float &lux) {
  if (!sensorInitialized || sensor == nullptr) {
    return false;
  }

  const OPT3001 result = sensor->readResult();

  if (result.error != NO_ERROR) {
    return false;
  }

  lux = result.lux;
  return true;
}
