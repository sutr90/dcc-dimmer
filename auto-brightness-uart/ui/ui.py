import serial
from PySide6.QtCore import QObject, Signal, Slot
from PySide6.QtWidgets import QApplication, QMainWindow, QSystemTrayIcon, QMenu, QLabel
from PySide6.QtCore import QThread, Qt
from PySide6.QtGui import QIcon

class SerialWorker(QObject):
    # This is our cross-thread messenger
    data_received = Signal(str)
    error_occurred = Signal(str)

    def __init__(self, port, baudrate=9600):
        super().__init__()
        self.port = port
        self.baudrate = baudrate
        self._running = True

    @Slot()
    def run(self):
        """The infinite loop that runs in the background thread."""
        try:
            with serial.Serial(self.port, self.baudrate, timeout=1) as ser:
                while self._running:
                    if ser.in_waiting > 0:
                        line = ser.readline().decode('utf-8').strip()
                        # Send the data to the UI thread safely
                        self.data_received.emit(line)
        except Exception as e:
            self.error_occurred.emit(str(e))

    def stop(self):
        self._running = False
        
class SensorApp(QMainWindow):
    def __init__(self):
        super().__init__()
        self.init_ui()
        self.setup_serial()
        self.setup_tray()
        
    def init_ui(self):
        self.setWindowTitle("test")
        label = QLabel("label")
        label.setAlignment(Qt.AlignCenter)
        self.setCentralWidget(label)

    def setup_serial(self):
        # 1. Create the thread and the worker
        self.thread = QThread()
        self.worker = SerialWorker(port="COM3") # Use /dev/ttyUSB0 for Linux

        # 2. Move worker to the thread
        self.worker.moveToThread(self.thread)

        # 3. Connect signals and slots
        self.thread.started.connect(self.worker.run)
        self.worker.data_received.connect(self.update_label)
        
        # 4. Start the engine
        self.thread.start()

    def update_label(self, value):
        # This function is executed in the UI THREAD
        self.my_label.setText(f"Sensor Value: {value}")
        
    def setup_tray(self):
        self.tray_icon = QSystemTrayIcon(self)
        self.tray_icon.setIcon(QIcon("brightness-auto.png")) # Use a real .ico or .png
        
        # Create a menu for the tray
        tray_menu = QMenu()
        show_action = tray_menu.addAction("Show")
        show_action.triggered.connect(self.showNormal)
        quit_action = tray_menu.addAction("Exit")
        quit_action.triggered.connect(self.actually_exit)
        
        self.tray_icon.setContextMenu(tray_menu)
        self.tray_icon.show()

    def closeEvent(self, event):
        """Intercept the close button to minimize to tray."""
        if self.tray_icon.isVisible():
            self.hide()
            event.ignore() # Prevent the app from actually closing

    def actually_exit(self):
        self.worker.stop()
        self.thread.quit()
        self.thread.wait()
        exit()


app = QApplication()
window = SensorApp();

app.exec()
