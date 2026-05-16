#include "SerialCommandBuffer.h"

SerialCommandBuffer::SerialCommandBuffer() : length(0) {
  buffer[0] = '\0';
}

SerialInputStatus SerialCommandBuffer::push(char incoming) {
  if (incoming == '\r') {
    // Accept CRLF line endings without making '\r' part of the command.
    return SerialInputStatus::None;
  }

  if (incoming == '\n') {
    buffer[length] = '\0';
    return SerialInputStatus::LineReady;
  }

  if (length >= SERIAL_COMMAND_BUFFER_SIZE - 1) {
    // Drop the partial command rather than processing a truncated value.
    reset();
    return SerialInputStatus::Overflow;
  }

  buffer[length++] = incoming;
  return SerialInputStatus::None;
}

char *SerialCommandBuffer::command() {
  return buffer;
}

void SerialCommandBuffer::reset() {
  length = 0;
  buffer[0] = '\0';
}
