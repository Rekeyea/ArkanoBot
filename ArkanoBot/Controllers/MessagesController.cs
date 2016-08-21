using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using System.Dynamic;
using System.Collections;
using System.Collections.Generic;

namespace ArkanoBot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        const string LUIS_URL = @"https://api.projectoxford.ai/luis/v1/application?id=bd160364-aeb6-4e9b-b246-9b17a64eb3a5&subscription-key=fd8ef514973f4a2192a4f43a2719d3f4&q={0}";

        private Dictionary<string, List<string>> EmployeeProjectMapping = new Dictionary<string, List<string>>()
        {
            {
                "Emiliano Conti" , new List<string>()
                {
                    "Proj1",
                    "Proj2",
                    "Proj3"
                }
            },
            {
                "Guillermo Subiran", new List<string>()
                {
                    "Proj4",
                    "Proj5"
                }

            },
            {
                "Agustin Bonilla", new List<string>()
                {
                    "Proj6",
                    "Proj7",
                    "Proj8",
                    "Proj9",
                    "Proj10",
                    "Proj11"
                }

            }
        };
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            // Get StateClient so that you can save context specific information
            StateClient state = activity.GetStateClient();

            // Get ConnectorClient to enable Sending Messages
            ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));

            string urlText = Uri.EscapeDataString(activity.Text);
            string url = String.Format(LUIS_URL, urlText);
            using(var client = new HttpClient())
            {
                var luisResponse = await client.GetAsync(url);
                if (luisResponse.IsSuccessStatusCode)
                {
                    var content = await luisResponse.Content.ReadAsStringAsync();
                    dynamic resContent = new ExpandoObject();
                    await Task.Factory.StartNew(() => resContent = JsonConvert.DeserializeObject<ExpandoObject>(content));
                    var intents = (IEnumerable<dynamic>)resContent.intents;
                    var expectedIntent = intents.First();
                    if (expectedIntent.intent == "ListProjects")
                    {
                        string entity = resContent.entities[0].entity;
                        var results = this.EmployeeProjectMapping
                            .Where(x => x.Key.ToLower().Contains(entity.ToLower()));
                        var count = results.Count();
                        if (count == 0)
                        {
                            Activity notFoundReply = activity.CreateReply("No matches found!");
                            await connector.Conversations.ReplyToActivityAsync(notFoundReply);
                        }
                        else
                        {
                            Activity countReply = activity.CreateReply($"Found {count} possible matches");
                            await connector.Conversations.ReplyToActivityAsync(countReply);
                            foreach (var res in results)
                            {
                                Activity resultReply = activity.CreateReply($"{res.Key} worked on the following projects: {String.Join(", ", res.Value)}");
                                await connector.Conversations.ReplyToActivityAsync(resultReply);
                            }
                        }
                    }
                    else
                    {
                        Activity replyNoUnderstand = activity.CreateReply("I couldn't do what you asked");
                        await connector.Conversations.ReplyToActivityAsync(replyNoUnderstand);
                    }
                }else
                {
                    Activity replyNoUnderstand = activity.CreateReply(luisResponse.ReasonPhrase);
                    await connector.Conversations.ReplyToActivityAsync(replyNoUnderstand);
                }
            }
            
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }
    }
}