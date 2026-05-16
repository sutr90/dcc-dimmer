import re
import json
import argparse
import signal
import sys
import queue
import threading
from pathlib import Path

import serial
from PySide6.QtCore import QObject, Signal, Slot, QThread, Qt
from PySide6.QtCore import QTimer
from PySide6.QtGui import QIcon
from PySide6.QtWidgets import (
    QApplication,
    QHBoxLayout,
    QFormLayout,
    QLabel,
    QMainWindow,
    QMenu,
    QPushButton,
    QSystemTrayIcon,
    QSpinBox,
    QWidget,
)


REPORT_RE = re.compile(r"^sensor=(?P<sensor>[^,]+),display=(?P<display>.+)$")
MODE_RE = re.compile(r"^mode=(?P<mode>automatic|manual)(?: value=(?P<value>\d+))?$")

MODE_AUTOMATIC = "automatic"
MODE_MANUAL = "manual"
UI_DIR = Path(__file__).resolve().parent
CONFIG_PATH = UI_DIR / "config.json"
DEFAULT_SERIAL_CONFIG = {
    "port": "COM3",
    "baudrate": 9600,
    "timeout": 1,
    "write_timeout": 1,
    "debug": False,
}

DEBUG_ENABLED = False


def debug_print(*args, **kwargs):
    if not DEBUG_ENABLED:
        return

    print(*args, **kwargs)


def load_serial_config():
    config = DEFAULT_SERIAL_CONFIG.copy()

    try:
        with CONFIG_PATH.open("r", encoding="utf-8") as handle:
            loaded = json.load(handle)
    except FileNotFoundError:
        return config
    except Exception:
        return config

    if not isinstance(loaded, dict):
        return config

    for key in ("port", "baudrate", "timeout", "write_timeout", "debug"):
        if key in loaded:
            config[key] = loaded[key]

    return config


class SerialWorker(QObject):
    data_received = Signal(str)
    error_occurred = Signal(str)

    def __init__(self, port, baudrate=9600, timeout=1, write_timeout=1):
        super().__init__()
        self.port = port
        self.baudrate = baudrate
        self.timeout = timeout
        self.write_timeout = write_timeout
        self._running = True
        self._serial = None
        self._command_queue = queue.Queue()
        self._serial_lock = threading.Lock()

    @Slot()
    def run(self):
        """Read serial lines and forward them to the UI thread."""
        debug_print(
            "SerialWorker.run:",
            f"opening {self.port} at {self.baudrate} baud",
            f"timeout={self.timeout}",
            f"write_timeout={self.write_timeout}",
        )
        try:
            with serial.Serial(
                self.port,
                self.baudrate,
                timeout=self.timeout,
                write_timeout=self.write_timeout,
            ) as ser:
                with self._serial_lock:
                    self._serial = ser
                debug_print("SerialWorker.run: serial port opened")
                while self._running:
                    self._drain_pending_commands()
                    line = ser.readline().decode("utf-8", errors="replace").strip()
                    if line:
                        debug_print("SerialWorker.run: received", repr(line))
                        self.data_received.emit(line)
        except Exception as exc:
            debug_print("SerialWorker.run: error", exc)
            self.error_occurred.emit(str(exc))
        finally:
            with self._serial_lock:
                self._serial = None
            debug_print("SerialWorker.run: exiting")

    def stop(self):
        debug_print("SerialWorker.stop: requested")
        self._running = False

    def queue_command(self, command):
        debug_print("SerialWorker.queue_command:", repr(command))
        self._command_queue.put(command)

        with self._serial_lock:
            serial_port = self._serial

        if serial_port is not None and hasattr(serial_port, "cancel_read"):
            try:
                serial_port.cancel_read()
                debug_print("SerialWorker.queue_command: canceled blocking read")
            except Exception as exc:
                debug_print("SerialWorker.queue_command: cancel_read failed", exc)

    def _drain_pending_commands(self):
        with self._serial_lock:
            serial_port = self._serial

        if serial_port is None:
            return

        while True:
            try:
                command = self._command_queue.get_nowait()
            except queue.Empty:
                return

            try:
                debug_print("SerialWorker._drain_pending_commands: sending", repr(command))
                payload = f"{command}\n".encode("utf-8")
                serial_port.write(payload)
                serial_port.flush()
                debug_print("SerialWorker._drain_pending_commands: sent", repr(command))
            except Exception as exc:
                debug_print("SerialWorker._drain_pending_commands: error", exc)
                self.error_occurred.emit(str(exc))
                return


