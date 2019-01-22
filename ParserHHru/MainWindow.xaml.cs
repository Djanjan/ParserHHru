using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ParserHHru
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<Summary> summaries;
        private Parser parser;
        private string pathCSV = Environment.CurrentDirectory;
        private bool isLimit;

        public MainWindow(string login, string password)
        {
            InitializeComponent();

            summaries = new ObservableCollection<Summary>();
            try
            {
                parser = new ParserAuthorized(login, password);
            }
            catch (Exception e)
            {
                throw  e;
            }

            this.Loaded += Window_Loaded;

            CountMaxSummaryPanel.Visibility = Visibility.Visible;
            CheckLimit();
            isLimit = true;
        }

        public MainWindow()
        {
            InitializeComponent();

            summaries = new ObservableCollection<Summary>();

            FeedbackTabItem.Visibility = Visibility.Hidden;

            parser = new ParserNotAuthorized();
            isLimit = false;
        }

        /// <summary>
        /// Проверяет и выставляет настройки лимита выборки
        /// </summary>
        private void CheckLimit()
        {
            DateTime endVisit = Properties.Settings.Default.EndVisit;
            if (endVisit == default(DateTime))
            {
                Properties.Settings.Default.EndVisit = DateTime.Now;
                Properties.Settings.Default.MaxCountSummary = 500;
                CountMaxSummaryTextBlock.Text = "500";
                ParseProgressBar.Maximum = 500;
                Properties.Settings.Default.Save();
                return;
            }

            int maxCountSumm = Properties.Settings.Default.MaxCountSummary;

            if (endVisit.Date.AddHours(24) > DateTime.Now.Date)
            {
                ParseProgressBar.Maximum = maxCountSumm;
                CountMaxSummaryTextBlock.Text = maxCountSumm.ToString();
            }
            else
            {
                Properties.Settings.Default.EndVisit = DateTime.Now;
                Properties.Settings.Default.MaxCountSummary = 500;
                CountMaxSummaryTextBlock.Text = "500";
                Properties.Settings.Default.Save();
            }
        }

        private void ParseStart_Click(object sender, RoutedEventArgs e)
        {
            if (!ManagerConnection.IsConnection())
            {
                System.Windows.MessageBox.Show("Отсутствует подлючение к сети");
                return;
            }

            if (parser.IsStart)
            {
                System.Windows.MessageBox.Show("Дождитесь остановки парсера!","Предупреждение");
                return;
            }

            if (CountSummaryAllTextBox.Text != "" && (CountMaxSummaryPanel.Visibility == Visibility.Visible))
            {
                ParseAndWrite(pathCSV, Convert.ToInt32(CountSummaryAllTextBox.Text) > Convert.ToInt32(CountMaxSummaryTextBlock.Text)
                    ? Convert.ToInt32(CountMaxSummaryTextBlock.Text) : Convert.ToInt32(CountSummaryAllTextBox.Text));
            }
            else
            {
                ParseAndWrite(pathCSV, CountSummaryAllTextBox.Text == "" ? 100 : Convert.ToInt32(CountSummaryAllTextBox.Text));
            }
        }

        private void SummariesGetSearch_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            summaries = parser.Summaries;
            SummaryViewDataGrid.ItemsSource = summaries;
            AddProgress(1);
        }

        private void AddProgress(int value)
        {
            ParseProgressBar.Value += value;

            if (isLimit)
            {
                var count = Convert.ToInt32(CountMaxSummaryTextBlock.Text) - value;
                CountMaxSummaryTextBlock.Text = count.ToString();
            }
        }

        private string UrlSearch(int page, string itemsOnPage)
        {
            var selectedGender = GenderComboBox.SelectedItem;
            var itemGender = (TextBlock)selectedGender;

            return "https://hh.ru/search/resume?" +
                $"text={SearchTextBox.Text}" +
                "&logic=normal" +
                "&pos=full_text" +
                "&exp_period=all_time" +
                //"&area=1556" + Код региона
                "&relocation=living_or_relocation" +
                "&salary_from=" +
                "&salary_to=" +
                "&currency_code=RUR" +
                "&education=none" +
                $"&age_from={AgeFromTextBox.Text}" +
                $"&age_to={AgeToTextBox.Text}" +
                $"&gender={itemGender.Text}" +
                "&order_by=publication_time" +
                "&search_period=0" +
                $"&items_on_page={itemsOnPage}" +
                $"&page={page.ToString()}";
        }

        //private void NextViewButton_Click(object sender, RoutedEventArgs e)
        //{
        //    curectPage++;
        //    StartParser(curectPage);
        //}

        //private void PrevViewButton_Click(object sender, RoutedEventArgs e)
        //{
        //    curectPage--;
        //    StartParser(curectPage);
        //}

        private void OpenDialogFolden_Click(object sender, RoutedEventArgs e)
        {
            if (parser.IsStart)
            {
                System.Windows.MessageBox.Show("Дождитесь остановки парсера!", "Предупреждение");
                return;
            }

            pathCSV = OpenDialogSetPatch();
        }

        private async void ParseAndWrite(string path, int countMaxSummaries)
        {
            List<Summary> summaryList = new List<Summary>();

            int countSummaries = countMaxSummaries;
            ParseProgressBar.Value = 0;
            ParseProgressBar.Maximum = countSummaries;

            summaryList.AddRange(summaries);

            parser.Summaries = new ObservableCollection<Summary>();
            parser.Summaries.CollectionChanged += SummariesGetSearch_CollectionChanged;

            try
            {
                int i = 0;
                int iPage = 0;
                int maxCountSumm = countSummaries;
                while (i < countSummaries)
                {
                    if (maxCountSumm > 100)
                    {
                        summaryList.AddRange(await parser.SearchSummariesAsync(UrlSearch(iPage, "100")));
                        i += 100;
                        iPage++;
                    }
                    else
                    {
                        summaryList.AddRange(await parser.SearchSummariesAsync(UrlSearch(iPage, maxCountSumm.ToString())));
                        i += maxCountSumm;
                        iPage++;
                        break;
                    }
                    maxCountSumm -= i;
                }
            }
            catch (Exception)
            {
                if (ManagerCSV.Write(summaryList, path))
                    System.Windows.MessageBox.Show($"Данные выгруженны по пути : {path}", "Уведомление");
                else
                    System.Windows.MessageBox.Show($"Данные не выгруженны!", "Ошибка");
                return;
            }

            if (summaryList.Count != 0)
            {
                if (ManagerCSV.Write(summaryList, path))
                    System.Windows.MessageBox.Show($"Данные выгруженны по пути : {path}", "Уведомление");
                else
                    System.Windows.MessageBox.Show($"Данные не выгруженны!", "Ошибка");
            }
            else
            {
                System.Windows.MessageBox.Show($"Данные не полученны! Походу вылезла капча, попробуйте перезайти в программу!", "Ошибка");
            }

        }

        private void OpenDialog()
        {
            string path = null;

            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            path = dialog.SelectedPath;

            if (path != null)
            {
                if (ManagerCSV.Write(summaries.ToList(), path, false))
                    System.Windows.MessageBox.Show($"Данные выгруженны по пути : {path}", "Уведомление");
                else
                    System.Windows.MessageBox.Show($"Данные не выгруженны!", "Ошибка");
            }
        }

        private string OpenDialogSetPatch()
        {
            string path = null;

            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            path = dialog.SelectedPath;

            if (path != null && path != "C:\\")
            {
                return path;
            }
            else
            {
                return Environment.CurrentDirectory;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Properties.Settings.Default.EndVisit = DateTime.Now;
            Properties.Settings.Default.MaxCountSummary = Convert.ToInt32(CountMaxSummaryTextBlock.Text);
            Properties.Settings.Default.Save();

            if (summaries.Count != 0)
            {
                ManagerCSV.Write(summaries.ToList(), pathCSV);
                //if (ManagerCSV.Write(summaries.ToList(), pathCSV))
                //    System.Windows.MessageBox.Show($"Данные выгруженны по пути : {pathCSV}", "Уведомление");
                //else
                //    System.Windows.MessageBox.Show($"Данные не выгруженны!", "Ошибка");
            }

            //System.Windows.Application.Current.Shutdown();
            this.Owner.Close();
        }

        private void ListViewItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //var item = sender as System.Windows.Controls.ListViewItem;
            //if (item != null && item.IsSelected)
            //{
               
            //};
        }

        private async void WriteFeedbakButton_Click(object sender, RoutedEventArgs e)
        {
            if (parser.IsStart)
            {
                System.Windows.MessageBox.Show("Дождитесь остановки парсера!", "Предупреждение");
                return;
            }

            var vacancy = VacancyListView.SelectedItem;
            if (vacancy == null)
            {
                System.Windows.MessageBox.Show("Выберите вакансию!", "Ошибка");
                return;
            }

            pathCSV = OpenDialogSetPatch();

            List<Summary> summaryList = new List<Summary>();

            isLimit = false;
            ParseProgressBar.Value = 0;
            ParseProgressBar.Maximum = 500;

            summaryList.AddRange(summaries);

            parser.Summaries = new ObservableCollection<Summary>();
            parser.Summaries.CollectionChanged += SummariesGetSearch_CollectionChanged;

            try
            {
                Vacancie buff = new Vacancie();
                while (true)
                {
                    buff = await ((ParserAuthorized)parser).GetFeedbackAsync((Vacancie)vacancy);
                    if (buff.Feedbacks.Count == 0)
                        break;

                    summaryList.AddRange(buff.Feedbacks);
                }
            }
            catch (Exception)
            {
                if (ManagerCSV.Write(summaryList, pathCSV))
                    System.Windows.MessageBox.Show($"Данные выгруженны по пути : {pathCSV}", "Уведомление");
                else
                    System.Windows.MessageBox.Show($"Данные не выгруженны!", "Ошибка");
                return;
            }

            if (summaryList.Count != 0)
            {
                if (ManagerCSV.Write(summaryList, pathCSV))
                    System.Windows.MessageBox.Show($"Данные выгруженны по пути : {pathCSV}", "Уведомление");
                else
                    System.Windows.MessageBox.Show($"Данные не выгруженны!", "Ошибка");
            }
            else
            {
                System.Windows.MessageBox.Show($"Данные не полученны! Походу вылезла капча, попробуйте перезайти в программу!", "Ошибка");
            }

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var vacancyes = ((ParserAuthorized)parser).GetVacancies();
            VacancyListView.ItemsSource = vacancyes;
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            //this.Owner.Visibility = Visibility.Visible;
            AuthorizationPage page = new AuthorizationPage();
            page.Show();

            this.Close();
        }
    }
}
