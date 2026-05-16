#ifndef COMMAND_PROCESSOR_H
#define COMMAND_PROCESSOR_H

#include <stdint.h>

#include "SensorScaling.h"

enum class OperatingMode {
  Automatic,
  Manual
};

enum class CommandStatus {
  Ignored,
  ModeAutomatic,
  ManualValueSet,
  ScaleSet,
  InvalidValue,
  InvalidScale,
  UnknownCommand
};

struct AppState {
  OperatingMode mode;
  uint8_t manualValue;
  float scaleFactor;
  float scaledSensorThresholds[MAX_I2C_VALUE];
};

struct CommandResult {
  CommandStatus status;
  uint8_t manualValue;
  float scaleFactor;
};

// Sets the startup mode, default manual value, scale factor, and threshold table.
void initializeAppState(AppState &state);

// Chooses the value that should be sent over I2C for the current mode.
uint8_t selectI2cValue(float rawSensorValue, const AppState &state);

// Applies one complete serial command to the app state.
// The command buffer may be modified while tokenizing command/value parts.
CommandResult processCommand(char *command, AppState &state);

#endif
