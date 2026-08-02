#ifndef VEML7700_AMBIENT_LIGHT_SENSOR_H
#define VEML7700_AMBIENT_LIGHT_SENSOR_H

#include "AmbientLightSensor.h"

class Veml7700AmbientLightSensor : public AmbientLightSensor {
public:
    bool begin() override;
    bool readLux(float &lux) override;

private:
    bool sensorInitialized = false;
};

#endif