class SensorApp(QMainWindow):
    def __init__(self, serial_config):
        super().__init__()
        self._shutting_down = False
        self._mode = MODE_AUTOMATIC
        self._manual_value = 0
        self._display_value = None
        self._serial_config = serial_config
        debug_print("SensorApp.__init__:", self._serial_config)
        self.init_ui()
        self.setup_serial()
        self.setup_tray()

    def init_ui(self):
        debug_print("SensorApp.init_ui")
        self.setWindowTitle("Auto Brightness UART")

        central = QWidget(self)
        layout = QFormLayout(central)

        self.sensor_value_label = QLabel("unknown")
        self.display_value_label = QLabel("unknown")
        self.mode_value_label = QLabel("automatic")
        self.mode_button = QPushButton("Switch to Manual")
        self.manual_value_spinbox = QSpinBox()
        self.manual_value_spinbox.setRange(0, 100)
        self.manual_value_spinbox.setValue(0)
        self.manual_value_spinbox.lineEdit().returnPressed.connect(self.apply_manual_value)
        self.manual_apply_button = QPushButton("Apply Manual Value")
        self.last_line_label = QLabel("waiting for serial data")

        for label in (
            self.sensor_value_label,
            self.display_value_label,
            self.mode_value_label,
            self.last_line_label,
        ):
            label.setTextInteractionFlags(Qt.TextSelectableByMouse)

        layout.addRow("Sensor", self.sensor_value_label)
        layout.addRow("Display", self.display_value_label)
        layout.addRow("Mode", self.mode_value_label)
        layout.addRow(self.mode_button)
        manual_row = QHBoxLayout()
        manual_row.addWidget(self.manual_value_spinbox)
        manual_row.addWidget(self.manual_apply_button)
        layout.addRow(manual_row)
        if DEBUG_ENABLED:
            layout.addRow("Last line", self.last_line_label)
        self.setCentralWidget(central)
        self.update_mode_widgets()
        self.update_tray_icon()
        self.adjustSize()
        self.setFixedSize(self.size())
        debug_print("SensorApp.init_ui: fixed size", self.size())

    def setup_serial(self):
        debug_print("SensorApp.setup_serial")
        self.thread = QThread()
        self.worker = SerialWorker(
            port=self._serial_config["port"],
            baudrate=self._serial_config["baudrate"],
            timeout=self._serial_config["timeout"],
            write_timeout=self._serial_config["write_timeout"],
        )
        self.worker.moveToThread(self.thread)

        self.thread.started.connect(self.worker.run)
        self.worker.data_received.connect(self.handle_serial_line)
        self.worker.error_occurred.connect(self.handle_serial_error)
        self.mode_button.clicked.connect(self.toggle_mode)
        self.manual_apply_button.clicked.connect(self.apply_manual_value)
        self.thread.start()
        debug_print("SensorApp.setup_serial: worker thread started")

    @Slot(str)
    def handle_serial_line(self, line):
        debug_print("SensorApp.handle_serial_line:", repr(line))
        self.last_line_label.setText(line)

        mode_match = MODE_RE.match(line)
        if mode_match:
            mode = mode_match.group("mode")
            self._mode = mode
            value = mode_match.group("value")
            if value is not None:
                self._manual_value = int(value)
                self._set_manual_value(self._manual_value)
            self.update_mode_widgets()
            debug_print("SensorApp.handle_serial_line: mode updated", self._mode, self._manual_value)
            return

        match = REPORT_RE.match(line)
        if not match:
            return

        sensor = match.group("sensor")
        display = match.group("display")

        self.sensor_value_label.setText(sensor)
        self.display_value_label.setText(display)
        self._display_value = None if display == "unknown" else int(display)
        self.update_tray_icon()
        debug_print("SensorApp.handle_serial_line: sensor/display updated", sensor, display)

    def update_mode_widgets(self):
        debug_print("SensorApp.update_mode_widgets:", self._mode, self._manual_value)
        if self._mode == MODE_AUTOMATIC:
            self.mode_value_label.setText("automatic")
            self.manual_value_spinbox.setEnabled(False)
            self.manual_apply_button.setEnabled(False)
        else:
            self.mode_value_label.setText(f"manual ({self._manual_value})")
            self.manual_value_spinbox.setEnabled(True)
            self.manual_apply_button.setEnabled(True)
        self.mode_button.setText(
            "Switch to Manual" if self._mode == MODE_AUTOMATIC else "Switch to Automatic"
        )

    def update_tray_icon(self):
        if not hasattr(self, "tray_icon"):
            return

        icon_name = self._get_display_icon_name()
        self.tray_icon.setIcon(QIcon(str(UI_DIR / icon_name)))
        debug_print("SensorApp.update_tray_icon:", icon_name)

    def _get_display_icon_name(self):
        if self._display_value is None:
            return "brightness-unknown.png"

        value = max(0, min(100, self._display_value))
        if value < 20:
            return "brightness0.png"
        if value < 40:
            return "brightness1.png"
        if value < 60:
            return "brightness2.png"
        if value < 80:
            return "brightness3.png"
        return "brightness4.png"

    def _set_manual_value(self, value):
        debug_print("SensorApp._set_manual_value:", value)
        self.manual_value_spinbox.blockSignals(True)
        self.manual_value_spinbox.setValue(value)
        self.manual_value_spinbox.blockSignals(False)

    @Slot()
    def toggle_mode(self):
        debug_print("SensorApp.toggle_mode:", self._mode)
        if self._mode == MODE_AUTOMATIC:
            self._manual_value = self.manual_value_spinbox.value()
            command = f"MANUAL {self._manual_value}"
            self.worker.queue_command(command)
            debug_print("SensorApp.sending command:", command)
        else:
            debug_print("SensorApp.toggle_mode:", self._mode)
            self.worker.queue_command("AUTO")
            debug_print("SensorApp.sending command:", "AUTO")

    @Slot()
    def apply_manual_value(self):
        debug_print("SensorApp.apply_manual_value:", self._mode)
        if self._mode != MODE_MANUAL:
            return

        self._manual_value = self.manual_value_spinbox.value()
        self.worker.queue_command(f"MANUAL {self._manual_value}")

    @Slot(str)
    def handle_serial_error(self, message):
        debug_print("SensorApp.handle_serial_error:", message)
        self.last_line_label.setText(f"serial error: {message}")

    def setup_tray(self):
        debug_print("SensorApp.setup_tray")
        self.tray_icon = QSystemTrayIcon(self)
        self.update_tray_icon()
        self.tray_icon.activated.connect(self.handle_tray_activated)

        tray_menu = QMenu()
        show_action = tray_menu.addAction("Show")
        show_action.triggered.connect(self.showNormal)
        quit_action = tray_menu.addAction("Exit")
        quit_action.triggered.connect(self.actually_exit)

        self.tray_icon.setContextMenu(tray_menu)
        self.tray_icon.show()
        debug_print("SensorApp.setup_tray: tray shown")

    @Slot(QSystemTrayIcon.ActivationReason)
    def handle_tray_activated(self, reason):
        debug_print("SensorApp.handle_tray_activated:", reason)
        if reason != QSystemTrayIcon.ActivationReason.DoubleClick:
            return

        if self.isVisible():
            self.hide()
        else:
            self.showNormal()
            self.raise_()
            self.activateWindow()

    def closeEvent(self, event):
        debug_print("SensorApp.closeEvent")
        if not self._shutting_down and self.tray_icon.isVisible():
            self.hide()
            event.ignore()
            debug_print("SensorApp.closeEvent: hidden to tray")
            return

        self.shutdown()
        event.accept()

    def shutdown(self):
        if self._shutting_down:
            debug_print("SensorApp.shutdown: already shutting down")
            return

        debug_print("SensorApp.shutdown: starting")
        self._shutting_down = True
        self.worker.stop()
        self.thread.quit()
        self.thread.wait()

        if hasattr(self, "tray_icon"):
            self.tray_icon.hide()
        debug_print("SensorApp.shutdown: complete")

    def actually_exit(self):
        debug_print("SensorApp.actually_exit")
        self.shutdown()
        QApplication.quit()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--debug", action="store_true", help="enable debug logging")
    args, _unknown_args = parser.parse_known_args()

    app = QApplication(sys.argv)

    signal.signal(signal.SIGINT, lambda *_: app.quit())

    serial_config = load_serial_config()

    global DEBUG_ENABLED
    DEBUG_ENABLED = bool(serial_config.get("debug")) or args.debug
    debug_print("main: debug enabled", DEBUG_ENABLED)
    debug_print("main: loaded serial config", serial_config)

    sigint_timer = QTimer()
    sigint_timer.timeout.connect(lambda: None)
    sigint_timer.start(100)
    app._sigint_timer = sigint_timer

    window = SensorApp(serial_config)
    app.aboutToQuit.connect(window.shutdown)
    window.show()
    debug_print("main: window shown")
    sys.exit(app.exec())


if __name__ == "__main__":
    main()
