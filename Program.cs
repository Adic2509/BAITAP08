using System;
using System.Collections.Generic;

namespace Lab08
{
    // ==========================================
    // 1. ENUMS
    // ==========================================
    public enum DeviceStatus { Available, Borrowed, UnderMaintenance }
    public enum AlertSeverity { Information, Warning, Critical }

    // ==========================================
    // 2. EXCEPTION
    // ==========================================
    public class InvalidDeviceReadingException : Exception
    {
        public string DeviceId { get; }
        public int InvalidValue { get; }
        public InvalidDeviceReadingException(string deviceId, int invalidValue) 
            : base($"Giá trị đọc {invalidValue} của thiết bị {deviceId} không hợp lệ (phải từ 0-100).")
        {
            DeviceId = deviceId;
            InvalidValue = invalidValue;
        }
    }

    // ==========================================
    // 3. SỰ KIỆN (EVENT ARGS)
    // ==========================================
    public class DeviceStatusChangedEventArgs : EventArgs
    {
        public string DeviceId { get; }
        public DeviceStatus OldStatus { get; }
        public DeviceStatus NewStatus { get; }
        public DateTime ChangedAt { get; }

        public DeviceStatusChangedEventArgs(string deviceId, DeviceStatus oldStatus, DeviceStatus newStatus, DateTime changedAt)
        {
            DeviceId = deviceId;
            OldStatus = oldStatus;
            NewStatus = newStatus;
            ChangedAt = changedAt;
        }
    }

    public class DeviceAlertEventArgs : EventArgs
    {
        public string DeviceId { get; }
        public string Message { get; }
        public AlertSeverity Severity { get; }
        public DateTime OccurredAt { get; }

        public DeviceAlertEventArgs(string deviceId, string message, AlertSeverity severity, DateTime occurredAt)
        {
            DeviceId = deviceId;
            Message = message;
            Severity = severity;
            OccurredAt = occurredAt;
        }
    }

    // ==========================================
    // 4. LỚP CƠ SỞ THIẾT BỊ (PUBLISHER)
    // ==========================================
    public abstract class TeachingDevice
    {
        public string Id { get; }
        public string Name { get; }
        public DeviceStatus Status { get; private set; }

        public event EventHandler<DeviceStatusChangedEventArgs>? StatusChanged;
        public event EventHandler<DeviceAlertEventArgs>? AlertRaised;

        protected TeachingDevice(string id, string name)
        {
            Id = id;
            Name = name;
            Status = DeviceStatus.Available;
        }

        public void ChangeStatus(DeviceStatus newStatus)
        {
            if (newStatus == Status) return;

            DeviceStatus oldStatus = Status;
            Status = newStatus;

            OnStatusChanged(new DeviceStatusChangedEventArgs(Id, oldStatus, newStatus, DateTime.Now));
        }

        protected void RaiseAlert(string message, AlertSeverity severity)
        {
            OnAlertRaised(new DeviceAlertEventArgs(Id, message, severity, DateTime.Now));
        }

        protected virtual void OnStatusChanged(DeviceStatusChangedEventArgs e)
        {
            StatusChanged?.Invoke(this, e);
        }

        protected virtual void OnAlertRaised(DeviceAlertEventArgs e)
        {
            AlertRaised?.Invoke(this, e);
        }
    }

    // ==========================================
    // 5. CÁC LOẠI THIẾT BỊ CỤ THỂ
    // ==========================================
    public class ProjectorRemote : TeachingDevice
    {
        public int BatteryLevel { get; private set; } = 100;
        private bool isWarningSent = false;

        public ProjectorRemote(string id, string name) : base(id, name) { }

        public void UpdateBatteryLevel(int newLevel)
        {
            if (newLevel < 0 || newLevel > 100) throw new InvalidDeviceReadingException(Id, newLevel);
            
            BatteryLevel = newLevel;

            if (BatteryLevel == 0)
            {
                RaiseAlert("Pin máy chiếu đã cạn kiệt (0%).", AlertSeverity.Critical);
                isWarningSent = false;
            }
            else if (BatteryLevel < 20 && !isWarningSent)
            {
                RaiseAlert("Pin máy chiếu đang ở mức thấp (dưới 20%).", AlertSeverity.Warning);
                isWarningSent = true; 
            }
            else if (BatteryLevel >= 20)
            {
                isWarningSent = false; 
            }
        }
    }

    public class AirConditionerRemote : TeachingDevice
    {
        public string AssignedRoom { get; }
        public bool IsResponsive { get; private set; } = true;

