using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace API_Test_Client {
	public partial class Main : Form {
		public Main() {
			InitializeComponent();
			Data.LoadFiles();

			//Setup 
			RequestTypesList.DataSource = new BindingSource(Data.requestTypes, null);
			RequestTypesList.DisplayMember = "Key";
			RequestEditorField.Text = Data.requestTypes[RequestTypesList.GetItemText(RequestTypesList.SelectedItem)];
		}

		private void SendRequest_Click(object sender, EventArgs e) {
			
		}

		private void UpdateEditor(object sender, EventArgs e) {
			RequestEditorField.Text = Data.requestTypes[RequestTypesList.GetItemText(RequestTypesList.SelectedItem)];
		}
	}
}
