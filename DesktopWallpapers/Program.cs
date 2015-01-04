using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using log4net;
using System.Xml;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using CommandLine;
using CommandLine.Text;
using Newtonsoft.Json;
using log4net.Appender;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]

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

        private static readonly int CacheTime = 360;

        public static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);        

       
        public static List<BingImage> BingImages = new List<BingImage>();

        public static string AppDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)+"\\j-b-n\\DesktopWallpaper\\";
        public static string SettingsFile = AppDir + "settings.jsn";

        public static MySettings settings = MySettings.Load(SettingsFile);

        public static bool BingXMLUpdate = false;


        ///CommandLine stuff
        [DllImport( "kernel32.dll" )]
        static extern bool AttachConsole(int dwProcessId);
        private const int ATTACH_PARENT_PROCESS = -1;

#region CommandLineParams
        class Options
        {
            [Option('s', "setwallpaper", DefaultValue = false, HelpText = "Set wallpaper!")]
            public bool SetWallpaper { get; set; }

            [Option('m', "mediaportal", DefaultValue = false, HelpText = "Set wallpaper in MediaPortal!")]
            public bool SetMediaportal { get; set; }

            [Option('f', "force", DefaultValue = false, HelpText = "Force!")]
            public bool Force { get; set; }

            [Option('v', "verbose", DefaultValue = false, HelpText = "Prints all messages to standard output.")]
            public bool Verbose { get; set; }

            [HelpOption]
            public string GetUsage()
            {
                return HelpText.AutoBuild(this,
                  (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
            }
        }      
#endregion


        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            //Configure log4net!
            log4net.Config.XmlConfigurator.Configure();

            var fileAppender = LogManager.GetRepository()
                             .GetAppenders()
                             .OfType<FileAppender>()
                             .FirstOrDefault(fa => fa.Name == "RollingFileAppender");
            
            if (fileAppender != null)
            {
                fileAppender.File = Path.Combine(AppDir, "DesktopWallpapers.log");
                fileAppender.ActivateOptions();
            }

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

                Options options = new Options();
                
                if (CommandLine.Parser.Default.ParseArguments(args, options))
                {
                    // consume values here
                    if (options.Verbose) Log.Debug("Verbose operations!");

                    if (options.Force)
                    {
                        refreshBing(true);
                    } else
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
                } else
                {
                    Console.WriteLine();
                //     Console.WriteLine(options.GetUsage());
                    Log.Debug("Failed to parse command line parameters!");
                }
            }
            Log.Debug("Application closing");
        }

        public static void LoadXML()
        {
            BingXMLUpdate = false;
            BingImages.Clear();

            if (File.Exists(settings.LocalXmlFilename))
            {
                string value = File.ReadAllText(settings.LocalXmlFilename);                
                XmlDocument doc = new XmlDocument();
                try
                {          
                    doc.LoadXml(value);
                    System.Xml.XmlNodeList NodeList = doc.GetElementsByTagName("image");                    
                    foreach (XmlNode node in NodeList)
                    {
                        BingImage BI = new BingImage();
                        BI.url = node["url"].InnerText;
                        BI.copyright = System.Text.RegularExpressions.Regex.Unescape(node["copyright"].InnerText);
                        BI.startdate = node["startdate"].InnerText;
                        BI.fullstartdate = node["fullstartdate"].InnerText;
                        BI.enddate = node["enddate"].InnerText;
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
                            BingXMLUpdate = true;
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
                            BingXMLUpdate = true;
                        }

                        if(dt < DateTime.Now)
                        {
                            Log.Info("Reload XML-file due to passed enddate!");
                            BingXMLUpdate = true;
                        }

                        if (settings.BingURL != BI.url)
                        {
                            settings.BingURL = BI.url;                            
                            BingXMLUpdate = true;
                        }
                    }
                }
                catch (XmlException e)
                {
                    Log.Error("Can't download or parse Bing XML-file: " + e.Message);
                    BingXMLUpdate = true;
                    return;
                }
            }
            else
            {
                BingXMLUpdate = true;
            }
        }

        public static void ClearCache()
        {
            BingXMLUpdate = true;
            string LocalImage = settings.LocalImageFilename;
            string newLocalImage = settings.LocalImageFilename + ".png";            

            if (File.Exists(settings.LocalXmlFilename))
                File.Delete(settings.LocalXmlFilename);

            if (File.Exists(LocalImage))
                File.Delete(LocalImage);

            if (File.Exists(newLocalImage))
                File.Delete(newLocalImage);
        }


        public static bool DownloadBingXML()
        {            
            int attempt = 1;
            bool success = false;

            while (!success && attempt<11)
            {
                Log.Debug("Downloading new Bing XML - attempt: " + attempt);
                WebClient client = new WebClient();
                client.Headers["User-Agent"] =
                        "Mozilla/4.0 (Compatible; Windows NT 5.1; MSIE 6.0) " +
                        "(compatible; MSIE 6.0; Windows NT 5.1; " +
                        ".NET CLR 1.1.4322; .NET CLR 2.0.50727)";

                // Download XML from Bing.
                try
                {
                    client.DownloadFile("http://www.bing.com/HPImageArchive.aspx?format=xml&idx=0&n=1&mkt=en-US", settings.LocalXmlFilename);
                }
                catch (WebException e)
                {
                    Log.Error("Failed to download Bing XML: " + e.Message);
                }

                if (File.Exists(settings.LocalXmlFilename))
                {
                    FileInfo f = new FileInfo(settings.LocalXmlFilename);

                    if (f.Length > 1)
                    {
                        success = true;
                        Log.Debug("Downloaded XML fiesize: " + f.Length.ToString());
                        Log.Info("Downloaded XML-file");
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

            WebClient client = new WebClient();
            Log.Info("Save image from "+url+" to " + LocalImage);

            if (File.Exists(LocalImage))
                File.Delete(LocalImage);

            if (File.Exists(newLocalImage))
                File.Delete(newLocalImage);

            try
            {
                client.DownloadFile(url, LocalImage);
            }
            catch (WebException e)
            {
                Log.Error("Can't download file \""+url+"\" or save it locally to \""+LocalImage+
                    "\": "+e.Message+"\n"+e.InnerException);                          
            }
            
            var img = Image.FromFile(LocalImage);
            img.Save(newLocalImage, ImageFormat.Png);
        }

        public static void refreshBing(bool force)
        {
            //First check if we need to refresh the Bing XML-file
            LoadXML();

            if (force || BingXMLUpdate)
            {
                DownloadBingXML();
                LoadXML();
                DesktopWallpapers.Program.settings.Save(DesktopWallpapers.Program.SettingsFile);

                foreach (DesktopWallpapers.Program.BingImage BI in DesktopWallpapers.Program.BingImages)
                {
                    if (BI.url != null)
                    {
                        DownloadImage("http://www.bing.com" + BI.url);
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
#endregion

#region Settings
        public class MySettings : AppSettings<MySettings>
        {
            public static string PathToImages = AppDir;
            public string LocalXmlFilename = string.Format(@"{0}{1}", PathToImages, "BingXML.xml");
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
                File.WriteAllText(fileName, JsonConvert.SerializeObject(this));
            }

            public static void Save(T pSettings, string fileName = DEFAULT_FILENAME)
            {
                File.WriteAllText(fileName, JsonConvert.SerializeObject(pSettings));
            }

            public static T Load(string fileName = DEFAULT_FILENAME)
            {                
                T t = new T();
                if (File.Exists(fileName))
                    t = JsonConvert.DeserializeObject<T>(File.ReadAllText(fileName));                                
                return t;
            }

        }


#endregion
    }
}
