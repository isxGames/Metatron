using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Metatron
{
    public partial class TextInputForm : Form
    {
        public string InputText = string.Empty;

        public TextInputForm(string defaultText)
        {
            InitializeComponent();
            textBoxInputText.Text = defaultText;

            DialogResult = DialogResult.Cancel;
        }

        private void buttonOk_Click(object sender, EventArgs e)
        {
            InputText = textBoxInputText.Text;
            
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
