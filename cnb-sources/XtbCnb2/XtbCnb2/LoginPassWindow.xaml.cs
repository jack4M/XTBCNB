using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ActiproSoftware.Windows.Controls.Ribbon;

namespace XtbCnb2
{
    /// <summary>
    /// Interaction logic for LoginPassWindow.xaml
    /// </summary>
    public partial class LoginPassWindow : RibbonWindow
    {
        public LoginPassWindow()
        {
            InitializeComponent();
        }

        private void submit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void validate()
        {
            submit.IsEnabled = username.Text.Length > 2 && password.Password.Length > 2;
        }

        private void username_TextChanged(object sender, TextChangedEventArgs e)
        {
            validate();
        }

        private void password_PasswordChanged(object sender, RoutedEventArgs e)
        {
            validate();
        }
    }
}
