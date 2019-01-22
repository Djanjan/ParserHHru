using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace ParserHHru
{
    internal abstract class Parser
    {
        protected IConfiguration configParser;

        public ObservableCollection<Summary> Summaries { get; set; }

        protected CookieContainer cookie;

        public bool IsStart { get; set; }

        //public delegate void ParserStateHandler();
        //public event ParserStateHandler ChangerCollection;
        //public event ParserStateHandler AddCollection;

        public Parser()
        {
            configParser = AngleSharp.Configuration.Default.WithDefaultLoader();    
        }

        protected virtual IHtmlDocument RequestTo(string uri)
        {
            string data = "";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.UserAgent = "Mozilla/5.0 (Macintosh; U; Intel Mac OS X 10_6_7; en-US) AppleWebKit/534.16 (KHTML, like Gecko) Chrome/10.0.648.205 Safari/534.16";
            request.Timeout = 10000;
            request.Method = "GET";
            request.CookieContainer = cookie;

            HttpWebResponse response;
            try
            {
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (Exception)
            {
                return null;
            }


            if (response.StatusCode == HttpStatusCode.OK)
            {
                Stream receiveStream = response.GetResponseStream();
                StreamReader readStream = null;

                if (response.CharacterSet == null)
                {
                    readStream = new StreamReader(receiveStream);
                }
                else
                {
                    readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));
                }

                data = readStream.ReadToEnd();

                response.Close();
                readStream.Close();
            }
            return new HtmlParser().ParseDocument(data);
        }

        protected virtual IHtmlDocument RequestTo(string uri, string data)
        {
            string dataResponse = "";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);

            var postData = Encoding.ASCII.GetBytes(data);

            request.UserAgent = "Mozilla/5.0 (Macintosh; U; Intel Mac OS X 10_6_7; en-US) AppleWebKit/534.16 (KHTML, like Gecko) Chrome/10.0.648.205 Safari/534.16";
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = postData.Length;
            request.Timeout = 20000;
            request.CookieContainer = cookie;

            using (var stream = request.GetRequestStream())
            {
                stream.Write(postData, 0, postData.Length);
            }   

            var response = (HttpWebResponse)request.GetResponse();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Stream receiveStream = response.GetResponseStream();
                StreamReader readStream = null;

                if(response.CharacterSet == null)
                {
                    readStream = new StreamReader(receiveStream);
                }
                else
                {
                    readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));
                }

                dataResponse = readStream.ReadToEnd();

                response.Close();
                readStream.Close();
            }
            return new HtmlParser().ParseDocument(dataResponse);
        }

        protected virtual async Task<IHtmlDocument> RequestTo(string uri, Dictionary<string, string> data, string xslf)
        {
            HttpClient client = new HttpClient();
 
            var values = new Dictionary<string, string>
            {
                { "username", "djn" },
                { "password", "kol" },
                { "backUrl", "https%3A%2F%2Fhh.ru%2F" },
                { "action", "%D0%92%D0%BE%D0%B9%D1%82%D0%B8" },
                { "_xsrf", $"{xslf}" }
            };

            var content = new FormUrlEncodedContent(values);
            var response = await client.PostAsync(uri, content);

            var responseString = await response.Content.ReadAsStringAsync();

            return new HtmlParser().ParseDocument(responseString);
        }    

        /// <summary>
        /// Поиск вакансий
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public virtual async Task<List<Summary>> SearchSummariesAsync(string url)
        {
            IsStart = true;
            //Summaries = new ObservableCollection<Summary>();
            List<Summary> summaries = new List<Summary>();
            await Task.Run(() => 
            {
                summaries = GetSummaries(url);
            });
            IsStart = false;
            return summaries;
        }

        /// <summary>
        /// Собирает все резюме со страницы
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        protected virtual List<Summary> GetSummaries(string url)
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

            }

            return summaries;
        }

        /// <summary>
        /// Парсит одно резюме.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        protected virtual Summary GetSummary(IElement item)
        {
            Summary summary = new Summary();

            var el = item.QuerySelector("a.resume-search-item__name");
            summary.Specialty = el.InnerHtml;
            summary.Link = "https://hh.ru" + el.GetAttribute("href");

            el = item.QuerySelector("div.resume-search-item__description-content");
            if (el != null)
                summary.Experience = el.Text().Replace("&nbsp;", "");

            el = item.QuerySelector("meta[itemprop='birthDate']");
            if (el != null)
                summary.BirthDate = el.GetAttribute("content");

            el = item.QuerySelector("meta[itemprop='gender']");
            if (el != null)
                summary.Gendor = el.GetAttribute("content");

            el = item.QuerySelector("span[data-qa='resume-serp__resume-age']");
            if (el != null)
                summary.Age = el.InnerHtml.Replace("&nbsp;", "");

            el = item.QuerySelector("span.resume-search-item__company-name");
            if (el != null)
                summary.LastWork = el.InnerHtml;

            el = item.QuerySelector("span.resume-search-item__company-name span");
            if (el != null)
                summary.LastWork += " : " + el.InnerHtml;

            var summariePage = RequestTo(summary.Link);
            if (summariePage == null)
            {
                return summary;
            }

            el = summariePage.QuerySelector("a[itemprop='email']");
            if (el != null)
                summary.Email = el.InnerHtml;

            el = summariePage.QuerySelector("span[itemprop='addressLocality']");
            if (el != null)
                summary.City = el.InnerHtml;

            el = item.QuerySelector("div.resume-search-item__fullname");
            if (el != null)//Спасовская Марина Артуровна,&nbsp;<span data-qa="resume-serp__resume-age">26&nbsp;лет</span>
                summary.FIO = el.Text().Split(',').Count() != 1 ? el.Text().Split(',')[0] : "";

            el = item.QuerySelector("div.resume__contacts-phone-print-version span[itemprop='telephone']");
            if (el != null)
                summary.Phone = el.Text();

            var els = summariePage.QuerySelectorAll("div.bloko-column.bloko-column_xs-4 p");
            if (els.Length != 0)
            {
                foreach (var elem in els)
                {
                    if (elem.InnerHtml.Contains("Занятость:"))
                        summary.Employment = elem.InnerHtml.Replace("Занятость: ", "");

                    if (elem.InnerHtml.Contains("График работы: "))
                        summary.Schedule = elem.InnerHtml.Replace("График работы: ", "");
                }
            }

            els = summariePage.QuerySelectorAll("span.Bloko-TagList-Text");
            if (els.Length != 0)
            {
                foreach (var elem in els)
                {
                    summary.Skills += elem.InnerHtml + " : ";
                }
            }

            el = summariePage.QuerySelector("div.resume-block-container div[data-qa='resume-block-skills']");
            if (el != null)
                summary.AboutMe = el.Text();

            els = summariePage.QuerySelectorAll("div.resume-block-item-gap div.bloko-columns-row");
            if (els.Length != 0)
            {
                foreach (var elem in els)
                {
                    var buff = elem.QuerySelector("div.bloko-column.bloko-column_s-2.bloko-column_m-2.bloko-column_l-2");
                    summary.Education += buff != null ? buff.Text() + " : " : "";

                    buff = elem.QuerySelector("div.resume-block__sub-title[itemprop='name']");
                    summary.Education += buff != null ? buff.Text() + " - " : "";

                    buff = elem.QuerySelector("div[data-qa='resume-block-education-organization']");
                    summary.Education += buff != null ? buff.Text() + " | " : " | ";
                }
            }

            els = summariePage.QuerySelectorAll("p[data-qa='resume-block-language-item']");
            if (els.Length != 0)
            {
                foreach (var elem in els)
                {
                    summary.Languages += elem.Text() + " | ";
                }
            }

            return summary;
        }

    }
}
