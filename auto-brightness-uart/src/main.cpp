#include <Arduino.h>

#include "AmbientLightSensor.h"
#include "CommandProcessor.h"
#include "DdcDisplay.h"
#include "SerialCommandBuffer.h"

const unsigned long REPORT_INTERVAL_MS = 1000;
const unsigned long DDC_BRIGHTNESS_READ_DELAY_MS = 50;
const unsigned long DDC_BRIGHTNESS_POLL_INTERVAL_MS = 1000;

unsigned long lastReportMs = 0;
unsigned long brightnessRequestMs = 0;
unsigned long lastBrightnessReadMs = 0;
bool brightnessWritePending = false;
bool brightnessReadRequestPending = false;
bool brightnessReadPending = false;
bool displayBrightnessKnown = false;
bool needsReadBack = false;
uint8_t pendingBrightness = 0;
uint8_t displayBrightness = 0;
AppState appState;
SerialCommandBuffer serialCommandBuffer;

void handleSerialInput();
void queueDisplayBrightnessWrite(uint8_t brightness);
void serviceDisplayBrightnessWrite(unsigned long now);
void serviceDisplayBrightnessRequest(unsigned long now);
void serviceDisplayBrightnessRead(unsigned long now);
void serviceDisplayBrightnessPolling(unsigned long now);
void scheduleDisplayBrightnessRead();
void printCommandResult(const CommandResult &result);
void printSensorReport(bool sensorValueKnown, float sensorValue);
#ifdef DDC_DEBUG
void printDdcDebug(DdcDebugEvent event, uint8_t value, const uint8_t *data, uint8_t length);
void printHexByte(uint8_t value);
#endif

void setup() {
  Serial.begin(9600);
  beginDdcDisplay();
#ifdef DDC_DEBUG
  setDdcDebugLogger(printDdcDebug);
  Serial.println("ddc=debug_enabled");
#endif
  scheduleDisplayBrightnessRead();
  beginAmbientLightSensor();
  initializeAppState(appState);
}

void loop() {
  handleSerialInput();

  const unsigned long now = millis();
  serviceDisplayBrightnessWrite(now);
  serviceDisplayBrightnessRequest(now);
  serviceDisplayBrightnessRead(now);
  serviceDisplayBrightnessPolling(now);

  if (now - lastReportMs >= REPORT_INTERVAL_MS) {
    lastReportMs = now;

    float sensorValue = 0.0f;
    const bool sensorValueKnown = readAmbientLightLux(sensorValue);
    printSensorReport(sensorValueKnown, sensorValue);

    if (sensorValueKnown && appState.mode == OperatingMode::Automatic) {
      const uint8_t brightness = selectI2cValue(sensorValue, appState);
      queueDisplayBrightnessWrite(brightness);
    }
  }
}

#ifdef DDC_DEBUG
void printDdcDebug(DdcDebugEvent event, uint8_t value, const uint8_t *data, uint8_t length) {
  static unsigned long lastBusBusyReportMs = 0;

  if (event == DdcDebugEvent::BusBusy) {
    const unsigned long now = millis();
    if (now - lastBusBusyReportMs < 1000) {
      return;
    }

    lastBusBusyReportMs = now;
  }

  Serial.print("ddc=");

  switch (event) {
    case DdcDebugEvent::BusBusy:
      Serial.print("bus_busy lines=");
      printHexByte(value);
      break;
    case DdcDebugEvent::WriteResult:
      Serial.print("write_result code=");
      Serial.print(value);
      break;
    case DdcDebugEvent::ShortRead:
      Serial.print("short_read bytes=");
      Serial.print(value);
      break;
    case DdcDebugEvent::InvalidReply:
      Serial.print("invalid_reply opcode=");
      printHexByte(value);
      break;
    case DdcDebugEvent::Reply:
      Serial.print("reply");
      break;
  }

  if (data != nullptr && length > 0) {
    Serial.print(" data=");
    for (uint8_t i = 0; i < length; i++) {
      if (i > 0) {
        Serial.print(' ');
      }
      printHexByte(data[i]);
    }
  }

  Serial.println();
}

