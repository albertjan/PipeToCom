using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NP2COM;


namespace NP2COMV
{
    public partial class Form1 : Form
    {
        public Form1 ()
        {
            InitializeComponent ();
        }

        private void Form1_Load (object sender, EventArgs e)
        {
            namedPipeComboBox.Items.AddRange(Directory.GetFiles(@"\\.\pipe\"));
            serialPortComboBox.Items.AddRange(SerialPort.GetPortNames());
            parityComboBox.Items.AddRange(Enum.GetNames(typeof(Parity)));
            stopBitsComboBox.Items.AddRange(Enum.GetNames(typeof(StopBits)));
            if (namedPipeComboBox.Items.Count > 0) namedPipeComboBox.SelectedIndex = 0;
            if (serialPortComboBox.Items.Count > 0) serialPortComboBox.SelectedIndex = 0;
            baudRateComboBox.SelectedIndex = 10;
            parityComboBox.SelectedIndex = 0;
            dataBitsComboBox.SelectedIndex = 3;
            stopBitsComboBox.SelectedIndex = 1;
        }

        private void button1_Click (object sender, EventArgs e)
        {
            new Connection(new Settings
                               {
                                   BaudRate = (int)baudRateComboBox.SelectedItem,
                                   ComPort = (string)serialPortComboBox.SelectedItem,
                               });
        }

    }
}
