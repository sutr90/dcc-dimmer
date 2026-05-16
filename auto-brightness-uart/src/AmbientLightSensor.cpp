#include "AmbientLightSensor.h"

#include <ClosedCube_OPT3001.h>

const uint8_t OPT3001_ADDRESS = 0x44;

ClosedCube_OPT3001 opt3001;
bool sensorInitialized = false;

bool beginAmbientLightSensor() {
  opt3001.begin(OPT3001_ADDRESS);

  OPT3001_Config config;
  config.rawData = 0;
  config.RangeNumber = B1100;
  config.ConvertionTime = B0;
  config.Latch = B1;
  config.ModeOfConversionOperation = B11;

  sensorInitialized = opt3001.writeConfig(config) == NO_ERROR;
  return sensorInitialized;
}

bool readAmbientLightLux(float &lux) {
  if (!sensorInitialized) {
    return false;
  }

  const OPT3001 result = opt3001.readResult();

  if (result.error != NO_ERROR) {
    return false;
  }

  lux = result.lux;
  return true;
}
