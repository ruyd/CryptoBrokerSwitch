using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PigSwitch.Controllers
{
    public partial class ApdddpHub
    {

        //      FIXME_VAR_TYPE CWS_LICENSE_API_URL = 'https://www.googleapis.com/chromewebstore/v1.1f/userlicenses/';
        //      FIXME_VAR_TYPE TRIAL_PERIOD_DAYS = 2;
        //      FIXME_VAR_TYPE statusDiv;


        //      /*****************************************************************************
        //      * Call to license server to request the license
        //      *****************************************************************************/

        //      void getLicense()
        //      {
        //          xhrWithAuth('GET', CWS_LICENSE_API_URL + chrome.runtime.id, true, onLicenseFetched);
        //      }

        //      void onLicenseFetched(error, status, response)
        //      {
        //          Console.WriteLine(error, status, response);
        //          statusDiv.text("Parsing license...");
        //          response = JSON.parse(response);
        //          //$("#license_info").text(JSON.stringify(response, null, 2));
        //          if (status === 200)
        //          {
        //              parseLicense(response);
        //          }
        //          else
        //          {
        //  //$("#dateCreated").text("N/A");
        //  //$("#licenseState").addClass("alert-danger");
        //  //$("#licenseStatus").text("Error");
        //              statusDiv.html("Error reading license server.");
        //          }
        //      }

        //      /*****************************************************************************
        //      * Parse the license and determine if the user should get a free trial
        //      *  - if license.accessLevel == "FULL", they've paid for the app
        //      *  - if license.accessLevel == "FREE_TRIAL" they haven't paid
        //      *    - If they've used the app for less than TRIAL_PERIOD_DAYS days, free trial
        //      *    - Otherwise, the free trial has expired 
        //      *****************************************************************************/

        //      void parseLicense(dynamic license)
        //      {
        //          FIXME_VAR_TYPE licenseStatus;
        //          FIXME_VAR_TYPE licenseStatusText;
        //          if (license.result && license.accessLevel == "FULL")
        //          {
        //              Console.WriteLine("Fully paid & properly licensed.");
        //              licenseStatusText = "FULL";
        //              licenseStatus = "alert-success";
        //          }
        //          else if (license.result && license.accessLevel == "FREE_TRIAL")
        //          {
        //              FIXME_VAR_TYPE daysAgoLicenseIssued = Date.now() - int.Parse(license.createdTime, 10);
        //              daysAgoLicenseIssued = daysAgoLicenseIssued / 1000 / 60 / 60 / 24;
        //              if (daysAgoLicenseIssued <= TRIAL_PERIOD_DAYS)
        //              {
        //                  Console.WriteLine("Free trial, still within trial period");
        //                  licenseStatusText = "FREE_TRIAL";
        //                  licenseStatus = "alert-info";
        //              }
        //              else
        //              {
        //                  Console.WriteLine("Free trial, trial period expired.");
        //                  licenseStatusText = "FREE_TRIAL_EXPIRED";
        //                  licenseStatus = "alert-warning";
        //              }
        //          }
        //          else
        //          {
        //              Console.WriteLine("No license ever issued.");
        //              licenseStatusText = "NONE";
        //              licenseStatus = "alert-danger";
        //          }
        ////$("#dateCreated").text(moment(int.Parse(license.createdTime, 10)).format("llll"));
        ////$("#licenseState").addClass(licenseStatus);
        ////$("#licenseStatus").text(licenseStatusText);
        //          statusDiv.html(" ");
        //      }

        //      /*****************************************************************************
        //      * Helper method for making authenticated requests
        //      *****************************************************************************/

        //      // Helper Util for making authenticated XHRs
        //      void xhrWithAuth(method, url, interactive, callback)
        //      {
        //          FIXME_VAR_TYPE retry = true;
        //          getToken();

        //          void getToken()
        //          {
        //              statusDiv.text("Getting auth token...");
        //              Console.WriteLine("Calling chrome.identity.getAuthToken", interactive);
        //              chrome.identity.getAuthToken({ interactive: interactive }, function(token) {
        //                  if (chrome.runtime.lastError)
        //                  {
        //                      callback(chrome.runtime.lastError);
        //                      return;
        //                  }
        //                  Console.WriteLine("chrome.identity.getAuthToken returned a token", token);
        //                  access_token = token;
        //                  requestStart();
        //              });
        //      }

        //      void requestStart()
        //      {
        //          statusDiv.text("Starting authenticated XHR...");
        //          FIXME_VAR_TYPE xhr = new XMLHttpRequest();
        //          xhr.open(method, url);
        //          xhr.setRequestHeader('Authorization', 'Bearer ' + access_token);
        //          xhr.onload = requestComplete;
        //          xhr.send();
        //      }

        //      void requestComplete()
        //      {
        //          statusDiv.text("Authenticated XHR completed.");
        //          if (this.status == 401 && retry)
        //          {
        //              retry = false;
        //              chrome.identity.removeCachedAuthToken({ token: access_token },
        //                                          getToken);
        //          }
        //          else
        //          {
        //              callback(null, this.status, this.response);
        //          }
        //      }
    }


}