using ADP.Portal.Core.Azure.Entities;
using Mapster;
using Microsoft.Graph.Models;

namespace ADP.Portal.Api.Mapster
{
    public static class AadGroupMapping
    {
        public static void Configure()
        {
            TypeAdapterConfig<AadGroup, Group>.NewConfig()
           .Map(dest => dest.MailEnabled, src => false)
           .Map(dest => dest.SecurityEnabled, src => true)
           .Map(dest => dest.MailNickname, src => src.DisplayName)
           .Map(dest => dest.AdditionalData, src => new Dictionary<string, object>
           {
                {
                    "owners@odata.bind" , new List<string>
                    {
                        $"https://graph.microsoft.com/v1.0/servicePrincipals/{src.OwnerId}",
                    }
                }
           });
        }
    }
}
