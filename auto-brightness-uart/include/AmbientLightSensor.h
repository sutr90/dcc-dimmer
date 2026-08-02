#ifndef AMBIENT_LIGHT_SENSOR_H
#define AMBIENT_LIGHT_SENSOR_H

class AmbientLightSensor {
public:
  virtual ~AmbientLightSensor();

  virtual bool begin() = 0;

  // Reads the current ambient light level in lux. Returns false if the sensor
  // does not have a valid value available.
  virtual bool readLux(float &lux) = 0;
};

#endif
