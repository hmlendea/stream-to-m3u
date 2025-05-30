using System;
using System.Collections.Generic;
using System.Linq;

using StreamToM3U.Configuration;
using StreamToM3U.DataAccess.DataObjects;
using StreamToM3U.Service.Models;

namespace StreamToM3U.Service.Mapping
{
    static class ChannelStreamMapping
    {
        internal static ChannelStream ToServiceModel(this ChannelStreamEntity dataObject)
        {
            ChannelStream serviceModel = new()
            {
                Id = dataObject.Id,
                ChannelName = dataObject.ChannelName,
                Provider = Enum.Parse<StreamProvider>(dataObject.Provider, true),
                ChannelId = dataObject.ChannelId,
                Title = dataObject.Title,
                Url = dataObject.Url,
                StreamBaseUrl = dataObject.StreamBaseUrl
            };

            return serviceModel;
        }

        internal static IEnumerable<ChannelStream> ToServiceModels(this IEnumerable<ChannelStreamEntity> dataObjects)
        {
            IEnumerable<ChannelStream> serviceModels = dataObjects.Select(dataObject => dataObject.ToServiceModel());

            return serviceModels;
        }
    }
}
