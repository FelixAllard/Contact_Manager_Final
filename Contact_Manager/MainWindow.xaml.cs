using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml.Linq;
using System.Windows.Controls;
using System;
using System.Linq.Expressions;
using System.Text;

namespace Contact_Manager
{
    public partial class MainWindow : Window
    {
        SqliteConnection _connection;


        public MainWindow()
        {
            InitializeComponent();
            _connection = new SqliteConnection("Data Source=contact-manager.db");
            _connection.Open();/*
            string createTableQuery = "CREATE TABLE IF NOT EXISTS Contacts (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT, Phone TEXT)";
            using (SqliteCommand command = new SqliteCommand(createTableQuery, _connection))
            {
                command.ExecuteNonQuery();
            }*/
            LoadContacts();
        }
        private void LoadContacts()
        {
            try
            {
                //Clearing else it just infinitly pills up
                ContactList.Items.Clear();
                string selectQuery = "SELECT * FROM Contacts";
                using (SqliteCommand command = new SqliteCommand(selectQuery, _connection))
                {
                    using (SqliteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ContactList.Items.Add($"{reader["Name"]} - {reader["Phone"]}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private void AddContact(String name, String phone)
        {
            try
            {
                string insertQuery = "INSERT INTO Contacts (Name, Phone) VALUES (@Name, @Phone)";
                using (SqliteCommand command = new SqliteCommand(insertQuery, _connection))
                {
                    // Use parameters to prevent SQL injection
                    command.Parameters.AddWithValue("@Name", name);
                    command.Parameters.AddWithValue("@Phone", phone);

                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex) { Console.WriteLine(ex); }
        }
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (ContactList.SelectedItem != null)
            {
                // Get the selected item's text
                string selectedContact = ContactList.SelectedItem.ToString();

                // Open the ContactDetailsWindow with the selected contact details
                ContactDetailsWindow detailsWindow = new ContactDetailsWindow(selectedContact);
                detailsWindow.ShowDialog();
            }
        }

        private void DeleteContactBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ContactList.SelectedItem != null)
            {
                // Get the selected item's text
                string selectedContact = ContactList.SelectedItem.ToString();

                // Extract the name from the selected item (assuming the format is "Name - Phone")
                string[] parts = selectedContact.Split('-');
                string nameToDelete = parts[0].Trim();



                // Delete the contact from the database based on the name
                string deleteQuery = "DELETE FROM Contacts WHERE Name = @Name";
                using (SqliteCommand command = new SqliteCommand(deleteQuery, _connection))
                {
                    // Use parameters to prevent SQL injection
                    command.Parameters.AddWithValue("@Name", nameToDelete);

                    command.ExecuteNonQuery();
                }
                // Reload contacts after deleting a contact
                LoadContacts();
            }
        }
        //IDK why it is called Button_Click but this is the handle of the 
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (ContactList.SelectedItem != null)
            {
                // Get the selected item int the Box in string format
                string selectedContact = ContactList.SelectedItem.ToString();

                // Extract the name from the selected item by dividing string with -
                string[] parts = selectedContact.Split('-');
                string nameToModify = parts[0].Trim();

                // Retrieve contact details from the database based on the name
                string selectQuery = "SELECT * FROM Contacts WHERE Name = @Name";
                using (SqliteCommand cmd = new SqliteCommand(selectQuery, _connection))
                {
                    cmd.Parameters.AddWithValue("@Name", nameToModify);

                    using (SqliteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            // Populate the TextBoxes with the contact details for modification
                            txtName.Text = reader["Name"].ToString();
                            txtPhone.Text = reader["Phone"].ToString();
                        }
                    }
                }

                // Delete the selected entry from the database
                string deleteQuery = "DELETE FROM Contacts WHERE Name = @Name";
                using (SqliteCommand command = new SqliteCommand(deleteQuery, _connection))
                {
                    // Use parameters to prevent SQL injection
                    command.Parameters.AddWithValue("@Name", nameToModify);

                    command.ExecuteNonQuery();
                }
                // Reload contacts after modifying
                LoadContacts();
            }
        }
        //Export to CSV
        private void ExportToCsvBtn_Click(object sender, RoutedEventArgs e)
        {
            //I found this online, I wasn't able to see where my file was elseway
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "ContactList",
                Filter = "CSV Files (*.csv)|*.csv",
                DefaultExt = ".csv",
                AddExtension = true
            };

            //I don't understand completly what this does, but it should show the dialog and get the selected file path
            bool? result = saveFileDialog.ShowDialog();
            //We verify with the previous event if the File path was found
            if (result == true)
            {
                string filePath = saveFileDialog.FileName;

                // Retrieve all contacts from the database
                string selectQuery = "SELECT * FROM Contacts";
                List<string> contacts = new List<string>();

                using (SqliteCommand command = new SqliteCommand(selectQuery, _connection))
                {
                    using (SqliteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // Format the contact data as a CSV line
                            string csvLine = $"{reader["Name"]},{reader["Phone"]}";
                            contacts.Add(csvLine);
                        }
                    }
                }
                // Write contacts to the CSV file
                try
                {
                    File.WriteAllLines(filePath, contacts, Encoding.UTF8);
                    MessageBox.Show("Contacts exported to CSV successfully.", "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (IOException ex)
                {
                    MessageBox.Show($"Error exporting contacts: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        private void ImportContactCsvBtn_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Import Contacts from CSV",
                Filter = "CSV Files (*.csv)|*.csv",
                DefaultExt = ".csv",
                AddExtension = true
            };

            // Show the dialog and get the selected file path
            bool? result = openFileDialog.ShowDialog();

            if (result == true)
            {
                string filePath = openFileDialog.FileName;

                try
                {
                    // Read lines from the CSV file
                    string[] csvLines = File.ReadAllLines(filePath, Encoding.UTF8);

                    // Insert contacts into the database
                    foreach (string csvLine in csvLines)
                    {
                        // Split each CSV line into name and phone
                        string[] parts = csvLine.Split(',');
                        if (parts.Length == 2)
                        {
                            // Insert the contact directly into the database
                            string insertQuery = "INSERT INTO Contacts (Name, Phone) VALUES (@Name, @Phone)";
                            using (SqliteCommand command = new SqliteCommand(insertQuery, _connection))
                            {
                                // Use parameters to prevent SQL injection
                                command.Parameters.AddWithValue("@Name", parts[0].Trim());
                                command.Parameters.AddWithValue("@Phone", parts[1].Trim());

                                command.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            // Handle invalid CSV format
                            MessageBox.Show($"Invalid CSV format: {csvLine}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }


                    // Reload contacts after importing
                    LoadContacts();
                    MessageBox.Show("Contacts imported from CSV successfully.", "Import Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error importing contacts: {ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void addContactBtn_Click(object sender, RoutedEventArgs e)
        {
            // Get the values from the textboxes
            string name = txtName.Text;
            string phone = txtPhone.Text;

            // Add the contact to the database
            AddContact(name, phone);

            // Reload contacts after adding a new one
            LoadContacts();
        }

        private void ResetDbBtn_Click(object sender, RoutedEventArgs e)
        {
            string truncateQuery = "DELETE FROM Contacts";
            using (SqliteCommand command = new SqliteCommand(truncateQuery, _connection))
            {
                command.ExecuteNonQuery();
            }
            LoadContacts();
        }
    }
}

