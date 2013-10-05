using System;
using System.Windows.Forms;

namespace DummyForm
{
    public partial class DummyForm : Form
    {
        public DummyForm()
        {
            InitializeComponent();
        }

        private void DummyForm_Load(object sender, EventArgs e)
        {
            Cycle();
        }

        private void btnNextCycle_Click(object sender, EventArgs e)
        {
            Cycle();
        }

        private const string _message_uni = "Здравей Свят";
        private const string _message = "Hello World";
        private int cycleIndex = 0;
        private const int cycleLength = 10;
        private int _integer = 0;

        private void Cycle()
        {
            lbConsole.Items.Add("Values in cycle " + cycleIndex++ + " from " + cycleLength + ":");
            lbConsole.Items.Add("Unicode message: " + _message_uni);
            lbConsole.Items.Add("ASCII message: " + _message);
            lbConsole.Items.Add("Current incrementing integer value: " + _integer++);
            lbConsole.Items.Add("");
            lbConsole.SelectedIndex = lbConsole.Items.Count - 1;
            lbConsole.SelectedIndex = -1;
        }
    }
}