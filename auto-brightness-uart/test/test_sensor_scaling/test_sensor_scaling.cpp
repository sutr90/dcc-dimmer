#include <math.h>
#include <stdint.h>
#include <string.h>

#include <unity.h>

#include "CommandProcessor.h"
#include "SensorScaling.h"
#include "SerialCommandBuffer.h"

float thresholds[MAX_I2C_VALUE];
AppState appState;

uint8_t expectedScaledValue(float rawValue, float scaleFactor) {
  if (rawValue <= 0.0f) {
    return 0;
  }

  const long rounded = lround(scaleFactor * sqrt(rawValue));

  if (rounded < 0) {
    return 0;
  }

  if (rounded > MAX_I2C_VALUE) {
    return MAX_I2C_VALUE;
  }

  return static_cast<uint8_t>(rounded);
}

void setUp() {
  buildScaledSensorThresholds(DEFAULT_SCALE_FACTOR, thresholds);
  initializeAppState(appState);
}

void tearDown() {}

void test_default_scale_matches_formula_for_representative_values() {
  const float rawValues[] = {
    -1.0f,
    0.0f,
    0.01f,
    1.0f,
    10.0f,
    100.0f,
    231.0f,
    358.0f,
    1023.0f,
    1400.0f,
    80000.0f
  };

  for (uint8_t i = 0; i < sizeof(rawValues) / sizeof(rawValues[0]); i++) {
    TEST_ASSERT_EQUAL_UINT8(
      expectedScaledValue(rawValues[i], DEFAULT_SCALE_FACTOR),
      scaleSensorValue(rawValues[i], thresholds)
    );
  }
}

void test_default_scale_clamps_large_values_to_100() {
  TEST_ASSERT_EQUAL_UINT8(MAX_I2C_VALUE, scaleSensorValue(80000.0f, thresholds));
  TEST_ASSERT_EQUAL_UINT8(MAX_I2C_VALUE, scaleSensorValue(1000000.0f, thresholds));
}

void test_configured_scale_rebuilds_thresholds() {
  const float scaleFactor = 1.25f;

  buildScaledSensorThresholds(scaleFactor, thresholds);

  TEST_ASSERT_EQUAL_UINT8(expectedScaledValue(1.0f, scaleFactor), scaleSensorValue(1.0f, thresholds));
  TEST_ASSERT_EQUAL_UINT8(expectedScaledValue(6400.0f, scaleFactor), scaleSensorValue(6400.0f, thresholds));
  TEST_ASSERT_EQUAL_UINT8(expectedScaledValue(80000.0f, scaleFactor), scaleSensorValue(80000.0f, thresholds));
}

void test_rounding_boundaries_are_respected() {
  const float scaleFactor = 2.0f;

  buildScaledSensorThresholds(scaleFactor, thresholds);

  TEST_ASSERT_EQUAL_UINT8(0, scaleSensorValue(0.0624f, thresholds));
  TEST_ASSERT_EQUAL_UINT8(1, scaleSensorValue(0.0625f, thresholds));
  TEST_ASSERT_EQUAL_UINT8(1, scaleSensorValue(0.5624f, thresholds));
  TEST_ASSERT_EQUAL_UINT8(2, scaleSensorValue(0.5625f, thresholds));
}

void test_manual_command_switches_mode_and_selects_manual_i2c_value() {
  char command[] = "MANUAL 42";

  const CommandResult result = processCommand(command, appState);

  TEST_ASSERT_EQUAL(static_cast<int>(CommandStatus::ManualValueSet), static_cast<int>(result.status));
  TEST_ASSERT_EQUAL(static_cast<int>(OperatingMode::Manual), static_cast<int>(appState.mode));
  TEST_ASSERT_EQUAL_UINT8(42, appState.manualValue);
  TEST_ASSERT_EQUAL_UINT8(42, selectI2cValue(1000.0f, appState));
}

void test_plain_number_command_switches_to_manual_mode() {
  char command[] = "77";

  const CommandResult result = processCommand(command, appState);

  TEST_ASSERT_EQUAL(static_cast<int>(CommandStatus::ManualValueSet), static_cast<int>(result.status));
  TEST_ASSERT_EQUAL(static_cast<int>(OperatingMode::Manual), static_cast<int>(appState.mode));
  TEST_ASSERT_EQUAL_UINT8(77, selectI2cValue(0.0f, appState));
}

