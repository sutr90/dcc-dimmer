#ifndef SERIAL_COMMAND_BUFFER_H
#define SERIAL_COMMAND_BUFFER_H

#include <stdint.h>

const uint8_t SERIAL_COMMAND_BUFFER_SIZE = 32;

enum class SerialInputStatus {
  None,
  LineReady,
  Overflow
};

class SerialCommandBuffer {
 public:
  SerialCommandBuffer();

  // Adds one received serial byte and reports whether a full line is ready.
  // This never waits for more data, so it is safe to call from loop().
  SerialInputStatus push(char incoming);

  // Returns the null-terminated command line after push() reports LineReady.
  char *command();

  // Clears the buffered partial command after processing or overflow.
  void reset();

 private:
  char buffer[SERIAL_COMMAND_BUFFER_SIZE];
  uint8_t length;
};

#endif
