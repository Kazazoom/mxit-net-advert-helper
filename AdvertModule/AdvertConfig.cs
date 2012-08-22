using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using log4net;


namespace AdvertModule
{
    using System.Configuration;

    internal static class AdvertConfig
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(AdvertConfig));

        #region Constructors

        /// <summary>
        /// Initializes static members of the <see cref="Config"/> class.
        /// </summary>
        static AdvertConfig()
        {
            LoadConfig();
        }

        #endregion Constructors


        #region Public Properties

        internal static Color lightColor_forBlackBG { get; set; }
        internal static Color lightColor_forWhiteBG { get; set; }
        internal static String OpenX_URL;
        internal static String OpenX_AddUnitID_120;
        internal static String OpenX_AddUnitID_240;
        internal static String OpenX_AddUnitID_320;
        internal static bool isShowShinkaBannerAd { get; set; }
        internal static int bannerAdTimeout { get; set; }
        internal static String mobiAppSaveActionURL;
        internal static String mobiAppServiceName;

        #endregion Public Properties

        public static void ReloadConfig()
        {
            ConfigurationManager.RefreshSection("appSettings");
            LoadConfig();
        }

        #region Private Methods

        /// <summary>
        /// Loads the AdvertConfig.
        /// </summary>
        private static void LoadConfig()
        {
            String temp;

            temp = ConfigurationManager.AppSettings["lightColor_forBlackBG"];
            if (!string.IsNullOrEmpty(temp))
            {
                lightColor_forBlackBG = Color.FromName(temp);
            }
            else
            {
                lightColor_forBlackBG = Color.White;
                Console.WriteLine("WARNING: Defaulting font colour for black background to white. Please add 'lightColor_forBlackBG' to your app.config file.");
            }

            temp = ConfigurationManager.AppSettings["lightColor_forWhiteBG"];
            if (!string.IsNullOrEmpty(temp))
            {
                lightColor_forWhiteBG = Color.FromName(temp);
            }
            else
            {
                lightColor_forWhiteBG = Color.White;
                Console.WriteLine("WARNING: Defaulting font colour for white background to black. Please add 'lightColor_forWhiteBG' to your app.config file.");
            }

            temp = ConfigurationManager.AppSettings["OpenX_URL"];
            if (!string.IsNullOrEmpty(temp))
            {
                OpenX_URL = temp;
            }
            else
            {
                OpenX_URL = "http://ox-d.shinka.sh/ma/1.0/arx?auid=";
                Console.WriteLine("WARNING: Defaulting Shinka URL to http://ox-d.shinka.sh/ma/1.0/arx?auid= Please add 'OpenX_URL' to your app.config file.");
            }

            temp = ConfigurationManager.AppSettings["OpenX_AddUnitID_120"];
            if (!string.IsNullOrEmpty(temp))
            {
                OpenX_AddUnitID_120 = temp;
            }
            else
            {
                OpenX_AddUnitID_120 = "";
                Console.WriteLine("ERROR: Please add a value for 'OpenX_AddUnitID_120' to your app.config file.");
            }

            temp = ConfigurationManager.AppSettings["OpenX_AddUnitID_240"];
            if (!string.IsNullOrEmpty(temp))
            {
                OpenX_AddUnitID_240 = temp;
            }
            else 
            {
                OpenX_AddUnitID_240 = "";
                Console.WriteLine("ERROR: Please add a value for 'OpenX_AddUnitID_240' to your app.config file.");
            }

            temp = ConfigurationManager.AppSettings["OpenX_AddUnitID_320"];
            if (!string.IsNullOrEmpty(temp))
            {
                OpenX_AddUnitID_320 = temp;
            }
            else
            {
                OpenX_AddUnitID_320 = "";
                Console.WriteLine("ERROR: Please add a value for 'OpenX_AddUnitID_320' to your app.config file.");
            }

            temp = ConfigurationManager.AppSettings["isShowShinkaBannerAd"];
            if (!string.IsNullOrEmpty(temp))
            {
                isShowShinkaBannerAd = (temp == "1");
            }
            else
            {
                isShowShinkaBannerAd = false;
                Console.WriteLine("WARNING: Defaulting to showing Banner Ad. Please add 'isShowShinkaBannerAd' to your app.config file.");
            }

            temp = ConfigurationManager.AppSettings["bannerAdTimeout"];
            if (!string.IsNullOrEmpty(temp))
            {
                Int16 tempInt = Convert.ToInt16(temp);
                bannerAdTimeout = tempInt;
            }
            else
            {
                bannerAdTimeout = 2000;
                Console.WriteLine("WARNING: Defaulting banner request timeout value to 2000ms. Please add 'bannerAdTimeout' to your app.config file.");
            }

            temp = ConfigurationManager.AppSettings["mobiAppSaveActionURL"];
            if (!string.IsNullOrEmpty(temp))
            {
                mobiAppSaveActionURL = temp;
            }
            else
            {
                mobiAppSaveActionURL = "http://unxprdw1.kazazoom.com/mobi_portals/playinc/saveaction.php";
                Console.WriteLine("WARNING: Defaulting mobiAppActionURL to 'http://unxprdw1.kazazoom.com/mobi_portals/playinc/saveaction.php'. Please add a value for 'mobiAppSaveActionURL' to your app.config file.");
            }

            temp = ConfigurationManager.AppSettings["mobiAppServiceName"];
            if (!string.IsNullOrEmpty(temp))
            {
                mobiAppServiceName = temp;
            }
            else
            {
                mobiAppServiceName = "playinc";
                Console.WriteLine("WARNING: Defaulting mobiAppActionURL to 'playinc'. Please add a value for 'mobiAppServiceName' to your app.config file.");
            }

        }

        #endregion Private Methods
    }
}
