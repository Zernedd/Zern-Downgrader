using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text.Json;

namespace MyApp
{
    internal class Program
    {
        public static string accesstoken = "";


        //this prevents users acc sharing the access tokens on screen share or in other methods, it is very easy to decode but its something.
        public static string EncodeToken(string text)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(text);
            return System.Convert.ToBase64String(bytes);
        }
        //this just decodes the base64 nothing interesting.
        public static string DecodeToken(string data)
        {
            var encodedbytes = System.Convert.FromBase64String(data);
            return System.Text.Encoding.UTF8.GetString(encodedbytes);
        }
       

        static void Main(string[] args)
        {
            //gets the path of the save and checks if it exists
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "acctoken.txt");
            if (!File.Exists(path))
            {
                //fallback for the file not existing
                Console.WriteLine("Hello! And Welcome To Zern's App Downgrader Please Input Meta Access Token (this should be saved and used later):");
                accesstoken = Console.ReadLine();




                using (StreamWriter outputFile = new StreamWriter(path))
                {
                    //save our token
                    outputFile.WriteLine(EncodeToken(accesstoken));

                }
            }

            if (File.Exists(path))
            {
                // ok the file exists lets attempt to download the file.
                accesstoken = DecodeToken(File.ReadAllText(path));

                

                //setting build id.
                Console.WriteLine("What is the build id of the app you will be downgrading?:");
                string appbuildid = Console.ReadLine();

                //basic string check so we dont spam meta's api with null req.
                if (!string.IsNullOrWhiteSpace(accesstoken) && !string.IsNullOrWhiteSpace(appbuildid))
                {
                    try
                    {
                        //meta threw a fit without these 2 stupid lines and i honestly dont know why, i dont even know what they do. (and yes these are stolen off stackoverflow)
                        ServicePointManager.Expect100Continue = true;
                        ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
                        //i wish i had a better way to display the name of the app but i dont. I might try and make it so it grabs it from oculus db in the future.
                        string downloadUrl = $"https://securecdn.oculus.com/binaries/download/?id={appbuildid}&access_token={accesstoken}";
                        string outputPath = Path.Combine(
                            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                            $"Build{appbuildid}.apk"
                        );
                        //make the webclient before making the call idiot
                        using (WebClient webClient = new WebClient())
                        {
                            //again meta threw a hissy fit without the user agent being spoofed so lets do that.
                            webClient.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                            webClient.Headers.Add(HttpRequestHeader.Accept, "*/*");
                            // progress bar so the app doesnt look frozen
                            webClient.DownloadProgressChanged += (s, e) =>
                            {
                                Console.Write($"\rdownloading: {e.ProgressPercentage}% ({e.BytesReceived / 1024} KB / {e.TotalBytesToReceive / 1024} KB)");
                            };
                            //ok its "done" how did we do.
                            webClient.DownloadFileCompleted += (s, e) =>
                            {
                                Console.WriteLine(); 

                                if (e.Error != null)
                                {
                                    //ok, it failed but why?
                                    Console.WriteLine($"Download failed: {e.Error.Message}");

                                    if (e.Error is WebException webEx && webEx.Response is HttpWebResponse response)
                                    {
                                        Console.WriteLine($"HTTP status code: {(int)response.StatusCode} {response.StatusCode}");

                                        if (response.StatusCode == HttpStatusCode.NotFound)
                                        {
                                            Console.WriteLine("Error: The requested build ID does not exist");
                                            File.Delete(path); //delete token bc it prolly no worky
                                        }
                                    }
                                }
                                else if (e.Cancelled)
                                {
                                    //someone killed the download prob the remote server.
                                    Console.WriteLine("Download failed.");
                                }
                                else
                                {
                                    //yay this somehow worked.
                                    Console.WriteLine("Download completed successfully.");
                                }

                                Console.WriteLine("Press any key to exit...");
                                Console.ReadLine();
                            };
                            //download the file.
                            Console.WriteLine("Starting download...");
                            webClient.DownloadFileAsync(new Uri(downloadUrl), outputPath);
                        }

                       
                        Console.ReadLine();
                    }
                    catch (Exception ex)
                    {
                        //error....uhh nothen rly here
                        Console.WriteLine($"\nGeneral error: {ex.Message}");
                    }
                }
                else
                {
                    // ok something is very wrong. throw an error and delete the text file to rule everything out.
                    File.Delete(path);
                    Console.WriteLine("Access token or build ID was empty. If you don't know how to get Meta access tokens, visit: https://computerelite.github.io/tools/Oculus/ObtainToken.html");
                }
            }
        }
    }
}
