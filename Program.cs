using System;
using System.Net;
using System.Reflection;

namespace MyApp
{
    internal class Program
    {
        //public string accesstoken;
        static void Main(string[] args)
        {
            Console.WriteLine("Hello! And Welcome To Zern's App Downgrader");
            //i added this bc vercel sucks
            Console.WriteLine("Do you want to use Meta API or Local API (use meta when expecting download longer than 60s)? Type 'meta' or 'local':");
            string apiChoice = Console.ReadLine()?.Trim().ToLower();

            Console.WriteLine("Please input Meta Access Token:");
            string accesstoken = Console.ReadLine();
            Console.WriteLine("What is the build ID of the app you will be downgrading?");
            string appbuildid = Console.ReadLine();

            if (!string.IsNullOrWhiteSpace(accesstoken) && !string.IsNullOrWhiteSpace(appbuildid))
            {
                try
                {
                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;

                    string downloadUrl;

                    if (apiChoice == "meta")
                    {
                        downloadUrl = $"https://securecdn.oculus.com/binaries/download/?id={appbuildid}&access_token={accesstoken}";
                    }
                    else
                    {

                        downloadUrl = $"https://zerndowngrader.vercel.app/download?build={appbuildid}&token={accesstoken}";
                    }

                    string outputPath = Path.Combine(
                        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                        $"Build{appbuildid}.apk"
                    );

                    using (WebClient webClient = new WebClient())
                    {
                        webClient.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                        webClient.Headers.Add(HttpRequestHeader.Accept, "*/*");

                        webClient.DownloadProgressChanged += (s, e) =>
                        {
                            Console.Write($"\rDownloading: {e.ProgressPercentage}% ({e.BytesReceived / 1024} KB / {e.TotalBytesToReceive / 1024} KB)");
                        };

                        webClient.DownloadFileCompleted += (s, e) =>
                        {
                            Console.WriteLine("\nDownload completed!");
                        };

                        Console.WriteLine("Starting download...");
                        webClient.DownloadFileAsync(new Uri(downloadUrl), outputPath);

                        Console.WriteLine("Press any key to exit...");
                        Console.ReadLine();
                    }
                }
                catch (WebException ex)
                {
                    Console.WriteLine($"\nError: {ex.Status} - {ex.Message}");
                    if (ex.Response is HttpWebResponse response)
                    {
                        Console.WriteLine($"HTTP status code: {(int)response.StatusCode} {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\nGeneral error: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("Access token or build ID was empty.");
                Console.WriteLine("If you don’t know how to get Meta access tokens, visit:");
                Console.WriteLine("https://computerelite.github.io/tools/Oculus/ObtainToken.html");
            }
        }
    }
}