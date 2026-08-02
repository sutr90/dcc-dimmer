#ifndef OPT3001_AMBIENT_LIGHT_SENSOR_H
#define OPT3001_AMBIENT_LIGHT_SENSOR_H

#include "AmbientLightSensor.h"

class ClosedCube_OPT3001;

class Opt3001AmbientLightSensor : public AmbientLightSensor {
public:
  bool begin() override;
  bool readLux(float &lux) override;

private:
  ClosedCube_OPT3001 *sensor = nullptr;
  bool sensorInitialized = false;
};

#endif
