﻿// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

using System;
using Microsoft.Azure.Management.Network;
using Microsoft.Azure.Common.Authentication.Models;
using Microsoft.Azure.Common.Authentication;
using Microsoft.Azure.Management.Network.Models;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Rest.Azure;
using System.Collections.Generic;
using Microsoft.Rest;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text;
using System.Net.Http.Headers;
using System.Net;
using System.Linq;

namespace Microsoft.Azure.Commands.Network
{
    public partial class NetworkClient
    {
        public INetworkManagementClient NetworkManagementClient { get; set; }

        public Action<string> VerboseLogger { get; set; }

        public Action<string> ErrorLogger { get; set; }

        public Action<string> WarningLogger { get; set; }

        public NetworkClient(AzureContext context)
            : this(AzureSession.ClientFactory.CreateArmClient<NetworkManagementClient>(context, AzureEnvironment.Endpoint.ResourceManager))
        {
        }

        public NetworkClient(INetworkManagementClient NetworkManagementClient)
        {
            this.NetworkManagementClient = NetworkManagementClient;
        }

        public NetworkClient()
        {
        }

        public string Generatevpnclientpackage(string resourceGroupName, string virtualNetworkGatewayName, VpnClientParameters parameters)
        {
            return Task.Factory.StartNew(() => GeneratevpnclientpackageAsync(resourceGroupName, virtualNetworkGatewayName, parameters)).Unwrap().GetAwaiter().GetResult();
        }

        public async Task<string> GeneratevpnclientpackageAsync(string resourceGroupName, string virtualNetworkGatewayName, VpnClientParameters parameters,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            AzureOperationResponse<string> result = await this.GeneratevpnclientpackageWithHttpMessagesAsync(resourceGroupName, virtualNetworkGatewayName,
                parameters, null, cancellationToken).ConfigureAwait(false);
            return result.Body;
        }

