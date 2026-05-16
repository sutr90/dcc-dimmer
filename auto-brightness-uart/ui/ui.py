import re
import signal
import sys

import serial
from PySide6.QtCore import QObject, Signal, Slot, QThread, Qt
from PySide6.QtCore import QTimer
from PySide6.QtGui import QIcon
from PySide6.QtWidgets import (
    QApplication,
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


class SerialWorker(QObject):
    data_received = Signal(str)
    error_occurred = Signal(str)
    command_requested = Signal(str)

    def __init__(self, port, baudrate=9600):
        super().__init__()
        self.port = port
        self.baudrate = baudrate
        self._running = True
        self._serial = None

    @Slot()
    def run(self):
        """Read serial lines and forward them to the UI thread."""
        try:
            with serial.Serial(self.port, self.baudrate, timeout=1, write_timeout=1) as ser:
                self._serial = ser
                while self._running:
                    line = ser.readline().decode("utf-8", errors="replace").strip()
                    if line:
                        self.data_received.emit(line)
        except Exception as exc:
            self.error_occurred.emit(str(exc))
        finally:
            self._serial = None

    def stop(self):
        self._running = False

    @Slot(str)
    def send_command(self, command):
        if self._serial is None:
            return

        try:
            payload = f"{command}\n".encode("utf-8")
            self._serial.write(payload)
            self._serial.flush()
        except Exception as exc:
            self.error_occurred.emit(str(exc))


class SensorApp(QMainWindow):
    def __init__(self):
        super().__init__()
        self._shutting_down = False
        self._mode = MODE_AUTOMATIC
        self._manual_value = 0
        self.init_ui()
        self.setup_serial()
        self.setup_tray()

    def init_ui(self):
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
        layout.addRow("", self.mode_button)
        layout.addRow("Manual value", self.manual_value_spinbox)
        layout.addRow("", self.manual_apply_button)
        layout.addRow("Last line", self.last_line_label)
        self.setCentralWidget(central)
        self.update_mode_widgets()

    def setup_serial(self):
        self.thread = QThread()
        self.worker = SerialWorker(port="COM3")
        self.worker.moveToThread(self.thread)

        self.thread.started.connect(self.worker.run)
        self.worker.data_received.connect(self.handle_serial_line)
        self.worker.error_occurred.connect(self.handle_serial_error)
        self.worker.command_requested.connect(self.worker.send_command)
        self.mode_button.clicked.connect(self.toggle_mode)
        self.manual_apply_button.clicked.connect(self.apply_manual_value)
        self.thread.start()

    @Slot(str)
    def handle_serial_line(self, line):
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
            return

        match = REPORT_RE.match(line)
        if not match:
            return

        sensor = match.group("sensor")
        display = match.group("display")

        self.sensor_value_label.setText(sensor)
        self.display_value_label.setText(display)

        if self._mode == MODE_MANUAL and display.isdigit():
            self._manual_value = int(display)
            self._set_manual_value(self._manual_value)

    def update_mode_widgets(self):
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

    def _set_manual_value(self, value):
        self.manual_value_spinbox.blockSignals(True)
        self.manual_value_spinbox.setValue(value)
        self.manual_value_spinbox.blockSignals(False)

    @Slot()
    def toggle_mode(self):
        if self._mode == MODE_AUTOMATIC:
            self._manual_value = self.manual_value_spinbox.value()
            self.worker.command_requested.emit(f"MANUAL {self._manual_value}")
        else:
            self.worker.command_requested.emit("AUTO")

    @Slot()
    def apply_manual_value(self):
        if self._mode != MODE_MANUAL:
            return

        self._manual_value = self.manual_value_spinbox.value()
        self.worker.command_requested.emit(f"MANUAL {self._manual_value}")

    @Slot(str)
    def handle_serial_error(self, message):
        self.last_line_label.setText(f"serial error: {message}")

    def setup_tray(self):
        self.tray_icon = QSystemTrayIcon(self)
        self.tray_icon.setIcon(QIcon("brightness-auto.png"))

        tray_menu = QMenu()
        show_action = tray_menu.addAction("Show")
        show_action.triggered.connect(self.showNormal)
        quit_action = tray_menu.addAction("Exit")
        quit_action.triggered.connect(self.actually_exit)

        self.tray_icon.setContextMenu(tray_menu)
        self.tray_icon.show()

    def closeEvent(self, event):
        if not self._shutting_down and self.tray_icon.isVisible():
            self.hide()
            event.ignore()
            return

        self.shutdown()
        event.accept()

    def shutdown(self):
        if self._shutting_down:
            return

        self._shutting_down = True
        try:
            self.worker.command_requested.disconnect(self.worker.send_command)
        except (TypeError, RuntimeError):
            pass
        self.worker.stop()
        self.thread.quit()
        self.thread.wait()

        if hasattr(self, "tray_icon"):
            self.tray_icon.hide()

    def actually_exit(self):
        self.shutdown()
        QApplication.quit()


def main():
    app = QApplication(sys.argv)

    signal.signal(signal.SIGINT, lambda *_: app.quit())

    sigint_timer = QTimer()
    sigint_timer.timeout.connect(lambda: None)
    sigint_timer.start(100)
    app._sigint_timer = sigint_timer

    window = SensorApp()
    app.aboutToQuit.connect(window.shutdown)
    window.show()
    sys.exit(app.exec())


if __name__ == "__main__":
    main()
