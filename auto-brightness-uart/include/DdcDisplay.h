#ifndef DDC_DISPLAY_H
#define DDC_DISPLAY_H

#include <stdint.h>

// DDC/CI uses the display's 7-bit I2C address. 0x37 is the common address for
// the connected monitor on the DDC bus.
const uint8_t DDC_DISPLAY_ADDRESS = 0x37;
const uint8_t DDC_DISPLAY_WRITE_ADDRESS = DDC_DISPLAY_ADDRESS << 1;
const uint8_t DDC_SOURCE_ADDRESS = 0x51;
const uint8_t DDC_SET_VCP_FEATURE = 0x03;
const uint8_t DDC_GET_VCP_FEATURE = 0x01;
const uint8_t DDC_GET_VCP_REPLY = 0x02;
const uint8_t DDC_VCP_BRIGHTNESS = 0x10;
const uint8_t DDC_SET_VCP_LENGTH = 0x84;
const uint8_t DDC_GET_VCP_LENGTH = 0x82;
const uint8_t DDC_GET_VCP_REPLY_LENGTH = 11;
const unsigned long DDC_BUS_IDLE_GUARD_MS = 10;

enum class DdcReadStatus {
  Deferred,
  Success,
  Failed
};

enum class DdcDebugEvent {
  BusBusy,
  WriteResult,
  ShortRead,
  InvalidReply,
  Reply
};

using DdcDebugLogger = void (*)(DdcDebugEvent event, uint8_t value, const uint8_t *data, uint8_t length);

// Initializes the Arduino Wire bus used for DDC/CI.
void beginDdcDisplay();

// Installs an optional diagnostic hook. The DDC layer never writes to Serial
// directly, so callers can decide whether and how much debug output to emit.
void setDdcDebugLogger(DdcDebugLogger logger);

// Sends DDC/CI "Set VCP Feature" for brightness. The value is expected to be
// already scaled to the monitor's usual 0..100 brightness range. Returns false
// if the bus is not idle long enough yet and the caller should retry later.
bool sendDdcBrightness(uint8_t brightness, unsigned long now);

// Sends DDC/CI "Get VCP Feature" for brightness. The caller should wait before
// calling readDdcBrightness(), because displays need time to prepare the reply.
// Returns false if the bus is not idle long enough yet.
bool requestDdcBrightness(unsigned long now);

// Reads and parses the pending DDC/CI brightness reply. Deferred means another
// master still appears active on the bus, so the caller should retry later.
DdcReadStatus readDdcBrightness(uint8_t &brightness, unsigned long now);

#endif
