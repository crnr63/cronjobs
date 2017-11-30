using ConsoleApp1;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace GmailQuickstart
{
    class Program
    {
        // If modifying these scopes, delete your previously saved credentials
        // at ~/.credentials/gmail-dotnet-quickstart.json
        static string[] Scopes = { GmailService.Scope.GmailReadonly };
        static string ApplicationName = "Gmail API .NET Quickstart";

        static void Main(string[] args)
        {
            UserCredential credential;

            using (var stream =
                new FileStream("client_secret.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = System.Environment.GetFolderPath(
                    System.Environment.SpecialFolder.Personal);
                credPath = Path.Combine(credPath, ".credentials/gmail-dotnet-quickstart.json");

                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);
            }

            // Create Gmail API service.
            var service = new GmailService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            // Define parameters of request.
            List<Message> result = new List<Message>();
            UsersResource.MessagesResource.ListRequest emailListRequest = service.Users.Messages.List("me");
            emailListRequest.LabelIds = "Label_1";
            //request.Q = "labelIds:[\"Label_1\"]";

            do
            {
                try
                {
                    ListMessagesResponse response = emailListRequest.Execute();

                    result.AddRange(response.Messages);
                    emailListRequest.PageToken = response.NextPageToken;


                    Console.WriteLine(response.Messages.Count);
                    foreach (var email in response.Messages)
                    {

                        var emailInfoRequest = service.Users.Messages.Get("me", email.Id);
                       
                        //make another request for that email id...
                        var emailInfoResponse = emailInfoRequest.Execute();

                        if (emailInfoResponse != null)
                        {
                            String from = "";
                            String date = "";
                            String subject = "";
                            String body = "";
                            //loop through the headers and get the fields we need...
                            foreach (var mParts in emailInfoResponse.Payload.Headers)
                            {
                                if (mParts.Name == "Date")
                                {
                                    date = mParts.Value;
                                }
                                else if (mParts.Name == "From")
                                {
                                    from = mParts.Value;
                                }
                                else if (mParts.Name == "Subject")
                                {
                                    subject = mParts.Value;
                                }

                                if (date != "" && from != "")
                                {
                                    if (emailInfoResponse.Payload.Parts == null && emailInfoResponse.Payload.Body != null)
                                    {
                                        body = emailInfoResponse.Payload.Body.Data;
                                    }
                                    else
                                    {
                                        body = getNestedParts(emailInfoResponse.Payload.Parts, "");
                                    }
                                    //need to replace some characters as the data for the email's body is base64
                                  
                                   

                               
                                

                                    // var htmlNodes = doc.DocumentNode.SelectNodes("//body/table");

                                    //  foreach (var node in htmlNodes)
                                    //  {

                                    //      Console.WriteLine(node.Attributes["value"].Value);
                                    //     }
                                    //now you have the data you want....

                                }

                            }

                            String codedBody = body.Replace("-", "+");
                            codedBody = codedBody.Replace("_", "/");
                            codedBody = codedBody.Replace("=", "/");
                            byte[] data = Convert.FromBase64String(codedBody);
                            body = Encoding.UTF8.GetString(data);
                            var doc = new HtmlDocument();
                            doc.LoadHtml(body);
                            
                            var htmlBody = doc.DocumentNode.SelectSingleNode("//body");

                            var htmlNodes = htmlBody.ChildNodes;
                            // Console.WriteLine(htmlNodes.IsReadOnly);

                            List<House> blue = GetTopSpeed(htmlNodes);
                        }




                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("An error occurred: " + e.Message);
                }
            } while (!String.IsNullOrEmpty(emailListRequest.PageToken));
            // List labels.



            Console.Read();

        }

        static List<House> GetTopSpeed(HtmlNodeCollection nodes)
        {

            List <House> stringList = new List<House> {};
            
            foreach (var node in nodes)
            {

                //Console.WriteLine(node.InnerText);
                string[] lines = node.InnerText.Split(
                         new[] { "\r\n", "\r", "\n" },
                            StringSplitOptions.None
                                );
               
                var blue = lines.Where(x => x != "");
                Regex Thismoney = new Regex(@"^\-?\(?\$?\s*\-?\s*\(?(((\d{1,3}((\,\d{3})*|\d*))?(\.\d{1,4})?)|((\d{1,3}((\,\d{3})*|\d*))(\.\d{0,4})?))\)?$");
                string[] blues = blue.ToArray();
                Regex auction = new Regex(@"^\bfrom\b\s*(\-?\$?\s*\-?\s*((((\d{1,3}((\,\d{3})*|\d*))?(\.\d{1,4})?)|((\d{1,3}((\,\d{3})*|\d*))(\.\d{0,4})?)))\)?)$");
                List<string> stats = blues.Where(x => x.Contains("sqft")|| x.Contains("bd")|| x.Contains("studio") ).ToList<string>();

                List<string> addresses = blues.Where(x => x.Contains("&zwnj")).Distinct().ToList<string>(); ;
                List<string> costs = blues.Where(x => x.Contains("Price Unknown")||Thismoney.IsMatch(x) || auction.IsMatch(x)).ToList<string>(); ;
                //List<string> textLines = new List<string>();
                if (stats.Count > 0 && addresses.Count>0 && costs.Count>0) {
                    Console.WriteLine("{0}.{1}.{2}", stats.Count, addresses.Count, costs.Count);
                                       for (int i = 0; i <= stats.Count-1; i++) {
          stringList.Add(new House { address = addresses[i], cost = costs[i], stats = stats[i] });
                        Console.WriteLine("{0}.{1}.{2}", stats[i], addresses[i], costs[i]); 
                    
                   }  
                }
                else {  }
             //   foreach ( var cost in addresses)
            //    {

               //     Console.WriteLine(cost);
             //   }
               


            }
            Console.WriteLine("called");
            Console.WriteLine(stringList.Count);
            return stringList;
        }
        static String getNestedParts(IList<MessagePart> part, string curr)
        {
            string str = curr;
            if (part == null)
            {
                return str;
            }
            else
            {
                foreach (var parts in part)
                {
                    if (parts.Parts == null)
                    {
                        if (parts.Body != null && parts.Body.Data != null)
                        {
                            str += parts.Body.Data;
                        }
                    }
                    else
                    {
                        return getNestedParts(parts.Parts, str);
                    }
                }

                return str;
            }


        }

    }
    }  