using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MXit.Billing;
using log4net;
using System.Reflection;
using System.Net;
using System.IO;
using System.Xml;
using System.Drawing;
using MXit.Messaging.MessageElements;
using MXit.Messaging.MessageElements.Replies;
using MXit.Common;
using MXit.Messaging;
using System.Threading;
using MXitConnectionModule;
using System.Security.Cryptography;
using MXit.User;


namespace AdvertModule
{
    public class AdvertHelper
    {
        private static volatile AdvertHelper instance;
        private static Dictionary<string, IImageStripReference> bannerStripCache = new Dictionary<string, IImageStripReference>();

        private static readonly ILog logger = LogManager.GetLogger(typeof(AdvertHelper));

        private AdvertHelper()
        {
        }

        /// <summary>
        /// Zama 12 july displaying the ad on the app
        /// </summary>
        public static AdvertHelper Instance
        {
            get
            {
                if (instance == null)
                {
                    if (instance == null)
                    {
                        instance = new AdvertHelper();
                        logger.Info("[" + MethodBase.GetCurrentMethod().Name + "()] - AdvertHelper VERSION 2.0.35");
                        Console.WriteLine(DateTime.Now.ToString() + " AdvertHelper VERSION 2.0.35");
                    }
                }

                return instance;
            }
        }

