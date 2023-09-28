namespace Slackord.Classes
{
    using System.Collections.Generic;
    using Newtonsoft.Json.Linq;

    public class SlackFile
    {
        public string Url { get; set; }
        public string Name { get; set; }
        public string FileType { get; set; }
        public string Title { get; set; }
        public string MimeType { get; set; }
    }

    public class FilesParser
    {
        public static List<SlackFile> ParseFiles(JArray slackFiles)
        {
            var filesList = new List<SlackFile>();

            if (slackFiles != null)
            {
                foreach (JObject slackFile in slackFiles.Cast<JObject>())
                {
                    var file = new SlackFile
                    {
                        Url = slackFile.Value<string>("url_private"),
                        Name = slackFile.Value<string>("name"),
                        FileType = slackFile.Value<string>("filetype"),
                        Title = slackFile.Value<string>("title"),
                        MimeType = slackFile.Value<string>("mimetype")
                    };

                    filesList.Add(file);
                }
            }

            return filesList;
        }
    }
}
