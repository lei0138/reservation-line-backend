using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace reservation_line_backend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LineMessageApiController : ControllerBase
    {
        public class WebhookRequestType
        {
            [JsonProperty("destination")]
            public string destination { get; set; }

            [JsonProperty("events")]
            public List<object> event_obj_list { get; set; }
        }
        public class SourceType
        {
            [JsonProperty("type")]
            public string type { get; set; }

            [JsonProperty("userId")]
            public string userId { get; set; }
        }
        public class SourceUserType : SourceType
        {
        }
        public class SourceGroupType : SourceType
        {
            [JsonProperty("groupId")]
            public string groupId { get; set; }
        }
        public class SourceRoomType : SourceType
        {
            [JsonProperty("roomId")]
            public string roomId { get; set; }
        }
        public class EventType
        {
            [JsonProperty("type")]
            public string type { get; set; }

            [JsonProperty("mode")]
            public string mode { get; set; }

            [JsonProperty("timestamp")]
            public long timestamp { get; set; }

            [JsonProperty("source")]
            public SourceType source { get; set; }
        }
        public class EventMessageType: EventType
        {
            [JsonProperty("replyToken")]
            public string replyToken { get; set; }

            [JsonProperty("message")]
            public Object message_obj { get; set; }
        }
        public class MessageType
        {
            [JsonProperty("id")]
            public string id { get; set; }

            [JsonProperty("type")]
            public string type { get; set; }
        }
        public class MessageTextType: MessageType
        {
            [JsonProperty("text")]
            public string text { get; set; }
        }

        public class TextMessageReplyType
        {
            [JsonProperty("type")]
            public string type { get; set; }

            [JsonProperty("text")]
            public string text { get; set; }

            public override string ToString()
            {
                return JsonConvert.SerializeObject(this);
            }
        }




        private readonly ILogger<LineMessageApiController> _logger;
        private readonly string _access_token;
        public LineMessageApiController(ILogger<LineMessageApiController> logger, IConfiguration iconfig)
        {
            _logger = logger;
            _access_token = iconfig.GetSection("ChannelAccessToken").Value;
        }

        private void LogInfo(string txt_log_info)
        {
            string message = string.Format("Time: {0} : {1}", DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt"), txt_log_info);
            message += Environment.NewLine;
            using (StreamWriter writer = new StreamWriter("InfoLog.txt", true))
            {
                writer.WriteLine(message);
                writer.Close();
            }
        }

        [HttpPost]
        public string Post([FromBody] Object request)
        {
            LogInfo(request.ToString());
            JsonElement root_element = JsonDocument.Parse(request.ToString()).RootElement;

            WebhookRequestType webhook_request = JsonConvert.DeserializeObject<WebhookRequestType>(request.ToString());

            foreach (object event_data in webhook_request.event_obj_list)
            {
                JsonElement json_event_data = JsonDocument.Parse(event_data.ToString()).RootElement;
                if (json_event_data.GetProperty("type").ToString().Equals("message"))
                {
                    EventMessageType message_event_data = JsonConvert.DeserializeObject<EventMessageType>(event_data.ToString());

                    JsonElement json_source_type = JsonDocument.Parse(message_event_data.message_obj.ToString()).RootElement;
                    if (json_source_type.GetProperty("type").ToString().Equals("text"))
                    {
                        MessageTextType message_text = JsonConvert.DeserializeObject<MessageTextType>(message_event_data.message_obj.ToString());

                        MessageResponse(webhook_request, message_event_data, message_text);
                    }
                }
            }

            var result = new ActionResult { ErrorCode=0, ErrorMessage="none" };
            return result.ToString();
        }

        private void MessageResponse(WebhookRequestType webhook, EventMessageType event_message, MessageTextType message_text)
        {
            if (message_text.text.Contains("予約"))
            {
                TextMessageReplyType[] text_message = new TextMessageReplyType[1];
                text_message[0] = new TextMessageReplyType();
                text_message[0].text = "Hi, there";
                text_message[0].type = "text";
                
                SendMessage(new Dictionary<string, object>{
                    { "replyToken", event_message.replyToken},
                    { "messages", text_message},
                    { "notificationDisabled", false }
                });
            }
        }

        private string SendMessage(Dictionary<string, object> _params = null)
        {
            HttpWebRequest http_request = (HttpWebRequest)WebRequest.Create("https://api.line.me/v2/bot/message/reply");
            http_request.Method = HttpMethod.Post.Method;
            http_request.ContentType = "application/json";
            http_request.Headers.Add("Authorization", "Bearer {" + _access_token + "}");

            if (_params != null)
            {
                using (var writer = new StreamWriter(http_request.GetRequestStream()))
                    writer.Write(JsonConvert.SerializeObject(_params).ToString());
            }

            string response_message = "";
            try
            {
                using (WebResponse webResponse = http_request.GetResponse())
                {
                    Stream str = webResponse.GetResponseStream();
                    if (str != null)
                    {
                        using (StreamReader sr = new StreamReader(str))
                        {
                            response_message = sr.ReadToEnd();
                        }
                    }
                }
            }
            catch (WebException wex)
            {
                response_message = "[error_message]" + wex.Message;

                try
                {
                    using (HttpWebResponse response = (HttpWebResponse)wex.Response)
                    {
                        Stream str = response.GetResponseStream();
                        if (str == null)
                        {
                            return "";
                        }

                        using (StreamReader sr = new StreamReader(str))
                        {
                            string error_message = sr.ReadToEnd();
                            LogInfo(error_message);
                        }
                    }
                }
                catch (Exception exeception)
                {
                    LogInfo(exeception.Message);
                }
            }

            return response_message;

        }

    }



}
