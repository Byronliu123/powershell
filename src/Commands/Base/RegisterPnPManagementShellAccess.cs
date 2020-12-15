﻿using Microsoft.Identity.Client;
using PnP.Framework;

using PnP.PowerShell.Commands.Model;
using PnP.PowerShell.Commands.Utilities;
using System;
using System.Diagnostics;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using TextCopy;

namespace PnP.PowerShell.Commands.Base
{
    [Cmdlet(VerbsLifecycle.Register, "PnPManagementShellAccess")]
    public class RegisterPnPManagementShellAccess : PSCmdlet
    {
        CancellationTokenSource source;

        [Parameter(Mandatory = false)]
        public AzureEnvironment AzureEnvironment = AzureEnvironment.Production;

        protected override void ProcessRecord()
        {
            source = new CancellationTokenSource();
            var messageWriter = new CmdletMessageWriter(this);
            CancellationToken cancellationToken = source.Token;

            var endPoint = GenericToken.GetAzureADLoginEndPoint(AzureEnvironment);

            var application = PublicClientApplicationBuilder.Create(PnPConnection.PnPManagementShellClientId).WithAuthority($"{endPoint}/organizations/").WithRedirectUri("https://login.microsoftonline.com/common/oauth2/nativeclient").Build();

            Task.Factory.StartNew(() =>
            {
                application.AcquireTokenWithDeviceCode(new[] { "https://graph.microsoft.com/.default" }, codeResult =>
                {
                    ClipboardService.SetText(codeResult.UserCode);
                    messageWriter.WriteMessage($"Provide consent for the PnP Management Shell application to access SharePoint.\n\nWe opened a browser and navigated to {codeResult.VerificationUrl}\n\nEnter code: {codeResult.UserCode} (we copied this code to your clipboard)");
                    //this.WriteFormattedWarning($"Provide consent for the PnP Management Shell application to access SharePoint.\n\nWe opened a browser and navigated to {codeResult.VerificationUrl}\n\nEnter code: {codeResult.UserCode} (we copied this code to your clipboard)");
                    BrowserHelper.LaunchBrowser(codeResult.VerificationUrl);
                    return Task.FromResult("");
                }).ExecuteAsync(cancellationToken).GetAwaiter().GetResult();
                messageWriter.Finished = true;
            }, cancellationToken);
            messageWriter.Start();

        }

        protected override void StopProcessing()
        {
            source.Cancel();
        }
    }
}