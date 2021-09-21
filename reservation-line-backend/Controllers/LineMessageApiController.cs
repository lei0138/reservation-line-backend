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

        public class FlexMessageReplyType
        {
            [JsonProperty("type")]
            public string type { get; set; }

            [JsonProperty("altText")]
            public string altText { get; set; }

            [JsonProperty("contents")]
            public object contents { get; set; }

            public override string ToString()
            {
                return JsonConvert.SerializeObject(this);
            }
        }

        public class XDateListType
        {
            [JsonProperty("events")]
            public List<XDateType> list { get; set; }
        }

        public class XDateType
        {
            [JsonProperty("Calendar_Date")]
            public DateTime Calendar_Date { get; set; }


            [JsonProperty("Holiday")]
            public int Holiday { get; set; }

            [JsonProperty("Location1_Enable")]
            public int Location1_Enable { get; set; }

            [JsonProperty("Location1_Remaining")]
            public int Location1_Remaining { get; set; }

            [JsonProperty("Location2_Enable")]
            public int Location2_Enable { get; set; }

            [JsonProperty("Location2_Remaining")]
            public int Location2_Remaining { get; set; }

            [JsonProperty("Location3_Enable")]
            public int Location3_Enable { get; set; }

            [JsonProperty("Location3_Remaining")]
            public int Location3_Remaining { get; set; }

            [JsonProperty("Location4_Enable")]
            public int Location4_Enable { get; set; }

            [JsonProperty("Location4_Remaining")]
            public int Location4_Remaining { get; set; }

            [JsonProperty("Location5_Enable")]
            public int Location5_Enable { get; set; }

            [JsonProperty("Location5_Remaining")]
            public int Location5_Remaining { get; set; }
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
                string xml_content = sendRequest($"https://dantaiapidemo.azurewebsites.net/api/srvCalendar/Search2?DtFrom={DateTime.Now.ToString("yyyy/MM/dd")}&DtTo={DateTime.Now.AddDays(7).ToString("yyyy/MM/dd")}");

                List<XDateType> json_content_list = JsonConvert.DeserializeObject<List<XDateType>>(xml_content);

                string msg_content = "{\"type\": \"bubble\",\"header\": {\"type\": \"box\",\"layout\": \"vertical\",\"contents\": [{\"type\": \"text\",\"text\": \"予約日程を選択してください。\",\"color\": \"#46dd69\",\"style\": \"normal\",\"weight\": \"bold\"}]},\"hero\": {\"type\": \"box\",\"layout\": \"vertical\",\"contents\": [{\"type\": \"text\",\"text\": \"" + DateTime.Now.ToString("yyyy/MM/dd") + "~" + DateTime.Now.AddDays(7).ToString("yyyy/MM/dd") + "\",\"offsetStart\": \"20px\",\"size\": \"lg\",\"weight\": \"bold\"}]},\"body\": {\"type\": \"box\",\"layout\": \"vertical\",\"contents\": [";

                int col_index = 0;
                for (int index = 0; index < json_content_list.Count; index++)
                {
                    if (col_index == 0)
                        msg_content += "{\"type\": \"box\",\"layout\": \"horizontal\",\"contents\": [";

                    if (col_index == 1)
                        msg_content += ",";

                    msg_content += "{\"type\": \"box\",\"layout\": \"vertical\",\"contents\": [{\"type\": \"text\",\"text\": \"" + json_content_list[index].Calendar_Date.ToString("MM/dd") + "\",\"align\": \"center\"}],\"backgroundColor\": \"#8fb9eb\",\"paddingTop\": \"10px\",\"paddingBottom\": \"10px\",\"cornerRadius\": \"10px\",\"action\": {\"type\": \"message\",\"label\": \"action\",\"text\": \"" + json_content_list[index].Calendar_Date.ToString("MM/dd") + "\"},\"width\": \"40%\"}";

                    if (col_index == 1 || (col_index == 0 && index == json_content_list.Count-1))
                    {
                        msg_content += "],\"offsetBottom\": \"10px\",\"justifyContent\": \"space-evenly\", \"paddingBottom\": \"10px\"}";

                        if (index != json_content_list.Count - 1)
                            msg_content += ",";
                    }

                    col_index = (col_index + 1) % 2;
                }
                msg_content += "]}}";

                FlexMessageReplyType[] flex_message = new FlexMessageReplyType[1];
                flex_message[0] = new FlexMessageReplyType();
                flex_message[0].type = "flex";
                flex_message[0].contents = JsonConvert.DeserializeObject(msg_content);
                flex_message[0].altText = "Flex Message";
                //TextMessageReplyType[] text_message = new TextMessageReplyType[1];
                //text_message[0] = new TextMessageReplyType();
                //text_message[0].text = "予約時間を選択してください。\r\n 2021/08/10 \r\n=================\r\n 10:30, \r\n 11:30, \r\n 12:30, \r\n 13:30";
                //text_message[0].type = "text";

                SendMessage(new Dictionary<string, object>{
                    { "replyToken", event_message.replyToken},
                    { "messages", flex_message},
                    { "notificationDisabled", false }
                });
            }

            if (message_text.text.Contains("11:30"))
            {
                TextMessageReplyType[] text_message = new TextMessageReplyType[1];
                text_message[0] = new TextMessageReplyType();
                text_message[0].text = "予約情報を確認後、okを入力してください。2021/08/10 11:30 ";
                text_message[0].type = "text";

                SendMessage(new Dictionary<string, object>{
                    { "replyToken", event_message.replyToken},
                    { "messages", text_message},
                    { "notificationDisabled", false }
                });
            }

            if (message_text.text.Contains("ok"))
            {
                TextMessageReplyType[] text_message = new TextMessageReplyType[1];
                text_message[0] = new TextMessageReplyType();
                text_message[0].text = "予約が完了しました。\r\n 2021 /08/10 11:30 ";
                text_message[0].type = "text";

                SendMessage(new Dictionary<string, object>{
                    { "replyToken", event_message.replyToken},
                    { "messages", text_message},
                    { "notificationDisabled", false }
                });
            }
        }

        private string sendRequest(string url)
        {
            HttpWebRequest http_request = (HttpWebRequest)WebRequest.Create(url);
            http_request.Method = HttpMethod.Get.Method;

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
                LogInfo(wex.Message);
            }

            return response_message;
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
                response_message = wex.Message;

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

            LogInfo(response_message);
            return response_message;
        }

    }



}