        /// <summary>
        /// The Generatevpnclientpackage operation generates Vpn client package for
        /// P2S client of the virtual network gateway in the specified resource group
        /// through Network resource provider.
        /// </summary>
        /// <param name='resourceGroupName'>
        /// The name of the resource group.
        /// </param>
        /// <param name='virtualNetworkGatewayName'>
        /// The name of the virtual network gateway.
        /// </param>
        /// <param name='parameters'>
        /// Parameters supplied to the Begin Generating  Virtual Network Gateway Vpn
        /// client package operation through Network resource provider.
        /// </param>
        /// <param name='customHeaders'>
        /// Headers that will be added to request.
        /// </param>
        /// <param name='cancellationToken'>
        /// The cancellation token.
        /// </param>
        public async Task<AzureOperationResponse<string>> GeneratevpnclientpackageWithHttpMessagesAsync(string resourceGroupName, string virtualNetworkGatewayName,
            VpnClientParameters parameters, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            #region 1. Send Async request to generate vpn client package

            // 1. Send Async request to generate vpn client package          
            string baseUrl = NetworkManagementClient.BaseUri.ToString();
            string apiVersion = NetworkManagementClient.ApiVersion;

            if (resourceGroupName == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "resourceGroupName");
            }
            if (virtualNetworkGatewayName == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "virtualNetworkGatewayName");
            }
            if (parameters == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "parameters");
            }

            // Construct URL
            var url = new Uri(new Uri(baseUrl + (baseUrl.EndsWith("/") ? "" : "/")), "subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/" +
                "providers/Microsoft.Network/virtualnetworkgateways/{virtualNetworkGatewayName}/generatevpnclientpackage").ToString();
            url = url.Replace("{resourceGroupName}", Uri.EscapeDataString(resourceGroupName));
            url = url.Replace("{virtualNetworkGatewayName}", Uri.EscapeDataString(virtualNetworkGatewayName));
            url = url.Replace("{subscriptionId}", Uri.EscapeDataString(NetworkManagementClient.SubscriptionId));
            url += "?" + string.Join("&", string.Format("api-version={0}", Uri.EscapeDataString(apiVersion)));

            // Create HTTP transport objects
            HttpRequestMessage httpRequest = new HttpRequestMessage();
            httpRequest.Method = new HttpMethod("POST");
            httpRequest.RequestUri = new Uri(url);
            // Set Headers
            httpRequest.Headers.TryAddWithoutValidation("x-ms-client-request-id", Guid.NewGuid().ToString());

            // Serialize Request
            string requestContent = JsonConvert.SerializeObject(parameters, NetworkManagementClient.SerializationSettings);
            httpRequest.Content = new StringContent(requestContent, Encoding.UTF8);
            httpRequest.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json; charset=utf-8");

            // Set Credentials
            if (NetworkManagementClient.Credentials != null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await NetworkManagementClient.Credentials.ProcessHttpRequestAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            }
            // Send Request
            cancellationToken.ThrowIfCancellationRequested();
            HttpClient httpClient = new HttpClient();
            HttpResponseMessage httpResponse = await httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);

            HttpStatusCode statusCode = httpResponse.StatusCode;
            cancellationToken.ThrowIfCancellationRequested();
            if ((int)statusCode != 202)
            {
                string responseContent = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new Exception(string.Format("Get-AzureRmVpnClientPackage Operation returned an invalid status code '{0}' with Exception:{1}",
                    statusCode, string.IsNullOrEmpty(responseContent) ? "NotAvailable" : responseContent));
            }

            // Create Result
            var result = new AzureOperationResponse<string>();
            result.Request = httpRequest;
            result.Response = httpResponse;
            if (httpResponse.Headers.Contains("x-ms-request-id"))
            {
                result.RequestId = httpResponse.Headers.GetValues("x-ms-request-id").FirstOrDefault();
            }
            else
            {
                throw new Exception(string.Format("Get-AzureRmVpnClientPackage Operation Failed as no valid status code received in response!"));
            }

            string operationId = result.RequestId;
            #endregion

            #region 2. Wait for Async operation to succeed and then Get the content i.e. VPN Client package Url from locationResults
            //Microsoft.WindowsAzure.Commands.Utilities.Common.TestMockSupport.Delay(60000);

            // 2. Wait for Async operation to succeed           
            var locationResultsUrl = new Uri(new Uri(baseUrl + (baseUrl.EndsWith("/") ? "" : "/")), "subscriptions/{subscriptionId}/providers/Microsoft.Network/" +
                "locations/westus.validation/operationResults/{operationId}").ToString();
            locationResultsUrl = locationResultsUrl.Replace("{operationId}", Uri.EscapeDataString(operationId));
            locationResultsUrl = locationResultsUrl.Replace("{subscriptionId}", Uri.EscapeDataString(NetworkManagementClient.SubscriptionId));
            locationResultsUrl += "?" + string.Join("&", string.Format("api-version={0}", Uri.EscapeDataString(apiVersion)));

            DateTime startTime = DateTime.UtcNow;
            DateTime giveUpAt = DateTime.UtcNow.AddMinutes(3);

            // Send the Get locationResults request for operaitonId till either we get StatusCode 200 or it time outs (3 minutes in this case)
            while (true)
            {
                HttpRequestMessage newHttpRequest = new HttpRequestMessage();
                newHttpRequest.Method = new HttpMethod("GET");
                newHttpRequest.RequestUri = new Uri(locationResultsUrl);

                if (NetworkManagementClient.Credentials != null)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await NetworkManagementClient.Credentials.ProcessHttpRequestAsync(newHttpRequest, cancellationToken).ConfigureAwait(false);
                }

                HttpResponseMessage newHttpResponse = await httpClient.SendAsync(newHttpRequest, cancellationToken).ConfigureAwait(false);

                if ((int)newHttpResponse.StatusCode != 200)
                {
                    if (DateTime.UtcNow > giveUpAt)
                    {
                        if (!string.IsNullOrEmpty(newHttpResponse.Content.ReadAsStringAsync().Result))
                        {
                            result.Body = newHttpResponse.Content.ReadAsStringAsync().Result;
                        }
                        else
                        {
                            string newResponseContent = await newHttpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                            throw new Exception(string.Format("Get-AzureRmVpnClientPackage Operation returned an invalid status code '{0}' with Exception:{1} while retrieving " +
                                "the Vpnclient PackageUrl!",
                                newHttpResponse.StatusCode, string.IsNullOrEmpty(newResponseContent) ? "NotAvailable" : newResponseContent));
                        }
                    }
                    else
                    {
                        // Wait for 15 seconds before retrying
                        Microsoft.WindowsAzure.Commands.Utilities.Common.TestMockSupport.Delay(15000);
                    }
                }
                else
                {
                    // Get the content i.e.VPN Client package Url from locationResults
                    result.Body = newHttpResponse.Content.ReadAsStringAsync().Result;
                    return result;
                }
            }
            #endregion
        }
    }
}