        private bool getBannerAd(String MxitUserID, MXit.User.GenderType userGenderType, int displayWidth, int displayHeight, int userAge, out BannerAd adDetail)
        {
            bool success = false;

            adDetail = new BannerAd();

            //Post back to openx
            String openXAdUnitToUse = AdvertConfig.OpenX_AdUnitID_120;
            String deviceName = "samsung/sgh"; //default to most popular device

            if (displayWidth >= 480)
            {
                openXAdUnitToUse = AdvertConfig.OpenX_AdUnitID_320;
                deviceName = "android";
            }
            else if (displayWidth >= 300)
            {
                openXAdUnitToUse = AdvertConfig.OpenX_AdUnitID_320;
                deviceName = "nokia/c3";
            }
            else if (displayWidth >= 240)
            {
                openXAdUnitToUse = AdvertConfig.OpenX_AdUnitID_240;
                deviceName = "nokia/5130%20xpressmusic";
            }
            else if (displayWidth >= 180)
            {
                openXAdUnitToUse = AdvertConfig.OpenX_AdUnitID_180;
                deviceName = "samsung/sgh";
            }
            else
            {
                openXAdUnitToUse = AdvertConfig.OpenX_AdUnitID_120;
                deviceName = "samsung/sgh";
            }

            string strOpenxUrl = AdvertConfig.OpenX_URL + openXAdUnitToUse;
            string strUserDetails = "&" + "c.device=" + deviceName + "&" + "c.age=" + userAge + "&" + "c.gender=" + userGenderType.ToString().ToLower() + "&" + "xid=" + MxitUserID + "&" + "c.country=za";
            string strCompleteUrl = strOpenxUrl + strUserDetails;

            //use the complete url on a mobile
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(strCompleteUrl);

            req.UserAgent = "Mozilla Compatible mxit_client";
            req.Headers.Add("HTTP_X_DEVICE_USER_AGENT", "Mozilla Compatible mxit_client");

            Random random = new Random(DateTime.Now.Second);
            int randomUpTo254 = random.Next(1, 254);

            String tempIP = "196.25.101." + randomUpTo254;

            req.Headers.Add("HTTP_X_FORWARDED_FOR", tempIP);
            req.Headers.Add("HTTP_REFERER", AdvertConfig.appID);
            //req.Headers.Add("HTTP_X_MXIT_USER_INPUT", ".header");

            req.Timeout = AdvertConfig.bannerAdTimeout;
            req.Proxy = null;//GlobalProxySelection.GetEmptyWebProxy(); // null;
            req.KeepAlive = false;
            req.ServicePoint.ConnectionLeaseTimeout = 1000;
            req.ServicePoint.MaxIdleTime = 1000;

            //if (AdvertConfig.isShowMessages)
            //{
            //    Console.WriteLine(DateTime.Now.ToString() + " URL: " + strCompleteUrl);
            //}

            string strResponse = "";

            try
            {

                logger.Debug("[" + MethodBase.GetCurrentMethod().Name + "()] - Starting to read server ad request...");

                using (HttpWebResponse response = (HttpWebResponse)req.GetResponse())
                {
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        using (StreamReader streamIn = new StreamReader(responseStream))
                        {
                            strResponse = streamIn.ReadToEnd();
                            streamIn.Close();
                        }
                        responseStream.Flush();
                        responseStream.Close();
                    }
                    response.Close();
                }

                logger.Debug("[" + MethodBase.GetCurrentMethod().Name + "()] - Finished reading server ad request...");

                if (strResponse.Contains("media"))
                {

                    //pass in the tags to remove

                    //Determine the ad type:

                    //We aren't using XML processing to try to save processing time, not having to prase the entire XML response into objects, etc. Instead we use basic string searches. 
                    //Welcome for somebody to do a benchmark to see if XML parsing will be faster or slower.

                    adDetail.creativeType = Between(strResponse, "<ad ", "<html>", "type=", 0, ">");
                    bool isGotCreativeType = (!string.IsNullOrEmpty(adDetail.creativeType));

                    if (isGotCreativeType)
                    {
                        //Get the generic click and impression fields
                        adDetail.clickURL = Between(strResponse, "<click>", "</click>", "", 7, "");
                        adDetail.impressionURL = Between(strResponse, "<impression>", "</impression>", "", 12, "");
                        //adDetail.inView = Between(strResponse, "<inview>", "</inview>", "", 8, "");

                        //Do logic for image type ad:
                        if (adDetail.creativeType == "image")
                        {
                            adDetail.altText = Between(strResponse, "<creative", "<media>", "alt=", 0, "target");
                            if (!String.IsNullOrEmpty(adDetail.altText))
                            {
                                adDetail.adImageURL = Between(strResponse, "<media>", "</media>", "", 7, "");
                                success = true;

                            }
                            else
                            {
                                logger.Error(MethodBase.GetCurrentMethod().Name + " - " + "SHINKA ERROR: Could not find alt text in string: " + strResponse + " USING URL: " + strCompleteUrl);
                                success = false;
                            }
                        }
                        else if (adDetail.creativeType == "html")
                        {
                            //Use the altText field for the text ad body
                            String hrefHTMLText = Between(strResponse, "<media>", "</media>", "", 7, "");

                            int startIndex = hrefHTMLText.IndexOf("&gt;") + 4;
                            int endIndex = hrefHTMLText.IndexOf("&lt;/a&gt;");
                            int length = endIndex - startIndex;

                            adDetail.altText = hrefHTMLText.Substring(startIndex, length);
                            success = true;
                        }
                        else
                        {
                            logger.Error(MethodBase.GetCurrentMethod().Name + " - " + "SHINKA ERROR: Not a known creative type: " + adDetail.creativeType);
                            success = false;
                        }
                    }
                    else
                    {
                        logger.Error(MethodBase.GetCurrentMethod().Name + " - " + "SHINKA ERROR: Could not get creative type: " + strResponse + " USING URL: " + strCompleteUrl);
                        success = false;
                    }
                }
                else
                {
                    logger.Debug(MethodBase.GetCurrentMethod().Name + " - " + "SHINKA Warning: Empty ad response:" + strResponse + " USING URL: " + strCompleteUrl + " FOR USER: " + MxitUserID);
                    success = false;
                }
                

            }
            catch (Exception ex)
            {
                logger.Error(MethodBase.GetCurrentMethod().Name + " - " + "Error doing Shinka Server Add Call: " + ex.ToString());
                logger.Error(MethodBase.GetCurrentMethod().Name + " - " + "Error with String: " + strResponse);
                success = false;
            }
            finally
            {
                req.Abort();
                req = null;
                GC.Collect();
            }

