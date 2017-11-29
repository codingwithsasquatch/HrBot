using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using HrBot.Dialogs;
using HrBot.Services;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;
using Microsoft.Azure.Documents.Client;
using Microsoft.Bot.Builder.Azure;

namespace HrBot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        private readonly string _defaultLanguage = "English";

        public MessagesController()
        {
        }
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            try
            {
                if (activity.Type == ActivityTypes.Message)
                {
                    // Todo: assume the language will be the same throughout the conversation and store the state in user data.
                    // re-evaluate the language only when no match
                    //var detectedLanguage = await DetectLanguageAsync(activity.Text);
                    //if (detectedLanguage != _defaultLanguage)
                    //{
                    //    //activity.Text = await TranslateTextFromAsync(detectedLanguage, activity.Text);
                    //}
                    var cognitiveService = new CognitiveServices();
                    if (activity.Text.StartsWith("feedback", StringComparison.CurrentCultureIgnoreCase))
                    {
                        var result = cognitiveService.InterpreteFeedback("en", activity.Text);
                        ConnectorClient client = new ConnectorClient(new Uri(activity.ServiceUrl));
                        Activity reply = activity.CreateReply(result);
                        await client.Conversations.ReplyToActivityAsync(reply);
                    }
                    else if (activity.Text.StartsWith("language", StringComparison.CurrentCultureIgnoreCase))
                    {
                        var result = await cognitiveService.DetectLanguageAsync(activity.Text);
                        ConnectorClient client = new ConnectorClient(new Uri(activity.ServiceUrl));
                        Activity reply = activity.CreateReply($"Language detected {result}");
                        await client.Conversations.ReplyToActivityAsync(reply);
                    }
                    else
                    {
                        await Conversation.SendAsync(activity, () => new CustomQnAMakerDialog());
                    }
                }
                else
                {
                    HandleSystemMessage(activity);
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }
}