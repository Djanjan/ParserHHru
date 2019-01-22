using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ParserHHru
{
    /// <summary>
    /// Логика взаимодействия для AuthorizationPage.xaml
    /// </summary>
    public partial class AuthorizationPage : Window
    {
        public AuthorizationPage()
        {
            InitializeComponent();
        }

        private void AuthorizationButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ManagerConnection.IsConnection())
            {
                System.Windows.MessageBox.Show("Отсутствует подлючение к сети");
                return;
            }
            if (LoginTextBox.Text == "" || PasswordTextBox.Text == "")
            {
                MessageBox.Show("Ошибка", "Введите логин и пароль");
                return;
            }

            MainWindow main;

            try
            {
                main = new MainWindow(LoginTextBox.Text, PasswordTextBox.Text);
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message + " Авторизация доступна ток для компаний.", "Ошибка");
                return;
            }

            main.Owner = this;
            main.Show();

            this.Visibility = Visibility.Hidden;
        }

        private void AuthorizationSkipButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow main = new MainWindow();
            main.Owner = this;

            main.Show();

            this.Visibility = Visibility.Hidden;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
