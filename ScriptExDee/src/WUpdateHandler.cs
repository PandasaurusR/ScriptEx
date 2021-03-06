﻿using System;
using WUApiLib;
using System.Runtime.InteropServices;
using System.Management;
using System.Drawing;
using System.ComponentModel;
using System.ServiceProcess;
using System.Threading;

/* NOTES ON COSTURA.FODY
 * The WUApiLib file has been manually forced to be embedded in FodyWeavers.xml
 * under the name Interop.WUApiLib. The automatic detection does not work
 * for some reason. As such, copy to local has been disabled because it is not
 * necessary. 
 */

namespace ScriptExDee
{
    /// <summary>
    /// Handles windows updates.
    /// 
    /// Code adapted from Magnus Hjorth and Devil Scorpio
    /// LINK: http://www.nullskull.com/a/1592/install-windows-updates-using-c--wuapi.aspx
    /// </summary>
    static class WUpdateHandler
    {
        // Thread pausing
        public static bool InstallPaused = false;


        // Update Searching
        static IAutomaticUpdates iAutomaticUpdates;
        static UpdateSessionClass uSession;
        static IUpdateSearcher uSearcher;
        static ISearchResult uResult;

        // Update Downloading
        static IUpdateDownloader uDownloader;
        static IDownloadResult uDownloadResult;
        static int uCount;

        // Update Installing
        static UpdateCollection uToInstall;
        static IUpdateInstaller uInstaller;
        static IInstallationResult uInstallResult;

        public static void WUP()
        {
            // Attempt to find updates
            while (true)
            {
                try
                {
                    FindUpdates();
                    break;
                }
                catch (Exception)
                {
                    SetStatus("WUH cannot continue (ERR:FU), retrying...");
                    Thread.Sleep(3000);
                }
            }

            // Exit early if no updates found
            if (uCount == 0)
            {
                return;
            }

            // Attempt to download updates
            try
            {
                DownloadUpdates();
            }
            catch (Exception)
            {
                SetStatus("WUH cannot continue (ERR:DU), download failed.");
                return;
            }

            // Exit early if no installable updates found
            if (uCount == 0)
            {
                return;
            }

            // Check to ensure not in Software Mode
            while (Terminal.State.Mode == Terminal.SpecialKeys.Software)
            {
                SetStatus("(PAUSED) Updates will install outside of Software Mode.");
                Thread.Sleep(1000);
            }

            // Attempt to install updates
            try
            {
                InstallUpdates();
            }
            catch (Exception)
            {
                SetStatus("WUH cannot continue (ERR:IU), installer failed.");
                return;
            }
        }

        #region # Update handler

        /// <summary>
        /// Search for updates
        /// </summary>
        static void FindUpdates()
        {
            // Check for iAutomaticUpdates.ServiceEnabled...
            iAutomaticUpdates = new AutomaticUpdates();
            if (!iAutomaticUpdates.ServiceEnabled)
            {
                iAutomaticUpdates.EnableService();
                SetStatus("Automatic updates enabled.");
            }

            // Start searching for updates
            SetStatus("Checking for Windows updates.");
            uSession = new UpdateSessionClass();
            uSearcher = uSession.CreateUpdateSearcher();


            // Continue checking until successful connection, retry every 3 seconds
            int failCount = 0;
            while (true)
            {
                try
                {
                    uResult = uSearcher.Search("IsInstalled=0 AND IsHidden=0");
                    break;
                }
                catch (Exception)
                {
                    failCount++;
                    SetStatus($"Unable to fetch updates. Retrying... (Attempt {failCount})");
                    Thread.Sleep(3000);
                }
            }
            
            uCount = uResult.Updates.Count;

            if (uCount == 0)
            {
                SetStatus("No updates found! You're up to date.");
                return;
            }
        }

        /// <summary>
        /// Download all found updates
        /// </summary>
        static void DownloadUpdates()
        {
            // Start downloading updates
            uDownloader = uSession.CreateUpdateDownloader();
            uDownloader.Updates = uResult.Updates;
            uDownloader.Priority = DownloadPriority.dpHigh;

            // capture download result
            SetStatus($"Downloading {uCount} update(s).");
            uDownloadResult = uDownloader.Download();

            // determine if update download was successful
            switch (uDownloadResult.ResultCode)
            {
                case OperationResultCode.orcSucceeded:
                    SetStatus("Downloads complete.");
                    break;
                case OperationResultCode.orcSucceededWithErrors:
                    SetStatus("Downloads incomplete. Some updates won't be installed.");
                    break;
                default:
                    SetStatus("Update download error. Unable to continue.");
                    return;
            }

            // queue successfully downloaded updates
            uToInstall = new UpdateCollection();
            uCount = 0;
            foreach (IUpdate update in uResult.Updates)
            {
                if (update.IsDownloaded)
                {
                    uToInstall.Add(update);
                    uCount++;
                }
            }
        }

        /// <summary>
        /// Install successfully downloaded updates
        /// </summary>
        static void InstallUpdates()
        {
            // Start installing updates
            uInstaller = uSession.CreateUpdateInstaller();
            uInstaller.Updates = uToInstall;

            // capture install result
            SetStatus($"Installing {uCount} update(s).");
            uInstallResult = uInstaller.Install();

            // determine if update was successful
            switch (uInstallResult.ResultCode)
            {
                case OperationResultCode.orcSucceeded:
                    SetStatus("Updates installed successfully.");
                    break;
                case OperationResultCode.orcSucceededWithErrors:
                    SetStatus("Updates partially installed. Some were not able to be installed.");
                    break;
                default:
                    SetStatus("Update install failed. No updates installed.");
                    break;
            }

            //End
            SetStatus("Updates completed. Reboot when convienient.");
        }

        #endregion

        #region # Status handler

        static void SetStatus(string status)
        {
            Terminal.Title.SetWUP(status);
            Terminal.Title.Update();
        }

        #endregion
    }


    /// <summary>
    /// Class to manage the progress of the WUpdater
    /// </summary>
    public static class WUpdateStatus
    {
        public static bool InSoftwareMode;
    }
}
