using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace Metatron
{
	public partial class ManuallyAddPilotForm : Form
	{
		public ManuallyAddPilotForm()
		{
			InitializeComponent();
		}

		private void radioButtonPilot_CheckedChanged(object sender, EventArgs e)
		{
			if (sender == radioButtonPilot)
			{
				if (((RadioButton)sender).Checked)
				{
					textBoxName.Visible = true;
					label1.Visible = true;
					label3.Visible = true;
					label4.Visible = true;
					label5.Visible = true;
					label6.Visible = true;
				}
			}
			else if (sender == radioButtonCorporation)
			{
				if (((RadioButton)sender).Checked)
				{
					textBoxName.Visible = false;
					label3.Visible = false;
					label4.Visible = false;
					label5.Visible = false;
					label6.Visible = false;
					label1.Visible = false;
				}
			}
		}

		private void buttonAdd_Click(object sender, EventArgs e)
		{
			var cachedPilot = new Core.CachedPilot();
			if (radioButtonPilot.Checked)
			{
				cachedPilot.Name = textBoxName.Text;
				int charID;
				if (!int.TryParse(textBoxID.Text, out charID))
				{
					MessageBox.Show("The character ID must be a valid integer.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    textBoxID.SelectAll();
				}
				else
				{
                    if (!Core.Metatron.PilotCache.CachedPilotsById.ContainsKey(charID))
                    {
                        cachedPilot.CharID = charID;
                        Core.Metatron.PilotCache.AddPilot(cachedPilot);
                    }
                    else
                    {
                        MessageBox.Show("Pilot has already been added.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        textBoxID.SelectAll();
                    }
				}
			}
			else
			{
				int corpID;
                if (!int.TryParse(textBoxID.Text, out corpID))
                {
                    MessageBox.Show("The corporation ID must be a valid integer.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    textBoxID.SelectAll();
                }
                else
                {
                    if (!Core.Metatron.CorporationCache.CachedCorporationsById.ContainsKey(corpID))
                    {
                        Core.Metatron.CorporationCache.GetCorporationInfo(corpID);
                        var sanityCheck = 200;

                        while (!Core.Metatron.CorporationCache.CachedCorporationsById.ContainsKey(corpID) &&
                            sanityCheck-- > 0)
                        {
                            Thread.Sleep(50);
                        }

                        if (sanityCheck <= 0)
                        {
                            MessageBox.Show("Could not look up corporation info. Are you sure the corporation ID is correct?",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                
			}
			Close();
		}
	}

	public sealed class ManuallyAddPilotEventArgs : EventArgs
	{
		public object CachedObject;
		public bool IsPilot;
		public ManuallyAddPilotEventArgs(Core.CachedPilot cachedObject, bool isPilot)
		{
			CachedObject = cachedObject;
			IsPilot = isPilot;
		}
	}
}
