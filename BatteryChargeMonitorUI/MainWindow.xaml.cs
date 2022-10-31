using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Data.SQLite;
using System.Data;

namespace BatteryChargeMonitorUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary> 
    public partial class MainWindow : Window
    {
        private static SQLiteConnection con = new SQLiteConnection("Data Source = C:\\Windows\\SysWOW64\\battery.sqlite; Version = 3");
        public MainWindow()
        {
            InitializeComponent();
        }

        private void DisplayData(object sender, RoutedEventArgs e)
        {
            con.Open();

            SQLiteCommand getData = new SQLiteCommand("SELECT * FROM batteryData", con);
            SQLiteDataReader batteryDataReader = getData.ExecuteReader();

            DataTable displayTable = new DataTable();
            displayTable.Columns.Add("Date", typeof(String));
            displayTable.Columns.Add("Hour", typeof(int));
            displayTable.Columns.Add("Discharge", typeof(int));
            displayTable.Columns.Add("Duration", typeof(int));

            int spotCount = 0;
            int optimalCount = 0;
            int badCount = 0;

            if (batteryDataReader.HasRows)
            {
                

                batteryDataReader.Read();
                DateTime currentRecordTime = DateTime.Parse(batteryDataReader.GetString(0));
                int currentHour = currentRecordTime.Hour;
                String currentStatus = batteryDataReader.GetString(1);
                int currentLevel = batteryDataReader.GetInt32(2);

                DateTime previousRecordTime = currentRecordTime;
                String previousStatus = currentStatus;
                int previousLevel = currentLevel;

                int discharge = 0;
                int duration = 0;

                DateTime idleTimeStart = currentRecordTime;
                DateTime idleTimeEnd;

                while(batteryDataReader.Read())
                {
                    currentRecordTime = DateTime.Parse(batteryDataReader.GetString(0));
                    currentStatus = batteryDataReader.GetString(1);
                    currentLevel = batteryDataReader.GetInt32(2);

                    if(previousStatus != currentStatus)
                    {
                        if (previousStatus == "Charging" && currentStatus == "Discharging")
                            spotCount++;
                        else if(previousStatus == "Charging" && currentStatus == "Idle")
                            idleTimeStart = currentRecordTime;
                        else if(previousStatus == "Idle" && currentStatus == "Discharging")
                        {
                            idleTimeEnd = currentRecordTime;
                            if ((idleTimeEnd - idleTimeStart).TotalMinutes > 30)
                                badCount++;
                            else
                                optimalCount++;
                        }
                    }

                    if (currentRecordTime.Hour == currentHour)
                    {
                        if(currentStatus == "Discharging")
                        {
                            duration += (int)(currentRecordTime - previousRecordTime).TotalMinutes;
                            discharge += previousLevel - currentLevel;
                        }
                    }
                    else 
                    {
                        if(currentStatus == "Discharging" && (currentRecordTime - previousRecordTime).TotalMinutes <= 1)
                        {
                            duration += 1;
                        }

                        displayTable.Rows.Add(currentRecordTime.Date.ToString("dd-MM-yyyy"), currentHour, discharge, duration);

                        duration = 0;
                        discharge = 0;
                        currentHour = currentRecordTime.Hour;

                    }

                    previousLevel = currentLevel;
                    previousRecordTime = currentRecordTime;
                    previousStatus = currentStatus;
                    
                }

                displayTable.Rows.Add(currentRecordTime.ToString("dd-MM-yyyy"), currentHour, discharge, duration);

                if (currentStatus == "Idle" && (currentRecordTime - idleTimeStart).TotalMinutes > 30)
                    badCount++;


                TextBlockCount.Text = "Spot count = " + spotCount.ToString() + "\n" +
                    "Optimal count = " + optimalCount.ToString() + "\n" +
                    "Bad count = " + badCount.ToString();

                datagridDisplay.ItemsSource = displayTable.DefaultView;
                
            }
            
            
            con.Close();

        }
    }
}
