using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using Chronic.Handlers;
using Microsoft.Bot.Builder.CognitiveServices.QnAMaker;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System.Threading.Tasks;
using HrBot.Data;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace HrBot.Dialogs
{
    [Serializable]
    public class CustomQnAMakerDialog : QnAMakerDialog
    {
        private const string LastQuestions = "lastquestions";
        private const string MyDirectDepositsQuestion = "Q. what is my direct deposit election";

        public CustomQnAMakerDialog() :
            base(new QnAMakerService(
                new QnAMakerAttribute(
                    ConfigurationManager.AppSettings["QnASubscriptionKey"],
                    ConfigurationManager.AppSettings["QnAKnowledgebaseId"],
                    "No good match in FAQ.",
                    0.5)))
        { }

        protected override async Task RespondFromQnAMakerResultAsync(IDialogContext context, IMessageActivity message, QnAMakerResults results)
        {
            try
            {
                if (results.Answers.Count > 0)
                {
                    var response = OverrideReponseForUserSpecificRequest(results, message.From.Id);
                    // Store Data if the user is authenticated
                    //if (message.From.Id != "default-user") => for now we do it just for the demo purpose
                    {
                        List<PastQnA> lastquestionsData;

                        // We want to suggest the previous questions only if we are in a new conversation
                        if (context.UserData.TryGetValue<List<PastQnA>>(LastQuestions, out lastquestionsData)
                            && lastquestionsData.Any()
                            && lastquestionsData.First().ConversationId != message.Conversation.Id)
                        {
                            StringBuilder previousQnA = new StringBuilder();
                            previousQnA.AppendLine("Hello. Here are the questions from your previous conversation:");

                            foreach (var qna in lastquestionsData)
                            {
                                previousQnA.AppendLine($"{qna.Question}");
                                previousQnA.AppendLine($"A: {qna.Answer}");
                            }
                            previousQnA.AppendLine("You can ask any other questions related to Direct deposit.");
                            context.UserData.SetValue(LastQuestions, new List<PastQnA>());
                            response = previousQnA.ToString();

                            await context.PostAsync(response);
                        }
                        else
                        {
                            var currentQnA = new PastQnA()
                            {
                                ConversationId = message.Conversation.Id,
                                Question = results.Answers.First().Questions.First(),
                                Answer = response
                            };

                            // We don't add initial greetings and concatenate the list of questions
                            if (!response.StartsWith("Hello", StringComparison.InvariantCultureIgnoreCase))
                            {
                                if (lastquestionsData != null)
                                {
                                    lastquestionsData.Add(currentQnA);
                                }
                                else
                                {
                                    lastquestionsData = new List<PastQnA>() { currentQnA };
                                }
                                context.UserData.SetValue(LastQuestions, lastquestionsData);
                            }
                            await context.PostAsync(response);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await context.PostAsync("Sorry we did not get that. Please try again");

            }
        }

        private static string OverrideReponseForUserSpecificRequest(QnAMakerResults results, string employeeId)
        {
            var response = results.Answers.First().Answer;
            var question = results.Answers.First().Questions.First();
            if (question.StartsWith(MyDirectDepositsQuestion, StringComparison.CurrentCultureIgnoreCase))
            {
                var docDbUri = new Uri(ConfigurationManager.AppSettings["DocumentDbUrl"]);
                var docDbKey = ConfigurationManager.AppSettings["DocumentDbKey"];
                using (var client = new DocumentClient(docDbUri, docDbKey))
                {
                    var query = from d in client.CreateDocumentQuery<DirectDeposit>(UriFactory.CreateDocumentCollectionUri("botdb", "botcollection"))
                                where d.Employee == employeeId
                                select d;
                    if (query.AsEnumerable().FirstOrDefault<DirectDeposit>() != null)
                    {
                        StringBuilder sb = new StringBuilder();
                        int cnt = 1;
                        foreach (var dd in query.AsEnumerable<DirectDeposit>())
                        {
                            sb.AppendLine("Direct deposit number ").Append(cnt).AppendLine(" - ");
                            sb.AppendLine("PayType: ").Append(dd.PayType).AppendLine(" - ");
                            sb.AppendLine("PaymentType: ").Append(dd.PaymentType).AppendLine(" - ");
                            sb.AppendLine("Account: ").Append(dd.Account).AppendLine(" - ");
                            sb.AppendLine("AccountNumber: ").Append(dd.AccountNumber).AppendLine(" - ");
                            sb.AppendLine("Distribution: ").Append(dd.Distribution).AppendLine(" - ");
                        }
                        response = sb.ToString();
                    }
                    else
                    {
                        response = "No direct deposit found";
                    }
                }
            }
            return response;
        }

        protected override async Task DefaultWaitNextMessageAsync(IDialogContext context, IMessageActivity message, QnAMakerResults results)
        {
            await base.DefaultWaitNextMessageAsync(context, message, results);
        }
    }
}