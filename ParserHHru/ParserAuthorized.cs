using AngleSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ParserHHru
{
    internal class ParserAuthorized : Parser
    {
        public ParserAuthorized(string login, string password)
        {
            configParser = AngleSharp.Configuration.Default.WithDefaultLoader();

            cookie = new CookieContainer();

            //Авторизация
            var html = RequestTo("https://hh.ru/");

            var xslf = html.QuerySelector("input[name='_xsrf']");

            RequestTo("https://hh.ru/account/login?backurl=%2F", $"username={Uri.EscapeDataString(login)}" +
            $"&password={password}" +
            $"&backUrl=https%3A%2F%2Fhh.ru%2F&action=%D0%92%D0%BE%D0%B9%D1%82%D0%B8&_xsrf={xslf.GetAttribute("value")}");
            //Авторизация

            if (!IsAuthorized())
            {
                throw new Exception("Ошибка авторизации, авторизация не выполнена!");
            }
        }

        public bool IsAuthorized()
        {
            var baseHtml = RequestTo("https://hh.ru/employer/vacancies");
            //var baseHtml = RequestTo("https://hh.ru/");
            if (baseHtml == null)
            {
                return false;
            }

            var elem = baseHtml.QuerySelector("div.bloko-form-item input.bloko-input");
            //var elem = baseHtml.QuerySelector("label.login-input");

            if (elem == null)
                return true;
            else
                return false;
        }

        protected override List<Summary> GetSummaries(string url)
        {
            List<Summary> summaries = new List<Summary>();

            var baseHtml = RequestTo(url);
            if (baseHtml == null)
            {
                return new List<Summary>();
            }

            var listSummaries = baseHtml.QuerySelectorAll("div.resume-search-item");

            foreach(var item in listSummaries)
            {
                var sum = GetSummary(item);
                if (sum != null)
                {
                    summaries.Add(sum);
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        Summaries.Add(sum);
                    }));
                }
                System.Threading.Thread.Sleep(8000);
            }

            return summaries;
        }

        /// <summary>
        /// Получить все вакансии
        /// </summary>
        /// <returns></returns>
        public List<Vacancie> GetVacancies()
        {
            IsStart = true;
            var baseHtml = RequestTo("https://hh.ru/employer/vacancies");
            if (baseHtml == null)
            {
                return new List<Vacancie>();
            }

            List<Vacancie> vacancies = new List<Vacancie>();

            var listVacancies = baseHtml.QuerySelectorAll("tbody.vacancies-dashboard-table-row-group");

            if (listVacancies.Length == 0)
            {
                return vacancies;
            }

            foreach (var item in listVacancies)
            {
                var vacan = item.QuerySelector("a.bloko-link.bloko-link_list");

                vacancies.Add(new Vacancie()
                {
                    Name = vacan.InnerHtml,
                    Link = vacan.GetAttribute("href")
                });
            }
            IsStart = false;
            return vacancies;
        }

        /// <summary>
        /// Получает отклики
        /// </summary>
        /// <param name="vacancie"></param>
        /// <returns></returns>
        public Vacancie GetFeedback(Vacancie vacancie)
        {
            Summaries = new ObservableCollection<Summary>();

            var url = $"https://hh.ru/employer/vacancyresponses?vacancyId={vacancie.Link.Split('/')[2]}";
            var baseHtml = RequestTo(url);
            if (baseHtml == null)
            {
                return new Vacancie();
            }


            List<Summary> summaries = new List<Summary>();

            int page = 0;
            //while(page != 1)
            while (true)
            {
                var buff = GetSummaries(url + "&page=" + page);
                if (buff.Count == 0)
                    break;

                summaries.AddRange(buff);

                page++;
            }

            vacancie.Feedbacks = summaries;
            return vacancie;
        }

        public async Task<Vacancie> GetFeedbackAsync(Vacancie vacancie)
        {
            IsStart = true;
            List<Summary> summaries = new List<Summary>();
            await Task.Run(() =>
            {
                var url = $"https://hh.ru/employer/vacancyresponses?vacancyId={vacancie.Link.Split('/')[2]}";
                var baseHtml = RequestTo(url);
                if (baseHtml == null)
                {
                    return;
                }

                int page = 0;
                //while(page != 1)
                while (true)
                {
                    var buff = GetSummaries(url + "&page=" + page);
                    if (buff.Count == 0)
                        break;

                    summaries.AddRange(buff);

                    page++;
                }

                vacancie.Feedbacks = summaries;
            });
            IsStart = false;
            vacancie.Feedbacks = summaries;
            return vacancie;
        }
    }
}