void printHexByte(uint8_t value) {
  if (value < 0x10) {
    Serial.print('0');
  }
  Serial.print(value, HEX);
}
#endif

void handleSerialInput() {
  while (Serial.available() > 0) {
    const char incoming = Serial.read();
    const SerialInputStatus status = serialCommandBuffer.push(incoming);

    if (status == SerialInputStatus::LineReady) {
      // Process complete commands only; partial serial input never blocks the
      // periodic sensor reporting and I2C update.
      const CommandResult result = processCommand(serialCommandBuffer.command(), appState);
      printCommandResult(result);
      if (result.status == CommandStatus::ManualValueSet) {
        queueDisplayBrightnessWrite(result.manualValue);
      }
      serialCommandBuffer.reset();
    } else if (status == SerialInputStatus::Overflow) {
      Serial.println("error=command_too_long");
    }
  }
}

void queueDisplayBrightnessWrite(uint8_t brightness) {
  if (displayBrightnessKnown && brightness == displayBrightness) {
    return;
  }
  pendingBrightness = brightness;
  brightnessWritePending = true;
}

void serviceDisplayBrightnessWrite(unsigned long now) {
  if (!brightnessWritePending || brightnessReadRequestPending || brightnessReadPending) {
    return;
  }

  if (sendDdcBrightness(pendingBrightness, now)) {
    displayBrightness = pendingBrightness;
    displayBrightnessKnown = true;
    brightnessWritePending = false;
    lastBrightnessReadMs = now;
    needsReadBack = true;
  }
}

void serviceDisplayBrightnessRequest(unsigned long now) {
  if (!brightnessReadRequestPending) {
    return;
  }

  if (requestDdcBrightness(now)) {
    brightnessRequestMs = now;
    brightnessReadRequestPending = false;
    brightnessReadPending = true;
  }
}

void serviceDisplayBrightnessRead(unsigned long now) {
  if (!brightnessReadPending || now - brightnessRequestMs < DDC_BRIGHTNESS_READ_DELAY_MS) {
    return;
  }

  uint8_t brightness = 0;
  const DdcReadStatus status = readDdcBrightness(brightness, now);

  if (status == DdcReadStatus::Deferred) {
    return;
  }

  if (status == DdcReadStatus::Success) {
    displayBrightness = brightness;
    displayBrightnessKnown = true;
  }

  lastBrightnessReadMs = now;
  brightnessReadPending = false;
}

void serviceDisplayBrightnessPolling(unsigned long now) {
  if (brightnessWritePending || brightnessReadRequestPending || brightnessReadPending) {
    return;
  }

  if (needsReadBack && now - lastBrightnessReadMs >= DDC_BRIGHTNESS_POLL_INTERVAL_MS) {
    scheduleDisplayBrightnessRead();
    needsReadBack = false;
  }
}

void scheduleDisplayBrightnessRead() {
  brightnessReadRequestPending = true;
}

void printCommandResult(const CommandResult &result) {
  switch (result.status) {
    case CommandStatus::Ignored:
      break;
    case CommandStatus::ModeAutomatic:
      Serial.println("mode=automatic");
      break;
    case CommandStatus::ManualValueSet:
      Serial.print("mode=manual value=");
      Serial.println(result.manualValue);
      break;
    case CommandStatus::ScaleSet:
      Serial.print("scale=");
      Serial.println(result.scaleFactor, 3);
      break;
    case CommandStatus::InvalidValue:
      Serial.println("error=invalid_value");
      break;
    case CommandStatus::InvalidScale:
      Serial.println("error=invalid_scale");
      break;
    case CommandStatus::UnknownCommand:
      Serial.println("error=unknown_command");
      break;
  }
}

void printSensorReport(bool sensorValueKnown, float sensorValue) {
  Serial.print("sensor=");
  if (sensorValueKnown) {
    Serial.print(sensorValue, 2);
  } else {
    Serial.print("unknown");
  }

  Serial.print(",display=");

  if (displayBrightnessKnown) {
    Serial.println(displayBrightness);
  } else {
    Serial.println("unknown");
  }
}
