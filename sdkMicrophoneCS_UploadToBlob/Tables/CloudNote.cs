using System;
using System.Globalization;
using System.Runtime.Serialization;
using Microsoft.WindowsAzure.Samples.Data.Services.Client;
using Microsoft.WindowsAzure.Samples.Phone.Storage;

namespace sdkMicrophoneCS.Tables
{
    [DataServiceEntity]
    [EntitySet("CloudNote")]
    public class CloudNote : TableServiceEntity
    {
        public CloudNote()
            : base(
                  "PartitionKey",
                  string.Format(
                      CultureInfo.InvariantCulture,
                      "{0:10}_{1}",
                      DateTime.MaxValue.Ticks - DateTime.Now.Ticks,
                      Guid.NewGuid()))
        {
        }

        public CloudNote(string partitionKey, string rowKey)
            : base(partitionKey, rowKey)
        {
        }


        //public CloudNote(string uri, string user, string partitionKey, string applicationId, string deviceId)
        //{
        //    this.Uri = uri;
        //    this.User = user;
        //    this.PartitionKey = partitionKey;
        //    this.ApplicationId = applicationId;
        //    this.DeviceId = deviceId;
        //}

        //[DataMember]
        //public string PartitionKey { get; set; }

        public string Uri { get; set; }

        public string ApplicationId { get; set; }

        public string DeviceId { get; set; }

        public DateTime Time { get; set; }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "By: {0}{1}At: {2}", Helper.DeviceId, Environment.NewLine, this.Time);
        }
    }
}
