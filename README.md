mxit-net-advert-helper
======================

The C# version of AdvertHelper for Mxit takes care of Shinka Ad requests and display for C# based Mxit Apps. The library is provided under the open source BSD-3 license.

Install steps:

1) Add the project to your solution in Visual Studio

2) Check that all the references work, you might need to fix the references to:
misc.util
log4net

3) Compile and check for errors

4) You will need to add the following values to your app.config file
      <add key="OpenX_URL" value="http://ox-d.shinka.sh/ma/1.0/arx?auid="/>
      <add key="OpenX_AddUnitID_120" value= "XXXXXX"/> 
      <add key="OpenX_AddUnitID_240" value= "XXXXXX"/> 
      <add key="OpenX_AddUnitID_320" value= "XXXXXX"/> 
      <add key="isShowShinkaBannerAd" value= "1"/>
      <add key="bannerAdTimeout" value= "2500"/>
	  
5) Add the following statement to your Controller class

            else if (messageReceived.Body.StartsWith(".clickad"))
            {
                AdvertModule.AdvertHelper.Instance.handleUserClickedOnAdLink(messageReceived,userInfo);

                IsSendMessage = false;
            }
			
6) Call this public function on the library for where you want to add the Ad banner:

AdvertModule.AdvertHelper.Instance.appendShinkaBannerAd(ref messageToSend, userInfo);
