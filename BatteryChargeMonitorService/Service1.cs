using System;
using System.ServiceProcess;
using System.Threading;
using System.Data.SQLite;
using Windows.Devices.Enumeration;
using Windows.Devices.Power;

namespace BatteryChargeMonitorService
{
    public partial class Service1 : ServiceBase
    {
        private static SQLiteConnection con = new SQLiteConnection("Data Source = battery.sqlite; Version = 3");
        private static Timer trigger;

        public Service1()
        {
            InitializeComponent();

            SQLiteConnection.CreateFile("battery.sqlite");

            con.Open();

            SQLiteCommand createTable = new SQLiteCommand("CREATE TABLE batteryData(RecordTime TEXT PRIMARY KEY, BatteryStatus TEXT, BatteryPercentage INT)", con);
            createTable.ExecuteNonQuery();

            con.Close();
        }

        protected override void OnStart(string[] args)
        {
            trigger = new Timer(batteryReport, null, 0, 60000);
        }

        protected override void OnStop()
        {
        }

        static async private void batteryReport(object state)
        {
            // Find batteries 
            var deviceInfo = await DeviceInformation.FindAllAsync(Battery.GetDeviceSelector());
            foreach (DeviceInformation device in deviceInfo)
            {
                // Create battery object
                var battery = await Battery.FromIdAsync(device.Id);

                // Get report
                var report = battery.GetReport();

                //Store data to SQLite DB
                storeData(report);
                
            }
        }

        static private void storeData(BatteryReport report)
        {

            con.Open();

            var batteryPercetage = (int) Math.Round(((float)report.RemainingCapacityInMilliwattHours / (float)report.FullChargeCapacityInMilliwattHours) * 100);

            // Storing data in SQLite DB
            SQLiteCommand insertData = new SQLiteCommand("INSERT INTO batteryData VALUES(" +
                "'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "', " +
                "'" + report.Status.ToString() + "', " +
                batteryPercetage.ToString() + ")",
                con);
            insertData.ExecuteNonQuery();
            

            con.Close();
        }
    }
}
