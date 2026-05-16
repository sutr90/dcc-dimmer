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
    QSystemTrayIcon,
    QWidget,
)


REPORT_RE = re.compile(r"^sensor=(?P<sensor>[^,]+),display=(?P<display>.+)$")


class SerialWorker(QObject):
    data_received = Signal(str)
    error_occurred = Signal(str)

    def __init__(self, port, baudrate=9600):
        super().__init__()
        self.port = port
        self.baudrate = baudrate
        self._running = True

    @Slot()
    def run(self):
        """Read serial lines and forward them to the UI thread."""
        try:
            with serial.Serial(self.port, self.baudrate, timeout=1) as ser:
                while self._running:
                    line = ser.readline().decode("utf-8", errors="replace").strip()
                    if line:
                        self.data_received.emit(line)
        except Exception as exc:
            self.error_occurred.emit(str(exc))

    def stop(self):
        self._running = False


class SensorApp(QMainWindow):
    def __init__(self):
        super().__init__()
        self._shutting_down = False
        self.init_ui()
        self.setup_serial()
        self.setup_tray()

    def init_ui(self):
        self.setWindowTitle("Auto Brightness UART")

        central = QWidget(self)
        layout = QFormLayout(central)

        self.sensor_value_label = QLabel("unknown")
        self.display_value_label = QLabel("unknown")
        self.last_line_label = QLabel("waiting for serial data")

        for label in (self.sensor_value_label, self.display_value_label, self.last_line_label):
            label.setTextInteractionFlags(Qt.TextSelectableByMouse)

        layout.addRow("Sensor", self.sensor_value_label)
        layout.addRow("Display", self.display_value_label)
        layout.addRow("Last line", self.last_line_label)
        self.setCentralWidget(central)

    def setup_serial(self):
        self.thread = QThread()
        self.worker = SerialWorker(port="COM3")
        self.worker.moveToThread(self.thread)

        self.thread.started.connect(self.worker.run)
        self.worker.data_received.connect(self.handle_serial_line)
        self.worker.error_occurred.connect(self.handle_serial_error)
        self.thread.start()

    @Slot(str)
    def handle_serial_line(self, line):
        self.last_line_label.setText(line)

        match = REPORT_RE.match(line)
        if not match:
            return

        sensor = match.group("sensor")
        display = match.group("display")

        self.sensor_value_label.setText(sensor)
        self.display_value_label.setText(display)

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
