using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;

namespace HrBot.Services
{
    public class CognitiveServices
    {
        private readonly string _defaultLanguage = "English";
        private readonly ITextAnalyticsAPI _client;

        public CognitiveServices()
        {
            _client = Initializeclient();
        }

        private static ITextAnalyticsAPI Initializeclient()
        {
            ITextAnalyticsAPI client = new TextAnalyticsAPI();
            client.AzureRegion = AzureRegions.Westcentralus;
            client.SubscriptionKey = ConfigurationManager.AppSettings["Ocp-Apim-Subscription-Key"];

            Console.OutputEncoding = System.Text.Encoding.UTF8;
            return client;
        }

        public async Task<string> TranslateTextFromAsync(string language, string text)
        {
            var client = new HttpClient();

            //var baseUri = "https://api.microsofttranslator.com/V2/Http.svc/Translate";
            var baseUri = "https://api.cognitive.microsoft.com/sts/v1.0";
            var builder = new UriBuilder(baseUri);

            var queryString = HttpUtility.ParseQueryString(string.Empty);
            queryString["from"] = language;
            queryString["to"] = _defaultLanguage;
            queryString["text"] = text;
            builder.Query = queryString.ToString();
            string url = builder.ToString();

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", ConfigurationManager.AppSettings["Ocp-Apim-Subscription-Key-Translator"]);
            // Request parameters
            HttpResponseMessage response = await client.GetAsync(url);

            return await response.Content.ReadAsStringAsync();

        }

        public async Task<string> DetectLanguageAsync(string text)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            LanguageBatchResult result = await _client.DetectLanguageAsync(
                new BatchInput(
                    new List<Input>()
                    {
                        new Input("1", text)
                    }),
                numberOfLanguagesToDetect: 1);

            if (result.Documents.FirstOrDefault() != null
                && result.Documents.First().DetectedLanguages.FirstOrDefault() != null)
            {
                return result.Documents.First().DetectedLanguages.First().Name;
            }
            return _defaultLanguage;
        }

        public String InterpreteFeedback(string language, string text)
        {
            SentimentBatchResult result = _client.Sentiment(
                new MultiLanguageBatchInput(
                    new List<MultiLanguageInput>()
                    {
                        new MultiLanguageInput(language, "1", text)
                    }
                ));
            var score = result.Documents.First().Score;
            if (score > 0.7d)
            {
                return $" Score = {score}. Thank you for your feedback. We are glad you enjoyed the experience";
            }
            else if (score < 0.3)
            {
                return $" Score = {score}. We are sorry you did not find the HR Bot useful. Feel free to look at our complete FAQ: www.ourfaq.com or call the HR department at (800)-111-1111";
            }
            else
            {
                return $" Score = {score}. Thank you for your feeback";
            }
        }
    }
}