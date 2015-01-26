using System;
using System.Windows.Forms;

namespace BuddyCon2
{
    public partial class FormSettings : Form
    {
        public FormSettings()
        {
            InitializeComponent();
        }

        private void FormSettings_Load(object sender, EventArgs e)
        {
            pgSettings.SelectedObject = BuddyConSettings2.Instance;
        }

        private void pgSettings_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            if (pgSettings.SelectedObject != null && pgSettings.SelectedObject is BuddyConSettings2)
                ((BuddyConSettings2)pgSettings.SelectedObject).Save();
        }
    }
}
