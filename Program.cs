using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace MyApp
{
    internal class Program
    {
        public static string accesstoken = "";
        public static bool usedb = false;
        public static string appbuildid = "";

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

        public static async Task<string> serch()
        {
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

            Console.WriteLine("Enter app name to search (e.g. Gorilla Tag):");
            string searchTerm = Console.ReadLine();
            //oculus db is cool
            string searchUrl = $"https://oculusdb.rui2015.me/api/v1/search/{Uri.EscapeDataString(searchTerm)}?groups=Quest,PCVR,GoAndGearVr";

            try
            {
                HttpResponseMessage response = await client.GetAsync(searchUrl);

                if (!response.IsSuccessStatusCode)
                {
                    //fail gime the code
                    Console.WriteLine($"fail. {(int)response.StatusCode} {response.ReasonPhrase}");
                    return null;
                }

                string searchJson = await response.Content.ReadAsStringAsync();
                if (searchJson.TrimStart().StartsWith("<"))
                {
                    // cloudflare kept throwing this so i added an error for it
                    Console.WriteLine("expected JSON but got HTML (likely Cloudflare being dumb).");
                    return null;
                }

                var results = JsonConvert.DeserializeObject<List<OculusdbSearchResult>>(searchJson);
                if (results == null || results.Count == 0)
                {
                    //no app idiot
                    Console.WriteLine("No apps found.");
                    return null;
                }

                Console.WriteLine("\nSearch Results:");
                for (int i = 0; i < results.Count; i++)
                {
                    //yay it worked here the apps
                    string displayName = string.IsNullOrWhiteSpace(results[i].Name) ? "Unknown Name" : results[i].Name;
                    Console.WriteLine($"{i + 1}. {results[i].DisplayName} (App ID: {results[i].Id})");
                }

                int choice = -1;
                while (true)
                {
                    Console.Write("\nSelect an app **number** to view its versions: ");
                    if (int.TryParse(Console.ReadLine(), out choice) && choice >= 1 && choice <= results.Count)
                        break;
                    Console.WriteLine("Please enter the number next to the app.");
                }

                string appId = results[choice - 1].Id;
                string versionApiUrl = $"https://oculusdb.rui2015.me/api/v1/connected/{appId}";
                string versionJson = await client.GetStringAsync(versionApiUrl);

                if (versionJson.TrimStart().StartsWith("<"))
                {
                    Console.WriteLine("expected JSON but got HTML. The app might be blocked or private.");
                    return null;
                }

                var versionData = JObject.Parse(versionJson);
                var versions = versionData["versions"];
                var apps = versionData["applications"];

                if (versions == null || !versions.Any())
                {
                    Console.WriteLine("nothing found");
                    return null;
                }

                // get app name from parentApplication: displayName
                string appName = apps?[0]?["appName"]?.ToString() ?? "Unknown App";

                Console.WriteLine($"\nVersions for {appName}:");
                foreach (var version in versions)
                {
                    bool downloadable = version["downloadable"]?.ToObject<bool>() ?? false;
                    if (!downloadable)
                        continue;

                    string name = version["version"]?.ToString() ?? "unknown";
                    string buildId = version["id"]?.ToString() ?? "unknown";
                    Console.WriteLine($"- {name} (Build ID: {buildId})");
                }

                Console.Write("\nEnter the **Build ID** of the version you want to download: ");
                return Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"error {ex.Message}");
                return null;
            }
        }




        public class OculusdbSearchResult
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("appName")]
            public string AppName { get; set; }

            public string DisplayName => !string.IsNullOrWhiteSpace(Name)
                                        ? Name
                                        : !string.IsNullOrWhiteSpace(AppName)
                                            ? AppName
                                            : "Unknown Name";
        }


        static async Task Main(string[] args)
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
                // Console.WriteLine("What is the build id of the app you will be downgrading?:");
                // string appbuildid = Console.ReadLine();
                Console.WriteLine("Would You Like To Query Oculus DB? (yes or no)");
                string dborman = Console.ReadLine()?.Trim().ToLower();

                if (dborman == "yes")
                {
                    appbuildid = await serch();
                }
                else
                {
                    Console.WriteLine("Please Enter Build ID:");
                    appbuildid = Console.ReadLine();
                }
                
             

               
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
