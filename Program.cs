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
            Console.WriteLine("Hello! And Welcome To Zern's App Downgrader Please Input Meta Access Token:");
            string accesstoken = Console.ReadLine();
            Console.WriteLine("What is the build id of the app you will be downgrading?:");
            string appbuildid = Console.ReadLine();


            if (!string.IsNullOrWhiteSpace(accesstoken) && !string.IsNullOrWhiteSpace(appbuildid))
            {
                try
                {
                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
                    //i wish i had a better way to display the name of the app but i dont. I might try and make it so it grabs it from oculus db in the future.
                    string downloadUrl = $"https://zerndowngrader.vercel.app/download?build={appbuildid}&token={accesstoken}";
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
                            Console.Write($"\rdownloading: {e.ProgressPercentage}% ({e.BytesReceived / 1024} KB / {e.TotalBytesToReceive / 1024} KB)");
                        };

                        webClient.DownloadFileCompleted += (s, e) =>
                        {
                            Console.WriteLine("\ndownload completed!");
                        };

                        Console.WriteLine("starting download...");
                        webClient.DownloadFileAsync(new Uri(downloadUrl), outputPath);

                        
                        Console.WriteLine("Press any key to exit...");
                        Console.ReadLine();
                    }
                }
                catch (WebException ex)
                {
                    Console.WriteLine($"\nerror: {ex.Status} - {ex.Message}");
                    if (ex.Response is HttpWebResponse response)
                    {
                        Console.WriteLine($"HTTP status code: {(int)response.StatusCode} {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\nGeneral error:  {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("Access token or build ID was empty. If You Dont know how to get meta access tokens please check this site: https://computerelite.github.io/tools/Oculus/ObtainToken.html");
            }
        }
    }
}