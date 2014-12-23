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

        private static readonly int CacheTime = 120;


        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        //private static readonly ILog log = LogManager.GetLogger(typeof (Program)) ;

        private static string PathToImages = "C:\\Users\\joabj85\\";
        private static string LocalXmlFilename = string.Format(@"{0}{1}", PathToImages, "BingXML.xml");
        public static string LocalImageFilename = string.Format(@"{0}{1}", PathToImages, "background");

        public static List<BingImage> BingImages = new List<BingImage>();

        public static string AppDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        ///CommandLine stuff
        [DllImport( "kernel32.dll" )]
        static extern bool AttachConsole(int dwProcessId);
        private const int ATTACH_PARENT_PROCESS = -1;



        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Properties.Settings.Default.Setting = "Foo";
            Properties.Settings.Default.Save();
            Log.Debug("Application Starting");
            Log.Debug("Appdir:" + AppDir);

            if (args == null)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Form1());                        
            }
            else
            {
                // redirect console output to parent process;
                // must be before any calls to Console.WriteLine()
                AttachConsole(ATTACH_PARENT_PROCESS);

                Console.WriteLine();
                // to demonstrate where the console output is going
                int argCount = args == null ? 0 : args.Length;
                Console.WriteLine("You specified {0} arguments:", argCount);
                for (int i = 0; i < argCount; i++)
                {
                    Console.WriteLine("  {0}", args[i]);
                }
                        
                Console.WriteLine();
            }
            
        }

        public static void LoadXML()
        {
            string value = File.ReadAllText(LocalXmlFilename);
            string url = "";

       
            Log.Info("PictureOfTheDay: Loaded xml: " + value.Length);
            XmlDocument doc = new XmlDocument();
            try
            {                
                BingImages.Clear();
                doc.LoadXml(value);
                System.Xml.XmlNodeList NodeList = doc.GetElementsByTagName("image");
                Log.Info("PictureOfTheDay: NodeList count:" + NodeList.Count);
                foreach (XmlNode node in NodeList)
                {
                    BingImage BI = new BingImage();
                    BI.url = node["url"].InnerText;
                    BI.copyright = System.Text.RegularExpressions.Regex.Unescape(node["copyright"].InnerText);
                    BI.startdate = node["startdate"].InnerText;
                    BI.fullstartdate = node["fullstartdate"].InnerText;
                    BI.enddate = node["enddate"].InnerText;

                    BingImages.Add(BI);
                }
            }
            catch (XmlException e)
            {
                Log.Error("Can't download or parse Bing XML-file: "+e.Message);
                return;
            }

            foreach (BingImage BI in BingImages)
            {
                if (BI.url != null)
                {
                    url = BI.url;
                }                
            }

        }

       

        public static void DownloadBingXML()
        {
            if (File.Exists(LocalXmlFilename))
            {
                DateTime fileModifiedDate = File.GetLastWriteTime(LocalXmlFilename);
                if (fileModifiedDate.AddMinutes(CacheTime) > DateTime.Now)
                {
                    return;
                }
            }

            WebClient client = new WebClient();
            client.Headers["User-Agent"] =
                    "Mozilla/4.0 (Compatible; Windows NT 5.1; MSIE 6.0) " +
                    "(compatible; MSIE 6.0; Windows NT 5.1; " +
                    ".NET CLR 1.1.4322; .NET CLR 2.0.50727)";

            // Download XML from Bing.
            try
            {
                client.DownloadFile("http://www.bing.com/HPImageArchive.aspx?format=xml&idx=0&n=1&mkt=en-US", LocalXmlFilename);
            }
            catch (WebException e)
            {
                Log.Error("Failed to download Bing XML: "+e.Message);             
            }
            Log.Info("PictureOfTheDay: Downloaded XML-file");
        }

        public static void DownloadImage(string url)
        {
            string LocalImage = LocalImageFilename;
            string newLocalImage = LocalImageFilename + ".png";

            if (File.Exists(LocalImage))
            {
                DateTime fileModifiedDate = File.GetLastWriteTime(LocalImage);
                if (fileModifiedDate.AddMinutes(CacheTime) > DateTime.Now)
                {
                    return;
                }
            }

            WebClient client = new WebClient();
            Log.Info("PictureOfTheDay: Save image from "+url+" to " + LocalImage);

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

        ///<summary>
        /// Function to set the wallpaper given a location
        /// </summary>
        ///<parameter>location of the file</parameter>
        static void SetWallpaper(string location)
        {
            const string userRoot = "HKEY_CURRENT_USER";
            const string subkey = @"Control Panel\Desktop\";
            const string keyName = userRoot + "\\" + subkey;           

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

            return str;
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

    }
}
