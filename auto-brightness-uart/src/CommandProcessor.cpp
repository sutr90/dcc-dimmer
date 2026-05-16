#include "CommandProcessor.h"

#include <stdlib.h>
#include <string.h>

namespace {

// AVR libc does not always provide a portable strcasecmp declaration, so keep a
// tiny ASCII-only comparison for the simple command words used here.
bool commandEquals(const char *left, const char *right) {
  while (*left != '\0' && *right != '\0') {
    char leftChar = *left;
    char rightChar = *right;

    if (leftChar >= 'a' && leftChar <= 'z') {
      leftChar -= 'a' - 'A';
    }

    if (rightChar >= 'a' && rightChar <= 'z') {
      rightChar -= 'a' - 'A';
    }

    if (leftChar != rightChar) {
      return false;
    }

    left++;
    right++;
  }

  return *left == '\0' && *right == '\0';
}

// Parses the direct I2C value accepted in manual mode. Manual values are already
// in the final 0..100 output range, not raw sensor units.
bool parseManualValue(const char *text, uint8_t &value) {
  if (*text == '\0') {
    return false;
  }

  char *end = nullptr;
  const long parsed = strtol(text, &end, 10);

  if (*end != '\0' || parsed < 0 || parsed > MAX_I2C_VALUE) {
    return false;
  }

  value = static_cast<uint8_t>(parsed);
  return true;
}

// Parses the coefficient used in scale * sqrt(raw). Zero and negative values
// would make threshold generation invalid, so they are rejected.
bool parseScaleFactor(const char *text, float &value) {
  if (*text == '\0') {
    return false;
  }

  char *end = nullptr;
  const float parsed = strtod(text, &end);

  if (*end != '\0' || parsed <= 0.0f || parsed > 10.0f) {
    return false;
  }

  value = parsed;
  return true;
}

}  // namespace

void initializeAppState(AppState &state) {
  state.mode = OperatingMode::Automatic;
  state.manualValue = 0;
  state.scaleFactor = DEFAULT_SCALE_FACTOR;
  buildScaledSensorThresholds(state.scaleFactor, state.scaledSensorThresholds);
}

uint8_t selectI2cValue(float rawSensorValue, const AppState &state) {
  if (state.mode == OperatingMode::Manual) {
    return state.manualValue;
  }

  return scaleSensorValue(rawSensorValue, state.scaledSensorThresholds);
}

CommandResult processCommand(char *command, AppState &state) {
  if (*command == '\0') {
    return {CommandStatus::Ignored, state.manualValue, state.scaleFactor};
  }

  if (commandEquals(command, "AUTO")) {
    state.mode = OperatingMode::Automatic;
    return {CommandStatus::ModeAutomatic, state.manualValue, state.scaleFactor};
  }

  char *separator = strchr(command, ' ');
  if (separator == nullptr) {
    separator = strchr(command, '=');
  }

  char *valueText = command;
  if (separator != nullptr) {
    // Split commands like "SET 42" and "SCALE=2.669" in-place so command and
    // value can be parsed without allocating another buffer on the Nano.
    *separator = '\0';
    valueText = separator + 1;
  }

  if (commandEquals(command, "SCALE")) {
    float value = 0.0f;
    if (!parseScaleFactor(valueText, value)) {
      return {CommandStatus::InvalidScale, state.manualValue, state.scaleFactor};
    }

    state.scaleFactor = value;
    buildScaledSensorThresholds(state.scaleFactor, state.scaledSensorThresholds);
    return {CommandStatus::ScaleSet, state.manualValue, state.scaleFactor};
  }

  if (separator == nullptr || commandEquals(command, "MANUAL") || commandEquals(command, "SET")) {
    uint8_t value = 0;
    if (!parseManualValue(valueText, value)) {
      return {CommandStatus::InvalidValue, state.manualValue, state.scaleFactor};
    }

    state.manualValue = value;
    state.mode = OperatingMode::Manual;
    return {CommandStatus::ManualValueSet, state.manualValue, state.scaleFactor};
  }

  return {CommandStatus::UnknownCommand, state.manualValue, state.scaleFactor};
}
