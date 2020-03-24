using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace open_ventilator_tester
{
    public partial class Form1 : Form
    {
        BindingList<Cyclepoint> cyclepoints = new BindingList<Cyclepoint>();
        bool bTestRunning = false;
        int currentStepNumber = -1;
        Cyclepoint currentStep = new Cyclepoint();

        public Form1()
        {
            InitializeComponent();
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

                if (currentStep.dura <= 0)
                {
                    if (cyclepoints.Count() > currentStepNumber+1)
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
                

            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            toolStripProgressBar1.Minimum = 0;
            toolStripProgressBar1.Maximum = cyclepoints.Count - 1;

            changeStep(0);
            bTestRunning = true;
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
        }

        private void stopMotor()
        {
            serialMotor.Open();
            serialMotor.Write("0");
            serialMotor.Close();

            bTestRunning = false;

        }
    }
}
