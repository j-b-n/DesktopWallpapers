using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Windows.Forms;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace DesktopWallpapers
{
    static class Program
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]

        private static extern int SystemParametersInfo(int uAction,
            int uParam, string lpvParam, int fuWinIni);

        //Const ints that are commands to the above functions
        private static readonly int SPI_SETDESKWALLPAPER = 0x14;
        private static readonly int SPIF_UPDATEINIFILE = 0x01;
        private static readonly int SPIF_SENDWININICHANGE = 0x02;

        private static readonly ILoggerFactory LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddSimpleConsole(options =>
            {
                options.SingleLine = true;
                options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ";
            });
        });
        public static readonly AppLogger Log = new AppLogger(LoggerFactory.CreateLogger("DesktopWallpapers.Program"));
        private static readonly HttpClient HttpClient = CreateHttpClient();
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            IncludeFields = true
        };
        private const string BingBaseUrl = "https://www.bing.com";
        private const string BingMetadataUrl = "https://www.bing.com/HPImageArchive.aspx?format=js&idx=0&n=1&mkt=sv-SE";


        public static List<BingImage> BingImages = new List<BingImage>();

        public static string AppDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\j-b-n\\DesktopWallpaper\\";
        public static string SettingsFile = AppDir + "settings.jsn";

        public static MySettings settings = MySettings.Load(SettingsFile);

        public static bool BingMetadataUpdate = false;

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
                "AppleWebKit/537.36 (KHTML, like Gecko) " +
                "Chrome/126.0.0.0 Safari/537.36 Edg/126.0.0.0");
            return client;
        }

        private static bool DownloadFileToPath(string url, string destination)
        {
            try
            {
                var data = HttpClient.GetByteArrayAsync(url).GetAwaiter().GetResult();
                File.WriteAllBytes(destination, data);
                return true;
            }
            catch (Exception e)
            {
                Log.Error("Failed to download file: " + e.Message);
                return false;
            }
        }

        private static bool TryDeleteFileWithRetry(string filePath, int maxAttempts, int delayMilliseconds)
        {
            if (!File.Exists(filePath))
            {
                return true;
            }

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    File.Delete(filePath);
                    return true;
                }
                catch (IOException)
                {
                    if (attempt == maxAttempts)
                    {
                        return false;
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    if (attempt == maxAttempts)
                    {
                        return false;
                    }
                }

                System.Threading.Thread.Sleep(delayMilliseconds);
            }

            return false;
        }


        ///CommandLine stuff
        [DllImport("kernel32.dll")]
        static extern bool AttachConsole(int dwProcessId);
        private const int ATTACH_PARENT_PROCESS = -1;

        #region CommandLineParams
        class Options
        {
            public bool SetWallpaper { get; set; }

            public bool SetMediaportal { get; set; }

            public bool Force { get; set; }

            public bool Verbose { get; set; }
        }
        #endregion


        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Log.SetLogFilePath(Path.Combine(AppDir, "DesktopWallpapers.log"));

            Log.Debug("Application Starting");
            Log.Debug("Appdir:" + AppDir);

            if (args == null || args.Length == 0)
            {
                Log.Debug("Windows Form mode");
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Form1());
            }
            else
            {
                Log.Debug("Command Line running!");
                // redirect console output to parent process;
                // must be before any calls to Console.WriteLine()
                AttachConsole(ATTACH_PARENT_PROCESS);

                if (TryParseOptions(args, out Options options))
                {
                    RunCLI(options);
                }
                else
                {
                    PrintUsage();
                }

            }
            Log.Debug("Application closing");
        }

        private static bool TryParseOptions(string[] args, out Options options)
        {
            options = new Options();
            foreach (string arg in args)
            {
                switch (arg)
                {
                    case "-s":
                    case "--setwallpaper":
                        options.SetWallpaper = true;
                        break;
                    case "-m":
                    case "--mediaportal":
                        options.SetMediaportal = true;
                        break;
                    case "-f":
                    case "--force":
                        options.Force = true;
                        break;
                    case "-v":
                    case "--verbose":
                        options.Verbose = true;
                        break;
                    case "-h":
                    case "--help":
                        return false;
                    default:
                        Log.Error("Unknown argument: " + arg);
                        return false;
                }
            }

            return true;
        }

        private static void PrintUsage()
        {
            Console.WriteLine("DesktopWallpapers CLI usage:");
            Console.WriteLine("  -s, --setwallpaper   Set current image as wallpaper");
            Console.WriteLine("  -m, --mediaportal    Save image for MediaPortal files");
            Console.WriteLine("  -f, --force          Force metadata/image refresh");
            Console.WriteLine("  -v, --verbose        Enable verbose logging");
            Console.WriteLine("  -h, --help           Show this help");
        }

        static int RunCLI(Options options)
        {
            // consume values here
            if (options.Verbose) Log.Debug("Verbose operations!");

            if (options.Force)
            {
                refreshBing(true);
            }
            else
            {
                refreshBing(false);
            }

            //if (options.Verbose) Console.WriteLine("Verbose!");
            if (options.SetWallpaper)
            {
                if (options.Verbose) Log.Debug("Setting wallpaper!");
                SetCurrentImageAsWallpaper();
            }

            if (options.SetMediaportal)
            {
                if (options.Verbose) Log.Debug("Use wallpaper in Mediaportal!");
                SaveMPFiles();
            }

            return 1;
        }

        public static void LoadBingMetadata()
        {
            BingMetadataUpdate = false;
            BingImages.Clear();

            if (File.Exists(settings.LocalMetadataFilename))
            {
                string value = File.ReadAllText(settings.LocalMetadataFilename);
                try
                {
                    value = value.TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
                    if (!value.StartsWith("{", StringComparison.Ordinal))
                    {
                        Log.Error("Bing metadata är inte JSON. Ignorerar svaret.");
                        BingMetadataUpdate = true;
                        return;
                    }

                    var root = JsonSerializer.Deserialize<BingApiResponse>(value, JsonOptions);
                    if (root == null || root.images == null || root.images.Count == 0)
                    {
                        Log.Error("Bing JSON saknar bilddata.");
                        BingMetadataUpdate = true;
                        return;
                    }

                    foreach (BingApiImage node in root.images)
                    {
                        BingImage BI = new BingImage();
                        BI.url = node.url;
                        BI.copyright = node.copyright;
                        BI.startdate = node.startdate;
                        BI.fullstartdate = node.fullstartdate;
                        BI.enddate = node.enddate;
                        BingImages.Add(BI);

                        DateTime dt = DateTime.Now;
                        try
                        {
                            dt = DateTime.ParseExact(BI.fullstartdate, "yyyyMMddHHmm",
                                System.Globalization.CultureInfo.InvariantCulture);
                        }
                        catch (Exception e)
                        {
                            Log.Error("Error: " + e.Message);
                        }

                        if (settings.BingFullstarttime != dt)
                        {
                            settings.BingFullstarttime = dt;                      
                            BingMetadataUpdate = true;
                        }

                        dt = DateTime.Now;
                        try
                        {
                            dt = DateTime.ParseExact(BI.enddate, "yyyyMMdd",
                                System.Globalization.CultureInfo.InvariantCulture);
                        }
                        catch (Exception e)
                        {
                            Log.Error("Error: " + e.Message);
                        }

                        if (settings.BingEnddate != dt)
                        {
                            settings.BingEnddate = dt;
                            BingMetadataUpdate = true;
                        }

                        if(dt < DateTime.Now)
                        {
                            Log.Info("Reload metadata file due to passed enddate!");
                            BingMetadataUpdate = true;
                        }

                        if (settings.BingURL != BI.url)
                        {
                            settings.BingURL = BI.url;                            
                            BingMetadataUpdate = true;
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error("Kan inte hämta eller tolka Bing JSON: " + e.Message);
                    BingMetadataUpdate = true;
                    return;
                }
            }
            else
            {
                BingMetadataUpdate = true;
            }
        }

        public static void ClearCache()
        {
            BingMetadataUpdate = true;
            string LocalImage = settings.LocalImageFilename;
            string newLocalImage = settings.LocalImageFilename + ".png";            

            if (!TryDeleteFileWithRetry(settings.LocalMetadataFilename, 3, 100))
            {
                Log.Error("Can't delete metadata file (locked): " + settings.LocalMetadataFilename);
            }

            if (!TryDeleteFileWithRetry(LocalImage, 3, 100))
            {
                Log.Error("Can't delete image file (locked): " + LocalImage);
            }

            if (!TryDeleteFileWithRetry(newLocalImage, 3, 100))
            {
                Log.Error("Can't delete PNG image file (locked): " + newLocalImage);
            }
        }


        public static bool DownloadBingMetadata()
        {            
            int attempt = 1;
            bool success = false;

            while (!success && attempt<11)
            {
                Log.Debug("Downloading new Bing JSON - attempt: " + attempt);
                if (!DownloadFileToPath(BingMetadataUrl, settings.LocalMetadataFilename))
                {
                    attempt = attempt + 1;
                    continue;
                }

                if (File.Exists(settings.LocalMetadataFilename))
                {
                    FileInfo f = new FileInfo(settings.LocalMetadataFilename);

                    if (f.Length > 1)
                    {
                        string body = File.ReadAllText(settings.LocalMetadataFilename).TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
                        if (!body.StartsWith("{", StringComparison.Ordinal))
                        {
                            Log.Error("Bing returnerade inte JSON (möjligen HTML-felsida). Försöker igen.");
                            success = false;
                            attempt = attempt + 1;
                            continue;
                        }

                        success = true;
                        Log.Debug("Downloaded JSON filesize: " + f.Length.ToString());
                        Log.Info("Downloaded Bing JSON file");
                        return true;
                    }
                    else
                    {
                        success = false;
                        Log.Debug("Filesize <= 1");
                    }
                }
                attempt = attempt + 1;                
            }
            return false;
        }

/// <summary>
/// Download image from url
/// </summary>
/// <param name="url">URL to image</param>
/// 
        public static void DownloadImage(string url)
        {
            string LocalImage = settings.LocalImageFilename;
            string newLocalImage = settings.LocalImageFilename + ".png";
            string tempLocalImage = settings.LocalImageFilename + ".tmp";
            string downloadTarget = LocalImage;

            Log.Info("Save image from "+url+" to " + LocalImage);

            if (!TryDeleteFileWithRetry(LocalImage, 3, 100))
            {
                Log.Info("Local image file is locked, using temporary download target.");
                downloadTarget = tempLocalImage;
                if (!TryDeleteFileWithRetry(tempLocalImage, 3, 100))
                {
                    Log.Error("Can't prepare temporary file for download: " + tempLocalImage);
                    return;
                }
            }

            if (!TryDeleteFileWithRetry(newLocalImage, 3, 100))
            {
                Log.Error("Can't replace image file (locked): " + newLocalImage);
                return;
            }

            if (!DownloadFileToPath(url, downloadTarget))
            {
                Log.Error("Can't download file \""+url+"\" or save it locally to \""+LocalImage+"\".");
                return;
            }

            try
            {
                using (var img = Image.FromFile(downloadTarget))
                {
                    img.Save(newLocalImage, ImageFormat.Png);
                }
            }
            catch (Exception e)
            {
                Log.Error("Can't convert downloaded image to PNG: " + e.Message);
                return;
            }
            finally
            {
                if (downloadTarget == tempLocalImage)
                {
                    TryDeleteFileWithRetry(tempLocalImage, 3, 100);
                }
            }
        }

        public static void refreshBing(bool force)
        {
            // First check if we need to refresh Bing metadata.
            LoadBingMetadata();

            if (force || BingMetadataUpdate)
            {
                DownloadBingMetadata();
                LoadBingMetadata();
                DesktopWallpapers.Program.settings.Save(DesktopWallpapers.Program.SettingsFile);

                foreach (DesktopWallpapers.Program.BingImage BI in DesktopWallpapers.Program.BingImages)
                {
                    if (BI.url != null)
                    {
                        DownloadImage(BingBaseUrl + BI.url);
                    }
                }
            }
        }

        public static string getLocalImageDate()
        {
            if (File.Exists(settings.LocalImageFilename))
            {
                DateTime dt = File.GetLastWriteTime(settings.LocalImageFilename);
                return dt.ToShortDateString() + " "+ dt.ToShortTimeString();
            }
            return "No file found!";
        }

        public static Image FromFile(string path)
        {
            var bytes = File.ReadAllBytes(path);
            var ms = new MemoryStream(bytes);
            var img = Image.FromStream(ms);
            return img;
        }

        public static void SaveMPFiles()
        {            
            if (File.Exists(DesktopWallpapers.Program.settings.LocalImageFilename))
            {
                Image img = FromFile(DesktopWallpapers.Program.settings.LocalImageFilename);
                foreach (string fileName in settings.MPFiles)
                {
                    if (File.Exists(fileName))
                        File.Delete(fileName);
                    img.Save(fileName, ImageFormat.Png);
                    Log.Info("Save MPFile: " + fileName);
                }
            }
        }

/// <summary>
/// Set the currently downloaded Image as Wallpaper
/// </summary>
        public static void SetCurrentImageAsWallpaper()
        {            
            Log.Debug("Setting wallpaper!");
            if (File.Exists(settings.LocalImageFilename + ".png"))
                SetWallpaper(settings.LocalImageFilename + ".png");                      
        }

///<summary>
/// Function to set the wallpaper given a location
/// </summary>
///<parameter>location of the file</parameter>
        static void SetWallpaper(string location)
        {
            const string userRoot = "HKEY_CURRENT_USER";
            const string subkey = @"Control Panel\Desktop\";
            const string keyName = userRoot + "\\" + subkey;
            
            Log.Debug("Setting wallpaper in registry");

            Microsoft.Win32.Registry.SetValue(keyName, "Wallpaper", location);

            SystemParametersInfo(SPI_SETDESKWALLPAPER,
            0,
            location,
            SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);
        }

        ///<summary>
        /// Function to set the wallpaper given a location
        /// </summary>
        ///<parameter>location of the file</parameter>
        public static string GetWallpaper()
        {
            const string userRoot = "HKEY_CURRENT_USER";
            const string subkey = @"Control Panel\Desktop";
            const string keyName = userRoot + "\\" + subkey;

            string str = (string) Microsoft.Win32.Registry.GetValue(keyName, "Wallpaper", null);
            Log.Debug("GetWallpaper: "+str);
            return str;
        }


        ///<summary>
        /// Function to clear the wallpaper
        /// </summary>        
        static void ClearWallpaper()
        {
            const string userRoot = "HKEY_CURRENT_USER";
            const string subkey = @"Control Panel\Desktop\";
            const string keyName = userRoot + "\\" + subkey;

            Log.Debug("Clear wallpaper setting in registry");

            Microsoft.Win32.Registry.SetValue(keyName, "Wallpaper", "");

            SystemParametersInfo(SPI_SETDESKWALLPAPER,
            0,
            "",
            SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);
        }        

#region BingImage
        public class BingImage
        {
            public string url;
            public string copyright;
            public string startdate;
            public string fullstartdate;
            public string enddate;            
        }

        public class BingApiResponse
        {
            public List<BingApiImage> images { get; set; }
        }

        public class BingApiImage
        {
            public string url { get; set; }
            public string copyright { get; set; }
            public string startdate { get; set; }
            public string fullstartdate { get; set; }
            public string enddate { get; set; }
        }
#endregion

#region Settings
        public class MySettings : AppSettings<MySettings>
        {
            public static string PathToImages = AppDir;
            public string LocalMetadataFilename = string.Format(@"{0}{1}", PathToImages, "BingMetadata.json");
            public string LocalImageFilename = string.Format(@"{0}{1}", PathToImages, "backgroundImage");
            public DateTime BingFullstarttime;
            public DateTime BingEnddate;
            public string BingURL = "";
            public List<string> MPFiles = new List<string>(); 
        }

        public class AppSettings<T> where T : new()
        {
            private const string DEFAULT_FILENAME = "settings.jsn";
           
            public void Save(string fileName = DEFAULT_FILENAME)
            {
                string path = Path.GetDirectoryName(fileName);
                Directory.CreateDirectory(path);             
                File.WriteAllText(fileName, JsonSerializer.Serialize(this, JsonOptions));
            }

            public static void Save(T pSettings, string fileName = DEFAULT_FILENAME)
            {
                File.WriteAllText(fileName, JsonSerializer.Serialize(pSettings, JsonOptions));
            }

            public static T Load(string fileName = DEFAULT_FILENAME)
            {                
                T t = new T();
                if (File.Exists(fileName))
                    t = JsonSerializer.Deserialize<T>(File.ReadAllText(fileName), JsonOptions) ?? new T();
                return t;
            }

        }


#endregion

        public sealed class AppLogger
        {
            private readonly ILogger _logger;
            private readonly object _fileLock = new object();
            private string _logFilePath;
            private const long MaxLogFileBytes = 1024 * 1024;
            private const int MaxLogBackups = 5;

            public AppLogger(ILogger logger)
            {
                _logger = logger;
            }

            public void SetLogFilePath(string path)
            {
                _logFilePath = path;
                try
                {
                    var directory = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                }
                catch
                {
                    // Ignore file logging setup errors; console logging still works.
                }
            }

            public void Debug(string message)
            {
                _logger.LogDebug(message);
                WriteToFile("DEBUG", message);
            }

            public void Info(string message)
            {
                _logger.LogInformation(message);
                WriteToFile("INFO", message);
            }

            public void Error(string message)
            {
                _logger.LogError(message);
                WriteToFile("ERROR", message);
            }

            private void WriteToFile(string level, string message)
            {
                if (string.IsNullOrWhiteSpace(_logFilePath))
                {
                    return;
                }

                string line = string.Format("{0:yyyy-MM-dd HH:mm:ss,fff} [{1}] {2}\r\n", DateTime.Now, level, message);
                try
                {
                    lock (_fileLock)
                    {
                        RotateIfNeeded();
                        File.AppendAllText(_logFilePath, line);
                    }
                }
                catch
                {
                    // Never throw from logging.
                }
            }

            private void RotateIfNeeded()
            {
                if (!File.Exists(_logFilePath))
                {
                    return;
                }

                var info = new FileInfo(_logFilePath);
                if (info.Length < MaxLogFileBytes)
                {
                    return;
                }

                string oldestBackup = _logFilePath + "." + MaxLogBackups;
                if (File.Exists(oldestBackup))
                {
                    File.Delete(oldestBackup);
                }

                for (int i = MaxLogBackups - 1; i >= 1; i--)
                {
                    string source = _logFilePath + "." + i;
                    string target = _logFilePath + "." + (i + 1);
                    if (File.Exists(source))
                    {
                        File.Move(source, target);
                    }
                }

                File.Move(_logFilePath, _logFilePath + ".1");
            }
        }
    }
}