        public AirConditionerRemote(string id, string name, string room) : base(id, name)
        {
            AssignedRoom = room;
        }

        public void UpdateResponseState(bool isResponsive)
        {
            if (IsResponsive == isResponsive) return; 

            IsResponsive = isResponsive;
            if (!IsResponsive)
            {
                RaiseAlert($"Điều hòa phòng {AssignedRoom} mất kết nối phản hồi.", AlertSeverity.Critical);
            }
        }
    }

    public class WirelessMicrophone : TeachingDevice
    {
        public int BatteryLevel { get; private set; } = 100;
        public int SignalQuality { get; private set; } = 100;
        private bool isBatteryWarningSent = false;
        private bool isSignalWarningSent = false;

        public WirelessMicrophone(string id, string name) : base(id, name) { }

        public void UpdateBatteryLevel(int newLevel)
        {
            if (newLevel < 0 || newLevel > 100) throw new InvalidDeviceReadingException(Id, newLevel);
            BatteryLevel = newLevel;

            if (BatteryLevel == 0)
            {
                RaiseAlert("Pin Micro đã hết (0%).", AlertSeverity.Critical);
                isBatteryWarningSent = false;
            }
            else if (BatteryLevel < 30 && !isBatteryWarningSent)
            {
                RaiseAlert("Pin Micro dưới mức 30%.", AlertSeverity.Warning);
                isBatteryWarningSent = true;
            }
            else if (BatteryLevel >= 30) isBatteryWarningSent = false;
        }

        public void UpdateSignalQuality(int newQuality)
        {
            if (newQuality < 0 || newQuality > 100) throw new InvalidDeviceReadingException(Id, newQuality);
            SignalQuality = newQuality;

            if (SignalQuality == 0)
            {
                RaiseAlert("Mất tín hiệu Micro hoàn toàn (0%).", AlertSeverity.Critical);
                isSignalWarningSent = false;
            }
            else if (SignalQuality < 40 && !isSignalWarningSent)
            {
                RaiseAlert("Tín hiệu Micro yếu (dưới 40%).", AlertSeverity.Warning);
                isSignalWarningSent = true;
            }
            else if (SignalQuality >= 40) isSignalWarningSent = false;
        }
    }

    // ==========================================
    // 6. CÁC SUBSCRIBER (DỊCH VỤ LẮNG NGHE)
    // ==========================================
    public class ConsoleNotificationService
    {
        public void HandleAlert(object? sender, DeviceAlertEventArgs e)
        {
            Console.WriteLine($"[NOTIFICATION] [{e.Severity}] {e.DeviceId}: {e.Message}");
        }
    }

    public class DeviceAuditLogger
    {
        private readonly List<string> entries = new();
        public IReadOnlyList<string> Entries => entries.AsReadOnly();

        public void HandleStatusChanged(object? sender, DeviceStatusChangedEventArgs e)
        {
            string log = $"[AUDIT] {e.ChangedAt:HH:mm:ss} - {e.DeviceId}: {e.OldStatus} -> {e.NewStatus}";
            entries.Add(log);
            Console.WriteLine(log);
        }
    }

    public class MaintenanceService
    {
        private readonly List<string> requests = new();
        public IReadOnlyList<string> Requests => requests.AsReadOnly();

        public void HandleCriticalAlert(object? sender, DeviceAlertEventArgs e)
        {
            if (e.Severity != AlertSeverity.Critical) return;

            string request = $"[MAINTENANCE] Cần bảo trì khẩn cấp {e.DeviceId}: {e.Message}";
            requests.Add(request);
            Console.WriteLine(request);
        }
    }

    // ==========================================
    // 7. DELEGATE (CHÍNH SÁCH LỌC)
    // ==========================================
    public delegate bool AlertFilter(DeviceAlertEventArgs alert);

    public class FilteredAlertSubscriber
    {
        private readonly AlertFilter filter;
        private readonly Action<DeviceAlertEventArgs> action;

        public FilteredAlertSubscriber(AlertFilter filter, Action<DeviceAlertEventArgs> action)
        {
            this.filter = filter;
            this.action = action;
        }

        public void HandleAlert(object? sender, DeviceAlertEventArgs e)
        {
            if (filter(e))
            {
                action(e);
            }
        }
    }

    // ==========================================
    // 8. ĐIỀU PHỐI VIÊN ĐĂNG KÝ (COORDINATOR)
    // ==========================================
    public class DeviceMonitoringCoordinator : IDisposable
    {
        private readonly List<TeachingDevice> devices;
        private readonly ConsoleNotificationService notificationService;
        private readonly DeviceAuditLogger auditLogger;
        private readonly MaintenanceService maintenanceService;

