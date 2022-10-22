using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Power;

namespace Battery_Monitor
{
    public partial class Service1 : ServiceBase
    {
        static SqlConnection con = new SqlConnection("Data Source = INL377; Initial Catalog = battery; Integrated Security = True");
        private static Timer trigger;
        static int previousCharge = 0;
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            trigger = new Timer(batteryReport, null, 0, 15000);
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

                //Store data to SQL Server DB
                if(int.Parse(report.RemainingCapacityInMilliwattHours.ToString()) != previousCharge || report.Status.ToString() == "Idle")
                {
                    storeData(report);
                    previousCharge = int.Parse(report.RemainingCapacityInMilliwattHours.ToString());
                }
            }
        }

        static private void storeData(BatteryReport report)
        {

            con.Open();

            // Stored procedure in SQL Server to store data
            SqlCommand bDI = new SqlCommand("batteryDataInsert", con);
            bDI.CommandType = CommandType.StoredProcedure;
            bDI.Parameters.AddWithValue("@remaining_capacity", SqlDbType.Int).Value = report.RemainingCapacityInMilliwattHours;
            bDI.Parameters.AddWithValue("@full_charge_capacity", SqlDbType.Int).Value = report.FullChargeCapacityInMilliwattHours;
            bDI.Parameters.AddWithValue("@charge_percentage", SqlDbType.Float).Value =
                (((float)report.RemainingCapacityInMilliwattHours / report.FullChargeCapacityInMilliwattHours) * 100);
            bDI.Parameters.AddWithValue("@record_time", SqlDbType.DateTime2).Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            bDI.Parameters.AddWithValue("@design_capacity", SqlDbType.Int).Value = report.DesignCapacityInMilliwattHours;
            bDI.Parameters.AddWithValue("@battery_status", SqlDbType.VarChar).Value = report.Status.ToString();
            bDI.Parameters.AddWithValue("@charge_rate", SqlDbType.Int).Value = report.ChargeRateInMilliwatts;
            bDI.Parameters.AddWithValue("@dc_fc", SqlDbType.Int).Value =
                report.DesignCapacityInMilliwattHours - report.FullChargeCapacityInMilliwattHours;
            bDI.ExecuteNonQuery();

            con.Close();
        }
    }
}
