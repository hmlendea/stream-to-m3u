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
            ChannelStream serviceModel = new ChannelStream();
            serviceModel.Id = dataObject.Id;
            serviceModel.ChannelName = dataObject.ChannelName;
            serviceModel.Provider = (StreamProvider)Enum.Parse(typeof(StreamProvider), dataObject.Provider, true);
            serviceModel.ChannelId = dataObject.ChannelId;
            serviceModel.Title = dataObject.Title;
            serviceModel.Url = dataObject.Url;
            serviceModel.StreamBaseUrl = dataObject.StreamBaseUrl;

            return serviceModel;
        }

        internal static IEnumerable<ChannelStream> ToServiceModels(this IEnumerable<ChannelStreamEntity> dataObjects)
        {
            IEnumerable<ChannelStream> serviceModels = dataObjects.Select(dataObject => dataObject.ToServiceModel());

            return serviceModels;
        }
    }
}
