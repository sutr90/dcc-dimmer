#ifndef AMBIENT_LIGHT_SENSOR_H
#define AMBIENT_LIGHT_SENSOR_H

// Initializes the currently installed ambient light sensor. The rest of the
// program talks through this wrapper so the hardware can be swapped later.
bool beginAmbientLightSensor();

// Reads the current ambient light level in lux. Returns false if the sensor
// does not have a valid value available.
bool readAmbientLightLux(float &lux);

#endif
