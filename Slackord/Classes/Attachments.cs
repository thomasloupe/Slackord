namespace Slackord.Classes
{
    using System.Collections.Generic;
    using Newtonsoft.Json.Linq;

    public class Attachment
    {
        public string Title { get; set; }
        public string Text { get; set; }
        public string ImageUrl { get; set; }
        public string ThumbUrl { get; set; }
    }

    public class AttachmentsParser
    {
        public static List<Attachment> ParseAttachments(JArray slackAttachments)
        {
            var attachmentsList = new List<Attachment>();

            if (slackAttachments != null)
            {
                foreach (JObject slackAttachment in slackAttachments.Cast<JObject>())
                {
                    var attachment = new Attachment
                    {
                        Title = slackAttachment.Value<string>("title"),
                        Text = slackAttachment.Value<string>("text"),
                        ImageUrl = slackAttachment.Value<string>("image_url"),
                        ThumbUrl = slackAttachment.Value<string>("thumb_url")
                    };

                    attachmentsList.Add(attachment);
                }
            }

            return attachmentsList;
        }
    }
}
