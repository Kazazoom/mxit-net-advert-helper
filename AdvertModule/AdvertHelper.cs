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


namespace AdvertModule
{
    public class AdvertHelper
    {
        private static volatile AdvertHelper instance;
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
                        instance = new AdvertHelper();
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

            deviceName = "nokia/c3";

            string strOpenxUrl = AdvertConfig.OpenX_URL + openXAdUnitToUse;
            string strUserDetails = "&" + "c.device=" + deviceName + "&" + "c.age=" + userAge + "&" + "c.gender=" + userGenderType + "&" + "xid=" + MxitUserID + "&" + "c.screensize=" + displayWidth + "x" + displayHeight;
            //string strCompleteUrl = strOpenxUrl + strUserDetails;
            string strCompleteUrl = strOpenxUrl + strUserDetails;
            //string strCompleteUrl = strOpenxUrl + Uri.EscapeDataString(strUserDetails);
            //use the complete url on a mobile
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(strCompleteUrl);

            req.UserAgent = "Mozilla Compatible mxit_client";
            req.Headers.Add("HTTP_X_DEVICE_USER_AGENT", "Mozilla Compatible mxit_client");
            req.Headers.Add("HTTP_X_FORWARDED_FOR", MxitUserID);
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
                            IMessageElement inlineImage = MessageBuilder.Elements.CreateInlineImage(adTodisplay.adImage, ImageAlignment.Center, TextFlow.AloneOnLine, 100);
                            messageToSend.Append(inlineImage);
                        }

                        messageToSend.Append("Go to ", CSS.Ins.clr["light"], CSS.Ins.mrk["d"]);
                        messageToSend.AppendLine(MessageBuilder.Elements.CreateLink(adTodisplay.altText, ".clickad~" + adTodisplay.clickURL));                        
                        messageToSend.AppendLine();

                        //register impression for the bannerad display
                        HttpWebRequest req = (HttpWebRequest)WebRequest.Create(adTodisplay.impressionURL);

                        req.UserAgent = "Mozilla Compatible mxit_client";
                        req.Headers.Add("HTTP_X_DEVICE_USER_AGENT", "Mozilla Compatible mxit_client");
                        req.Headers.Add("HTTP_X_FORWARDED_FOR", MxitUserID);
                        req.Headers.Add("HTTP_REFERER", AdvertConfig.appID);

                        req.Timeout = AdvertConfig.bannerAdTimeout;
                        req.Proxy = null;
                        req.KeepAlive = false;
                        req.ServicePoint.ConnectionLeaseTimeout = 1000;
                        req.ServicePoint.MaxIdleTime = 1000;

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
        }

        public void handleUserClickedOnAdLink(MessageReceived messageReceived, MXit.User.UserInfo userInfo)
        {
            String adClickURL = messageReceived.Body.Split('~')[1];

            createAndQueueRequestToMobiApp(userInfo, adClickURL);

            //We need to wait a short while before calling the redirect to make sure the Mobi App has saved the URL to redirect to:
            Thread.Sleep(20);

            MXit.Navigation.RedirectRequest redirectRequest;
            String messageForMobiApp = ".gotourl~" + adClickURL;
            redirectRequest = messageReceived.CreateRedirectRequest(AdvertConfig.mobiAppServiceName, messageForMobiApp);

            //Redirect the users context

            //************* Replace with your own client.RedirectRequest based on where your client object resides: ***************
            MXitConnectionModule.ConnectionManager.Instance.RedirectRequest(redirectRequest);
            //****************************
        }

    }
}
