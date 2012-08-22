mxit-net-advert-helper
======================

The C# version of AdvertHelper for Mxit takes care of Shinka Ad requests and display for C# based Mxit Apps

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