        public DeviceMonitoringCoordinator(
            IEnumerable<TeachingDevice> devices,
            ConsoleNotificationService notificationService,
            DeviceAuditLogger auditLogger,
            MaintenanceService maintenanceService)
        {
            this.devices = new List<TeachingDevice>(devices);
            this.notificationService = notificationService;
            this.auditLogger = auditLogger;
            this.maintenanceService = maintenanceService;
        }

        public void Subscribe()
        {
            foreach (var device in devices)
            {
                // Đăng ký Event thay đổi trạng thái cho Logger
                device.StatusChanged += auditLogger.HandleStatusChanged;

                // Đăng ký Event cảnh báo cho Notification và Maintenance
                device.AlertRaised += notificationService.HandleAlert;
                device.AlertRaised += maintenanceService.HandleCriticalAlert;
            }
        }

        public void Unsubscribe()
        {
            foreach (var device in devices)
            {
                device.StatusChanged -= auditLogger.HandleStatusChanged;
                device.AlertRaised -= notificationService.HandleAlert;
                device.AlertRaised -= maintenanceService.HandleCriticalAlert;
            }
        }

        public void Dispose()
        {
            Unsubscribe();
        }
    }

    // ==========================================
    // 9. CHƯƠNG TRÌNH KIỂM THỬ (MAIN)
    // ==========================================
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("=== BẮT ĐẦU KIỂM THỬ HỆ THỐNG EVENT ===");

            // 1. Tạo các thiết bị
            var prj = new ProjectorRemote("PRJ01", "Máy chiếu 1");
            var ac = new AirConditionerRemote("AC01", "Điều hòa 1", "Phòng 101");
            var mic = new WirelessMicrophone("MIC01", "Micro 1");

            var devices = new List<TeachingDevice> { prj, ac, mic };

            // 2. Tạo các Subscriber
            var notifier = new ConsoleNotificationService();
            var logger = new DeviceAuditLogger();
            var maintenance = new MaintenanceService();

            // 3. Dùng Coordinator để đăng ký hàng loạt
            using (var coordinator = new DeviceMonitoringCoordinator(devices, notifier, logger, maintenance))
            {
                coordinator.Subscribe();

                // TEST 1: Thay đổi trạng thái
                Console.WriteLine("\n[TEST 1] Thay đổi trạng thái thiết bị:");
                prj.ChangeStatus(DeviceStatus.Borrowed);
                mic.ChangeStatus(DeviceStatus.UnderMaintenance);
                prj.ChangeStatus(DeviceStatus.Borrowed); // Không kích hoạt vì trạng thái không đổi

                // TEST 2: Cảnh báo Warning
                Console.WriteLine("\n[TEST 2] Kích hoạt cảnh báo Warning:");
                prj.UpdateBatteryLevel(15); 
                prj.UpdateBatteryLevel(10); // Không gửi lặp lại thông báo Warning

                mic.UpdateSignalQuality(35);

                // TEST 3: Cảnh báo Critical
                Console.WriteLine("\n[TEST 3] Kích hoạt cảnh báo Critical (Gọi cả Notifier & Maintenance):");
                ac.UpdateResponseState(false);
                mic.UpdateBatteryLevel(0);

                // TEST 4: Delegate Custom Filter (Lọc riêng thông tin)
                Console.WriteLine("\n[TEST 4] Kiểm thử Delegate Filter:");
                var criticalOnly = new FilteredAlertSubscriber(
                    alert => alert.Severity == AlertSeverity.Critical,
                    alert => Console.WriteLine($"=> CUSTOM FILTER CHỚP NHOÁNG: Thiết bị {alert.DeviceId} ĐANG GẶP SỰ CỐ!")
                );
                
                // Đăng ký cục bộ
                prj.AlertRaised += criticalOnly.HandleAlert;
                prj.UpdateBatteryLevel(0); // Sẽ in ra 3 dòng: Notifier, Maintenance, và Custom Filter

                // Dispose() của Coordinator sẽ tự động Unsubscribe.
            }

            // TEST 5: Ngoại lệ
            Console.WriteLine("\n[TEST 5] Kiểm tra ném ngoại lệ InvalidDeviceReadingException:");
            try
            {
                mic.UpdateBatteryLevel(150);
            }
            catch (InvalidDeviceReadingException ex)
            {
                Console.WriteLine($"BẮT LỖI THÀNH CÔNG: {ex.Message}");
            }
        }
    }
}