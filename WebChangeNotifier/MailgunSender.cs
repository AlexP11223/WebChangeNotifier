using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RestSharp;
using RestSharp.Authenticators;
using static WebChangeNotifier.Logger;

namespace WebChangeNotifier
{
    public class MailAttachment
    {
        public MailAttachment(string fileName, byte[] content)
        {
            FileName = fileName;
            Content = content;
        }

        public MailAttachment(string fileName, string content)
            : this(fileName, Encoding.UTF8.GetBytes(content))
        {
        }

        public string FileName { get; }
        public byte[] Content { get; }
    }

    public class MailgunSender
    {
        private readonly MailgunSettings _emailSettings;

        public MailgunSender(MailgunSettings emailSettings)
        {
            _emailSettings = emailSettings;
        }

        public void Send(string text, IEnumerable<MailAttachment> files = null)
        {
            Log("Sending email: " + text);

            var client = new RestClient
            {
                BaseUrl = new Uri("https://api.mailgun.net/v3"),
                Authenticator = new HttpBasicAuthenticator("api", _emailSettings.ApiKey)
            };
            var request = new RestRequest();
            request.AddParameter("domain", _emailSettings.Domain, ParameterType.UrlSegment);
            request.Resource = "{domain}/messages";
            request.AddParameter("from", _emailSettings.From);
            request.AddParameter("to", _emailSettings.To);
            request.AddParameter("subject", "WebChangeNotifier");
            request.AddParameter("text", text);
            if (files != null)
            {
                request.Files.AddRange(files.Select(f => FileParameter.Create("attachment", f.Content, f.FileName)));
            }
            request.Method = Method.POST;

            var response = client.Execute(request);

            Log(response.Content);
        }
    }
}
