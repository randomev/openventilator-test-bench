using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace open_ventilator_tester
{
    public partial class Form1 : Form
    {

        private static Mutex mutLog = new Mutex();
        private static Mutex mut = new Mutex();

        BindingList<Cyclepoint> cyclepoints = new BindingList<Cyclepoint>();
        bool bTestRunning = false;
        int currentStepNumber = -1;
        Cyclepoint currentStep = new Cyclepoint();
        string logfilename = "";

        DataTable dtLogData = new DataTable();
        DateTime dt0 = DateTime.MinValue;  // any date will do, just know which you use!


        double last_motorcurrent = 0;
        double last_motorvoltage = 0;
        double last_motortemp = 0;
        double last_flow = 0;
        double last_pressure = 0;
        double last_rpm = 0.0;
        double last_rpm_setpoint = 0.0;

        List<List<DataPoint>> datapoints = new List<List<DataPoint>>(); // = { new List<DataPoint>(), new List<DataPoint>(), new List<DataPoint>(), new List<DataPoint>(), new List<DataPoint>() };
        List<DataField> datanames = new List<DataField>();

        const int DATA_TIME = 0;
        const int DATA_MOTOR_RPM = 1;
        const int DATA_MOTOR_RPM_SETPOINT = 2;
        const int DATA_FLOW = 3;
        const int DATA_MOTORVOLTAGE = 4;
        const int DATA_MOTORCURRENT = 5;

        const int DATA_MAX = 5;

        public Form1()
        {
            InitializeComponent();
        }

        static private double addPointsToGraph(System.Windows.Forms.DataVisualization.Charting.Series series, List<DataPoint> points)
        {
            double avg = 0;
            avg = 0;
            if (points.Count == 0)
                return 0;

            foreach (DataPoint dp in points)
            {
                DateTime datepoint = new DateTime(dp.x);
                
                series.Points.AddXY(datepoint.ToOADate(), dp.y);
                //series.Points.AddXY(dp.x, dp.y);
                avg = avg + dp.y;
            }
            avg = avg / points.Count;
            return avg;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Uni_T_Devices.UT372 rpm_meter = new Uni_T_Devices.UT372();

            rpm_meter.openUSB();

            //rpm_meter.dumpUSBData();

            //MessageBox.Show(rpm_meter.parseSerialInputToRPM("070?<3=7<60655>607;007885").ToString());
            //Console.WriteLine(rpm_meter.parseSerialInputToRPM("07;7;7;7;7;655>607;007885").ToString());
            //Console.WriteLine(rpm_meter.parseSerialInputToRPM("0607;7;7;7;655>607;007885").ToString());

            dataGridView1.DataSource = cyclepoints;
            dataGridView1.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);

            dataGridView2.Rows.Clear();

            datanames.Add(new DataField("Time", 1));
            datanames.Add(new DataField("MRPM", 2));
            datanames.Add(new DataField("MRPM Set", 3));
            datanames.Add(new DataField("Flow", 4));
            datanames.Add(new DataField("MVoltage",5));
            datanames.Add(new DataField("MCurrent", 6));

            foreach (DataField df in datanames)
            {
                dtLogData.Columns.Add(df.Name);
                datapoints.Add(new List<DataPoint>());

                chart1.Series.Add(df.Name);
                chart1.Series.Last().ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
                chart1.Series.Last().XValueType = System.Windows.Forms.DataVisualization.Charting.ChartValueType.Time;
            }
            dt0 = DateTime.Now;  // any date will do, just know which you use!
            chart1.ChartAreas[0].AxisX.LabelStyle.Format = "HH:mm:ss";

        }

        private void btnOpenTestcycle_Click(object sender, EventArgs e)
        {
            try
            {
                XMLHandler x = new XMLHandler();

                OpenFileDialog o = new OpenFileDialog();
                o.ShowDialog();
                if (o.FileName != "")
                { 
                    cyclepoints = x.DeSerializeCyclepointsFromXML(o.FileName);
                    dataGridView1.DataSource = cyclepoints;
                }

            }
            catch (Exception ex)
            {

            }
        }

        private void btnSaveParameters_Click(object sender, EventArgs e)
        {
            try
            {
                XMLHandler x = new XMLHandler();

                SaveFileDialog o = new SaveFileDialog();
                o.ShowDialog();
                if (o.FileName!="")
                    x.SerializeCyclepoints2XML(cyclepoints, o.FileName);

            }
            catch (Exception ex)
            {

            }
        }

        void UpdateGraphAndUI()
        {

            mut.WaitOne();

            string timenow = DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss.fff");

            if (datapoints[DATA_MOTOR_RPM_SETPOINT].Count > 0)
            {
                last_motorcurrent = addPointsToGraph(chart1.Series[DATA_MOTOR_RPM_SETPOINT], datapoints[DATA_MOTOR_RPM_SETPOINT]);
            }

            mutLog.WaitOne();

            DataRow r = dtLogData.NewRow();
            r["Time"] = timenow;

            for (int i = 1;i<DATA_MAX;i++)
            {
                r[datanames[i].Index] = datapoints[i];
                datapoints[i] = new List<DataPoint>();
            }

            dtLogData.Rows.Add(r);
            mutLog.ReleaseMutex();

            mut.ReleaseMutex();
        }

        private void tmrMain_Tick(object sender, EventArgs e)
        {
            lblTime.Text = DateTime.Now.ToShortTimeString();
            if (bTestRunning)
            {
                lblStatus.Text = "RUNNING";
            }
            else
            {
                lblStatus.Text = "STOPPED";
            }
            
            if (bTestRunning)
            {
                currentStep.dura = currentStep.dura - tmrMain.Interval/1000;
                
                lblStepRemainingSec.Text = currentStep.dura.ToString()  + " s remaining";
                if (toolStripProgressBar1.Maximum >= currentStep.dura)
                    toolStripProgressBar1.Value = toolStripProgressBar1.Maximum - Convert.ToInt16(currentStep.dura);

                dataGridView1.CurrentCell = dataGridView1.Rows[currentStepNumber].Cells[0];

                datapoints[DATA_MOTOR_RPM_SETPOINT].Add(new DataPoint(currentStep.outp));

                if (currentStep.dura <= 0)
                {
                    if (cyclepoints.Count > currentStepNumber+1)
                    {
                        changeStep(currentStepNumber + 1);
                    }
                    else
                    {
                        bTestRunning = false;
                        changeStep(0, false);
                        stopMotor();
                    }

                }
                UpdateGraphAndUI();

            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {


            SaveFileDialog sfdial = new SaveFileDialog();
            sfdial.Filter = "Comma Separated Value (*.CSV)|*.CSV";
            sfdial.ShowDialog();
            logfilename = sfdial.FileName;
            if (logfilename == "")
            {
                MessageBox.Show("Select a log - file.", "Check filename?", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            for (int i = 0; i < chart1.Series.Count; i++)
            {
                chart1.Series[i].Points.Clear();
            }

            toolStripProgressBar1.Minimum = 0;
            toolStripProgressBar1.Maximum = cyclepoints.Count - 1;

            changeStep(0);
            bTestRunning = true;
            tmrWriteLog.Enabled = true;

        }

        private void changeStep(int step, bool controlMotor = true)
        {
            currentStepNumber = step;
            
            dataGridView1.CurrentCell = dataGridView1.Rows[currentStepNumber].Cells[0];
            dataGridView1.Rows[currentStepNumber].Selected = true;
            currentStep = new Cyclepoint();

            currentStep.dura = cyclepoints[currentStepNumber].dura;
            currentStep.outp = cyclepoints[currentStepNumber].outp;

            toolStripProgressBar1.Value = 0;
            toolStripProgressBar1.Maximum = Convert.ToInt16(cyclepoints[currentStepNumber].dura);
            
            if (controlMotor)
            {
                serialMotor.Open();
                serialMotor.Write(currentStep.outp.ToString());
                serialMotor.Close();
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            stopMotor();
            tmrWriteLog.Enabled = false;
        }

        private void stopMotor()
        {
            serialMotor.Open();
            serialMotor.Write("0");
            serialMotor.Close();

            bTestRunning = false;

        }


        private void writeLog()
        {
            try
            {

                string csvContent = "";

                mutLog.WaitOne();
                foreach (DataRow r in dtLogData.Rows)
                {
                    for (int i = 0; i < dtLogData.Columns.Count; i++)
                    {
                        csvContent = csvContent + r[i].ToString() + ";";
                    }
                    csvContent = csvContent + "\r\n";
                }

                System.IO.StreamWriter file = new System.IO.StreamWriter(logfilename, true);

                //    // Write the DataPoints into the file.

                file.Write(csvContent);

                file.Close();

                dtLogData.Rows.Clear();

                mutLog.ReleaseMutex();

                long length = new System.IO.FileInfo(logfilename).Length / 1000;
                lblLogStatus.Text = DateTime.Now.ToString() + " log " + length.ToString() + " kt";
            }
            catch (Exception ex)
            {
                lblLogStatus.Text = DateTime.Now.ToString() + " LOG ERROR: " + ex.Message;
            }


        }

        private void tmrWriteLog_Tick(object sender, EventArgs e)
        {
            writeLog();
        }
    }
}
