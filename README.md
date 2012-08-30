### Welcome to the *AdvertHelper 1.2.2* Release!

[AdvertHelper](https://github.com/Kazazoom/mxit-net-advert-helper/) is a C#
helper for Mxit that takes care of Shinka Ad requests and display for C# based
Mxit Applications.

## RELEASE INFORMATION

*AdvertHelper 1.2.2*

30 August 2012

This is the first stable release of this helper.

### VERSIONING

For transparency and insight into our release cycle, and for striving to
maintain backward compatibility, AdvertHelper will be maintained under the
Semantic Versioning guidelines as much as possible.

Releases will be numbered with the following format:

`<major>.<minor>.<patch>`

And constructed with the following guidelines:

* Breaking backward compatibility bumps the major (and resets the minor and
  patch)
* New additions without breaking backward compatibility bumps the minor (and
  resets the patch)
* Bug fixes and misc changes bumps the patch

For more information on SemVer, please see the included semver.md document.

### REQUIREMENTS

 -  You will need to download and use the Mxit-Net-Communications-Helper or
    replace the “RedirectRequest” call to point to your own client object.
 -  Due to a problem with displaying HTTP links in the C# API we have had to
    create a work around that displays the HTTP link to Shinka adverts in a Mobi
    App page. The redirect to the Mobi page is transparent to the user. To
    leverage our work around, you will also need redirect access to the PlayInc
    Mobi App, or you will need to write your own work around for this purpose if
    you don’t use our solution.

### INSTALLATION

 1. Add the project to your solution in Visual Studio
 2. Check that all the references work, you might need to fix the references to:
     -  misc.util
     -  log4net
 3. Compile and check for errors
 4. You will need to add the following values to your app.config file

    ```xml
	<add key="ExternalAppName" value="yourserviceidfromthemxitdashboard"/>
    <add key="OpenX_URL" value="http://ox-d.shinka.sh/ma/1.0/arx?auid="/>
    <add key="OpenX_AdUnitID_120" value= "XXXXXX"/>
	<add key="OpenX_AdUnitID_180" value= "XXXXXX"/>
    <add key="OpenX_AdUnitID_240" value= "XXXXXX"/>
    <add key="OpenX_AdUnitID_320" value= "XXXXXX"/>
    <add key="isShowShinkaBannerAd" value= "1"/>
    <add key="bannerAdTimeout" value= "2500"/>
    ```

### USAGE

 1. Add the following statement to your Controller class:

    ```c#
    if (messageReceived.Body.StartsWith(".clickad"))
    {
        AdvertModule.AdvertHelper.Instance.handleUserClickedOnAdLink(messageReceived,userInfo);
        IsSendMessage = false;
    }
    ```
	
 2. Add this call to your Controller.Start (or similar) method 
 
    ```c#
    AdvertModule.QueueHelper_HTTP.Instance.StartQueueHandlers();
    ```
	
 3. Call this public function on the library for where you want to add the Ad 
    banner:

    ```c#
    AdvertModule.AdvertHelper.Instance.appendShinkaBannerAd(ref messageToSend, userInfo);
    ```
	
	You can retrieve the userInfo object via the Mxit getUser call upon the users first access of your app, and should be caching it in your database, and loading it into the user's session upon user's logon to your app.

### CONTRIBUTING

If you wish to contribute to AdvertHelper for C#, please read the README-GIT.md
file.

### QUESTIONS AND FEEDBACK

If you find code in this release behaving in an unexpected manner or contrary to
its documented behavior, please create an issue in the AdvertHelper issue
tracker at:

https://github.com/Kazazoom/mxit-net-advert-helper/issues

### LICENSE

The files in this archive are released under the open source BSD-3 license.
You can find a copy of this license in LICENSE.txt.

### ACKNOWLEDGEMENTS

The AdvertHelper team would like to thank all the contributors to the project.
Please visit us sometime soon at http://www.kazazoom.com.
