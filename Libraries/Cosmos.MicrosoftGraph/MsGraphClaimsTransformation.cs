// <copyright file="MsGraphClaimsTransformation.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.MicrosoftGraph
{
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;

    // SEE: https://damienbod.com/2021/09/06/using-azure-security-groups-in-asp-net-core-with-an-azure-b2c-identity-provider/
    public class MsGraphClaimsTransformation
    {
        private readonly MsGraphService msGraphService;

        /// <summary>
        /// Initializes a new instance of the <see cref="MsGraphClaimsTransformation"/> class.
        /// </summary>
        /// <param name="msGraphService">Microsoft Graph Service.</param>
        public MsGraphClaimsTransformation(MsGraphService msGraphService)
        {
            this.msGraphService = msGraphService;
        }

        /// <summary>
        /// Returns a claims principal with the group claims added.
        /// </summary>
        /// <param name="principal">The claims principal.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            ClaimsIdentity claimsIdentity = new ();
            var groupClaimType = "group";
            if (!principal.HasClaim(claim => claim.Type == groupClaimType))
            {
                var objectidentifierClaimType = "http://schemas.microsoft.com/identity/claims/objectidentifier";
                var objectIdentifier = principal.Claims.FirstOrDefault(t => t.Type == objectidentifierClaimType);

                if (objectIdentifier != null)
                {
                    var groups = await msGraphService.GetGraphApiUserMemberGroups(objectIdentifier.Value);

                    if (groups != null)
                    {
                        foreach (var group in groups)
                        {
                            if (!string.IsNullOrEmpty(group.DisplayName) && !string.IsNullOrEmpty(group.Id))
                            {
                                var claim = new Claim(groupClaimType, group.DisplayName);
                                claim.Properties.Add("id", group.Id);
                                claimsIdentity.AddClaim(claim);
                            }
                        }
                    }
                }
            }

            principal.AddIdentity(claimsIdentity);
            return principal;
        }
    }
}
