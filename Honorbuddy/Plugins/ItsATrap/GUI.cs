using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ItsATrap
{
    public partial class GUI : Form
    {
        private MobHandler mobHandler;

        public GUI()
        {
            InitializeComponent();
            mobHandler = new MobHandler(ListBox);
        }

        public MobHandler getMobHandler()
        {
            return mobHandler;
        }

        private void ListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ListBox.SelectedItem != null)
            {
                string name = ListBox.SelectedItem.ToString();
                int id = mobHandler.getMobId(name);

                mobIdTextBox.Text = "" + id;
                mobNameTextBox.Text = name;

                mobHandler.listAllItems();
            }
        }

        //Add/change
        private void button1_Click(object sender, EventArgs e)
        {
            Console.WriteLine("Button1 clicked");
            string id = mobIdTextBox.Text;
            int iId = 0;
            try
            {
                iId = int.Parse(id);
            }
            catch(Exception ex)
            {
                mobIdTextBox.Text = ""+iId;
            }

            string name = mobNameTextBox.Text;
            if(name=="")
            {
                name = ""+iId;
            }

            mobHandler.addItem(name, iId);
        }

        //Delete
        private void button2_Click(object sender, EventArgs e)
        {
            mobHandler.deleteItem();
        }
    }
}
