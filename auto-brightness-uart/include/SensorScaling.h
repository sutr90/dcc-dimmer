#ifndef SENSOR_SCALING_H
#define SENSOR_SCALING_H

#include <stdint.h>

const uint8_t MAX_I2C_VALUE = 100;
const float DEFAULT_SCALE_FACTOR = 2.669f;

// Builds the threshold table used to approximate round(scaleFactor * sqrt(raw)).
// This does the expensive sqrt inversion only when the scale changes.
void buildScaledSensorThresholds(float scaleFactor, float thresholds[MAX_I2C_VALUE]);

// Converts a positive raw sensor value to the 0..100 I2C value by searching the
// precomputed thresholds. Runtime path avoids sqrt and floating multiplication.
uint8_t scaleSensorValue(float rawValue, const float thresholds[MAX_I2C_VALUE]);

#endif
