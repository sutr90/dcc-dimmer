#include "Veml7700AmbientLightSensor.h"

#include <Adafruit_VEML7700.h>

namespace {
Adafruit_VEML7700 veml7700;
}

bool Veml7700AmbientLightSensor::begin() {
  sensorInitialized = veml7700.begin();
  return sensorInitialized;
}

bool Veml7700AmbientLightSensor::readLux(float &lux) {
  if (!sensorInitialized) {
    return false;
  }

  lux = veml7700.readLux();
  return true;
}
