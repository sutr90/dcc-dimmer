#include "SensorScaling.h"

void buildScaledSensorThresholds(float scaleFactor, float thresholds[MAX_I2C_VALUE]) {
  for (uint8_t outputValue = 0; outputValue < MAX_I2C_VALUE; outputValue++) {
    // round(scale * sqrt(x)) changes from n to n + 1 at n + 0.5.
    // Solving n + 0.5 = scale * sqrt(x) gives x = ((n + 0.5) / scale)^2.
    const float roundedBoundary = (static_cast<float>(outputValue) + 0.5f) / scaleFactor;
    thresholds[outputValue] = roundedBoundary * roundedBoundary;
  }
}

uint8_t scaleSensorValue(float rawValue, const float thresholds[MAX_I2C_VALUE]) {
  if (rawValue <= 0.0f) {
    return 0;
  }

  uint8_t lower = 0;
  uint8_t upper = MAX_I2C_VALUE;

  // lower_bound over thresholds: the first threshold greater than rawValue is
  // the rounded output value. Values beyond the last threshold clamp to 100.
  while (lower < upper) {
    const uint8_t middle = lower + ((upper - lower) / 2);

    if (rawValue < thresholds[middle]) {
      upper = middle;
    } else {
      lower = middle + 1;
    }
  }

  return lower;
}
