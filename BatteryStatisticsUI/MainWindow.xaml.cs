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
using System.Data;
using System.Data.SqlClient;
using System.Globalization;

namespace BatteryStatisticsUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    
    public partial class MainWindow : Window
    {
        static SqlConnection con = new SqlConnection("Data Source = INL377; Initial Catalog = battery; Integrated Security = True");
        public MainWindow()
        {
            InitializeComponent();
            textGeneral.Text = "";
            textTitle.Text = "";
            datagridGeneral.IsEnabled = false;
        }


        

        private void ChargeCycle(object sender, RoutedEventArgs e)
        {
            
            textGeneral.Text = "";
            textTitle.Text = "";
            con.Open();

            SqlDataAdapter hc = new SqlDataAdapter("exec hourlyCharge", con);
            DataTable dt = new DataTable();
            hc.Fill(dt);
            datagridGeneral.ItemsSource = dt.DefaultView;

            con.Close();
        }

        private void BatteryData(object sender, RoutedEventArgs e)
        {
            
            textGeneral.Text = "";
            textTitle.Text = "";
            con.Open();
            SqlDataAdapter bd = new SqlDataAdapter("select * from batteryData order by RecordTime desc", con);
            DataTable dt = new DataTable();
            bd.Fill(dt);
            datagridGeneral.ItemsSource = dt.DefaultView;
            
            
            con.Close();
        }

        

        private void CurrentState(object sender, RoutedEventArgs e)
        {
            textGeneral.Text = "";
            textTitle.Text = "";
            con.Open();
            SqlCommand cs = new SqlCommand("select top 1 * from batteryData order by RecordTime desc", con);
            SqlDataReader r = cs.ExecuteReader();
            

            if(r.HasRows)
            {
                while(r.Read())
                {
                    textGeneral.Text += "Battery Status: " + r["BatteryStatus"] + "\n";
                    textGeneral.Text += "Remaining Capacity: " + r["ChargePercentage"] + "%\t("
                        + r["RemainingCapacity"] + " mWh)\n";
                    textGeneral.Text += "Full Charge Capacity: " + r["FullChargeCapacity"] + " mWh\n";
                    textGeneral.Text += "Design Capacity: " + r["DesignCapacity"] + " mWh\n";
                    textGeneral.Text += "Charge/Discharge rate: " + r["ChargeRate"] + " mW\n";
                    textGeneral.Text += "Design Capacity - Full Charge Capacity difference: " + r["DC_FC_difference"] + " mWh";
                }
            }
            textTitle.Text = "Current State";
            con.Close();
        }

        private void ChargingPattern(object sender, RoutedEventArgs e)
        {
            textGeneral.Text = "";
            textTitle.Text = "";
            con.Open();

            SqlDataAdapter cp = new SqlDataAdapter("select RecordTime, BatteryStatus, ChargePercentage from batteryData order by RecordTime asc", con);

            DataTable dt = new DataTable();
            cp.Fill(dt);

            int BadCount = 0, OptimalCount = 0, SpotCount = 0;
            bool charging = false, idle = false, overcharge = false; ;
            DateTime start_time = DateTime.MinValue, current_time;

            foreach (DataRow r in dt.Rows)
            {
                
                if (r["BatteryStatus"].ToString() == "Discharging")
                {
                    if(!charging)
                    {
                        if (idle)
                        {
                            if (overcharge)
                            {
                                overcharge = false;
                            }
                            else
                            {
                                OptimalCount += 1;
                            }
                            idle = false;   
                        }
                        continue;
                    }
                    else
                    {
                        SpotCount += 1;
                        charging = false;
                    }
                }
                else if(r["BatteryStatus"].ToString() == "Charging")
                {
                    if (charging)
                        continue;
                    else
                    {
                        charging = true;
                        //start_time = current_time;
                    }         
                }
                else if(r["BatteryStatus"].ToString() == "Idle")
                {
                    current_time = DateTime.Parse(r["RecordTime"].ToString());
                    charging = false;
                    if(idle)
                    {
                        if (overcharge)
                            continue;
                        else
                        {
                            if ((current_time - start_time).TotalMinutes > 30)
                            {    
                                    BadCount += 1;
                                    overcharge = true;
                            }
                        }
                    }
                    else
                    {
                        start_time = DateTime.Parse(r["RecordTime"].ToString());
                        idle = true;
                    }
                    

                }
            }

            //MessageBoxResult result = MessageBox.Show("SpotCount: " + SpotCount.ToString());
            //result = MessageBox.Show("Optimal Count: " + OptimalCount.ToString());
            //result = MessageBox.Show("Bad Count: " + BadCount.ToString());
            textTitle.Text = "Charge Pattern";

            textGeneral.Text += "Spot Count: " + SpotCount.ToString() + "\n";
            textGeneral.Text += "Optimal Count: " + OptimalCount.ToString() + "\n";
            textGeneral.Text += "Bad Count: " + BadCount.ToString();
            con.Close();
        }

        private void datagridGeneral_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