            if ((adDetail.creativeType == "image") && !String.IsNullOrEmpty(adDetail.adImageURL))
            {
                logger.Debug("[" + MethodBase.GetCurrentMethod().Name + "()] - Starting to read ad bitmap image...");
                //convert the url into an image and load to the bitmap
                adDetail.adImage = BitmapFromWeb(adDetail.adImageURL);
                logger.Debug("[" + MethodBase.GetCurrentMethod().Name + "()] - Finished reading ad bitmap image...");
                success = true;
            }

            //add banner to user session
            //userSession.CurrentBannerAd = adDetail;

            return success;
        }


        //Helper function for caching, creates hash from image
        public string GetImageHash(Bitmap img) {
            string hash;

            byte[] byteArray = ImageToByte2(img);

            MD5 md5 = System.Security.Cryptography.MD5.Create();
            
            byte[] hashBytes = md5.ComputeHash(byteArray);

            // step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("X2"));
            }
            hash = sb.ToString();

            return hash;
        }

        //Helper function for caching, converts Bitmap to Byte Array
        public static byte[] ImageToByte2(Bitmap img)
        {
            byte[] byteArray = new byte[0];
            using (MemoryStream stream = new MemoryStream())
            {
                img.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                stream.Close();

                byteArray = stream.ToArray();
            }
            return byteArray;
        }

        private string Between(string content, string start, string end, string search, int startposition, string searchEnd)
        {
            int tagstartIndex = content.IndexOf(start) + startposition;

            int startvaluekey = content.IndexOf(end);
            int endIndex = startvaluekey - (tagstartIndex);
            string result = content.Substring(tagstartIndex, endIndex);
            string finalString = string.Empty;

            //search for the key
            if (search != string.Empty)
            {
                if (result.Contains(search))
                {
                    //get the start of the search string
                    int startsearchIndex = result.IndexOf(search + '"');

                    int endsearchIndex = result.IndexOf(searchEnd);
                    //check if the alt key is not an empty string
                    int length = (endsearchIndex - 1) - startsearchIndex;
                    if (endsearchIndex - startsearchIndex > 3)
                    {
                        int lengthOfFinalString = length;
                        if (search == "alt=")
                            lengthOfFinalString = lengthOfFinalString - 1;

                        finalString = result.Substring((startsearchIndex), lengthOfFinalString);
                        //chop the equal sign and t
                        finalString = finalString.Substring(search.Length+1);
                    }

                    else
                        finalString = string.Empty;
                }

            }
            else
                finalString = result;

            return finalString;
        }

        private Bitmap BitmapFromWeb(string URL)
        {

            // create a web request to the url of the image
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(URL);

            // set the method to GET to get the image
            req.Method = "GET";
            req.Timeout = AdvertConfig.bannerAdTimeout;
            req.Proxy = null;//GlobalProxySelection.GetEmptyWebProxy(); // null;
            req.KeepAlive = false;
            req.ServicePoint.ConnectionLeaseTimeout = 1000;
            req.ServicePoint.MaxIdleTime = 1000;

            try
            {

                // create a bitmap from the stream of the response
                Bitmap bmp;

                using (HttpWebResponse response = (HttpWebResponse)req.GetResponse())
                {
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        using (StreamReader streamIn = new StreamReader(responseStream))
                        {
                            bmp = new Bitmap(responseStream);
                            streamIn.Close();
                        }
                        responseStream.Flush();
                        responseStream.Close();
                    }
                    response.Close();
                }

                // return the Bitmap of the image
                return bmp;
            }
            catch (Exception ex)
            {
                logger.Error(MethodBase.GetCurrentMethod().Name + " - " + "Error getting bitmap from web: " + ex.ToString());
                return null;
            }
            finally
            {
                req.Abort();
                req = null;
                GC.Collect();
            }
        }

        private string StripTagsCharArray(string source)
        {
            char[] array = new char[source.Length];
            int arrayIndex = 0;
            bool inside = false;

            for (int i = 0; i < source.Length; i++)
            {
                char let = source[i];
                if (let == '<')
                {
                    inside = true;
                    continue;
                }
                if (let == '>')
                {
                    inside = false;
                    continue;
                }
                if (!inside)
                {
                    array[arrayIndex] = let;
                    arrayIndex++;
                }
            }
            return new string(array, 0, arrayIndex);
        }

        public bool appendShinkaBannerAd(ref MessageToSend messageToSend, MXit.User.UserInfo userInfo)
        {   
            bool gotShinkaAd = false;

            try
            {
                if (AdvertConfig.isShowShinkaBannerAd)
                {
                    String MxitUserID = userInfo.UserId;
                    MXit.User.GenderType userGenderType = userInfo.Gender;
                    int displayWidth = userInfo.DeviceInfo.DisplayWidth;
                    int displayHeight = userInfo.DeviceInfo.DisplayHeight;
                    int userAge = AgeInYears(userInfo.DateOfBirth);

                    BannerAd adTodisplay;
                    gotShinkaAd = AdvertHelper.Instance.getBannerAd(MxitUserID, userGenderType, displayWidth, displayHeight, userAge, out adTodisplay);

                    if (gotShinkaAd)
                    {
                        if (adTodisplay.creativeType == "image")
                        {
                            if ((AdvertConfig.bannerCacheSize > 0) && (messageToSend.ToDevice.HasFeature(DeviceFeatures.Gaming)))
                            {
                                //use ImageStrips to allow caching of images on users device
                                string bannerHash = GetImageHash(adTodisplay.adImage);
                                if (!bannerStripCache.ContainsKey(bannerHash))
                                {
                                    //reset the cache once the max cache size is reached (could consider more intelligent caching)
                                    if (bannerStripCache.Count >= AdvertConfig.bannerCacheSize)
                                    {
                                        bannerStripCache.Clear();
                                    }
                                    
                                    IImageStripReference bannerImageStrip = MXitConnectionModule.ConnectionManager.Instance.RegisterImageStrip(bannerHash, adTodisplay.adImage, adTodisplay.adImage.Width / 1, adTodisplay.adImage.Height, 0);
                                    bannerStripCache[bannerHash] = bannerImageStrip;
                                }

                                //this doesn't allow client-side auto resizing of images, may want to consider server side resizing
                                ITable boardAd = MessageBuilder.Elements.CreateTable(messageToSend, "ad_" + bannerHash, 1, 1);
                                boardAd.SelectionMode = SelectionRectType.Outline;
                                boardAd.Style.Align = (AlignmentType)((int)AlignmentType.VerticalCenter + (int)AlignmentType.HorizontalCenter);
                                boardAd.Mode = TableSendModeType.Update;
                                boardAd[0, 0].Frames.Set(bannerStripCache[bannerHash], 0);                                
                                messageToSend.Append(boardAd);
                            }
                            else
                            {

                            int imageDisplayWidthPerc;

                            if (displayWidth <= 128)
                            {
                                imageDisplayWidthPerc = 99;
                            }
                            else
                            {
                                imageDisplayWidthPerc = 100;
                            }

                            IMessageElement inlineImage = MessageBuilder.Elements.CreateInlineImage(adTodisplay.adImage, ImageAlignment.Center, TextFlow.AloneOnLine, imageDisplayWidthPerc);
                            messageToSend.Append(inlineImage);
                        }
                        }

                        messageToSend.Append("Go to ", CSS.Ins.clr["light"], CSS.Ins.mrk["d"]);
                        String displayText = System.Net.WebUtility.HtmlDecode(adTodisplay.altText);
                        messageToSend.AppendLine(MessageBuilder.Elements.CreateBrowserLink(displayText, adTodisplay.clickURL));                        
                        messageToSend.AppendLine();

                        //register impression for the bannerad display
                        HttpWebRequest req = (HttpWebRequest)WebRequest.Create(adTodisplay.impressionURL);

                        req.UserAgent = "Mozilla Compatible mxit_client";
                        req.Headers.Add("HTTP_X_DEVICE_USER_AGENT", "Mozilla Compatible mxit_client");


                        Random random = new Random(DateTime.Now.Second);
                        int randomUpTo254 = random.Next(1, 254);
                        String tempIP = "196.11.239." + randomUpTo254;
                        req.Headers.Add("HTTP_X_FORWARDED_FOR", tempIP);

                        req.Headers.Add("HTTP_REFERER", AdvertConfig.appID);

                        req.Timeout = AdvertConfig.bannerAdTimeout;
                        req.Proxy = null;
                        req.KeepAlive = false;
                        req.ServicePoint.ConnectionLeaseTimeout = 10000;
                        req.ServicePoint.MaxIdleTime = 10000;

                        QueueHelper_HTTP.Instance.QueueItem(req);
                    }
                }
                //zama end
            }
            catch (Exception ex)
            {
                logger.Error("[" + MethodBase.GetCurrentMethod().Name + " - Error getting or showing Shinka ad: " + ex.ToString());
            }

            return gotShinkaAd; //so that the calling function knows if an ad was displayed
        }

        public int AgeInYears(DateTime birthDate)
        {            
            DateTime now = DateTime.Today;
            int age = now.Year - birthDate.Year;
            if (birthDate.DayOfYear > now.DayOfYear) //if the user hasn't had their birthday yet, reduce by 1
            { 
                age--;
            }
            return age;
        }
        
        /*
        //Need this so that we can show an intermediate page on a Mobi app until C# apps are allowed to show HTTP links
        //This will send a request to a HTTP service to save the URL that the user should go to, when he is redirected to the destination Mobi App. 
        //This will only work if you have redirect permission from your C# App to the required Mobi App. Request this redirect permission from Mxit (Robert)
        public void createAndQueueRequestToMobiApp(MXit.User.UserInfo userInfo, String adClickURL)
        {
            String saveActionAPI_URL = AdvertConfig.mobiAppSaveActionURL;

            String saveActionToMobiAppURL =
                saveActionAPI_URL +
                "?mxituserid=" + userInfo.UserId +
                "&actiontype=" + "1" +
                "&action=" + adClickURL;

            //register impression for the bannerad display
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(saveActionToMobiAppURL);
            req.Headers.Add("X_MXPASS_MXIT_USERID", userInfo.UserId);
            req.Headers.Add("X_MXPASS_ACTION", adClickURL);
            //
            req.Timeout = AdvertConfig.bannerAdTimeout;
            req.Proxy = null;
            req.KeepAlive = false;
            req.ServicePoint.ConnectionLeaseTimeout = 1000;
            req.ServicePoint.MaxIdleTime = 1000;

            QueueHelper_HTTP.Instance.QueueItem(req);
            //QueueHelper_HTTP.Instance.processHTTPWebRequestImmediately(req);
        }
        */

        // No longer needed if we can display a HTTP link
        /*
        public void handleUserClickedOnAdLink(MessageReceived messageReceived, MXit.User.UserInfo userInfo)
        {
            String adClickURL = messageReceived.Body.Split('|')[1];

            createAndQueueRequestToMobiApp(userInfo, adClickURL); 

            //We need to wait a short while before calling the redirect to make sure the Mobi App has saved the URL to redirect to:
            Thread.Sleep(20);

            MXit.Navigation.RedirectRequest redirectRequest;
            String messageForMobiApp = ".gotourl|" + adClickURL;
            //redirectRequest = messageReceived.CreateRedirectRequest(AdvertConfig.mobiAppServiceName, messageForMobiApp);
            redirectRequest = messageReceived.CreateRedirectRequest(AdvertConfig.mobiAppServiceName);

            //Redirect the users context

            //************* Replace with your own client.RedirectRequest based on where your client object resides: ***************
            MXitConnectionModule.ConnectionManager.Instance.RedirectRequest(redirectRequest);
            //****************************

            
            //String mobiMessageBody = @"::op=cmd|type=platreq|selmsg=Click here to continue|dest=http%3a//www.google.com|id=12345: Back";
            //RESTMessageToSend rMessageToSend = new RESTMessageToSend(AdvertConfig.mobiAppServiceName, userInfo.UserId, mobiMessageBody);
            //RESTConnectionHelper.Instance.SendMessage(rMessageToSend);
             
        }
         */
    

    }
}
