
using System.Windows;

namespace Contact_Manager
{
    public partial class ContactDetailsWindow : Window
    {
        public ContactDetailsWindow(string contactDetails)
        {
            InitializeComponent();
            lblContactDetails.Text = contactDetails;
        }
    }
}