void test_auto_command_switches_back_to_scaled_sensor_value() {
  char manualCommand[] = "SET=25";
  char autoCommand[] = "AUTO";

  processCommand(manualCommand, appState);
  const CommandResult result = processCommand(autoCommand, appState);

  TEST_ASSERT_EQUAL(static_cast<int>(CommandStatus::ModeAutomatic), static_cast<int>(result.status));
  TEST_ASSERT_EQUAL(static_cast<int>(OperatingMode::Automatic), static_cast<int>(appState.mode));
  TEST_ASSERT_EQUAL_UINT8(expectedScaledValue(100.0f, DEFAULT_SCALE_FACTOR), selectI2cValue(100.0f, appState));
}

void test_scale_command_rebuilds_thresholds() {
  char command[] = "SCALE 1.25";

  const CommandResult result = processCommand(command, appState);

  TEST_ASSERT_EQUAL(static_cast<int>(CommandStatus::ScaleSet), static_cast<int>(result.status));
  TEST_ASSERT_FLOAT_WITHIN(0.001f, 1.25f, appState.scaleFactor);
  TEST_ASSERT_EQUAL_UINT8(expectedScaledValue(6400.0f, 1.25f), selectI2cValue(6400.0f, appState));
}

void test_invalid_manual_value_does_not_change_state() {
  char command[] = "MANUAL 101";

  const CommandResult result = processCommand(command, appState);

  TEST_ASSERT_EQUAL(static_cast<int>(CommandStatus::InvalidValue), static_cast<int>(result.status));
  TEST_ASSERT_EQUAL(static_cast<int>(OperatingMode::Automatic), static_cast<int>(appState.mode));
  TEST_ASSERT_EQUAL_UINT8(0, appState.manualValue);
}

void test_serial_command_buffer_collects_line_without_blocking() {
  SerialCommandBuffer buffer;

  TEST_ASSERT_EQUAL(static_cast<int>(SerialInputStatus::None), static_cast<int>(buffer.push('S')));
  TEST_ASSERT_EQUAL(static_cast<int>(SerialInputStatus::None), static_cast<int>(buffer.push('E')));
  TEST_ASSERT_EQUAL(static_cast<int>(SerialInputStatus::None), static_cast<int>(buffer.push('T')));
  TEST_ASSERT_EQUAL(static_cast<int>(SerialInputStatus::None), static_cast<int>(buffer.push(' ')));
  TEST_ASSERT_EQUAL(static_cast<int>(SerialInputStatus::None), static_cast<int>(buffer.push('5')));
  TEST_ASSERT_EQUAL(static_cast<int>(SerialInputStatus::LineReady), static_cast<int>(buffer.push('\n')));
  TEST_ASSERT_EQUAL_STRING("SET 5", buffer.command());
}

void test_serial_command_buffer_ignores_carriage_return() {
  SerialCommandBuffer buffer;

  buffer.push('A');
  TEST_ASSERT_EQUAL(static_cast<int>(SerialInputStatus::None), static_cast<int>(buffer.push('\r')));
  buffer.push('U');
  buffer.push('T');
  buffer.push('O');
  TEST_ASSERT_EQUAL(static_cast<int>(SerialInputStatus::LineReady), static_cast<int>(buffer.push('\n')));
  TEST_ASSERT_EQUAL_STRING("AUTO", buffer.command());
}

int main(int argc, char **argv) {
  UNITY_BEGIN();
  RUN_TEST(test_default_scale_matches_formula_for_representative_values);
  RUN_TEST(test_default_scale_clamps_large_values_to_100);
  RUN_TEST(test_configured_scale_rebuilds_thresholds);
  RUN_TEST(test_rounding_boundaries_are_respected);
  RUN_TEST(test_manual_command_switches_mode_and_selects_manual_i2c_value);
  RUN_TEST(test_plain_number_command_switches_to_manual_mode);
  RUN_TEST(test_auto_command_switches_back_to_scaled_sensor_value);
  RUN_TEST(test_scale_command_rebuilds_thresholds);
  RUN_TEST(test_invalid_manual_value_does_not_change_state);
  RUN_TEST(test_serial_command_buffer_collects_line_without_blocking);
  RUN_TEST(test_serial_command_buffer_ignores_carriage_return);
  return UNITY_END();
}
