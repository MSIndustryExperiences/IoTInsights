namespace FlattenAndPost
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Xml.Serialization;

    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Host;
    using Microsoft.Azure.WebJobs.ServiceBus;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;

    using Newtonsoft.Json;

    public static class FlattenAndPost
    {
        [FunctionName("FlattenAndPost")]
        public static async Task Run(
            [QueueTrigger("smssamples", Connection = "queueConnectionString")]
            string myQueueItem,
            TraceWriter log,
            ExecutionContext context,
            [EventHub("samplesEventHub", Connection = "smssamplesEventHub")]
            IAsyncCollector<string> asyncSampleCollector,
            [EventHub("eventsEventhub", Connection = "smsEventsEventHub")]
            IAsyncCollector<string> asyncEventCollector,
            [EventHub("conditionsEventhub", Connection = "smsConditionsEventHub")]
            IAsyncCollector<string> asyncConditionCollector,
            [Table("eventsfromfunction", Connection = "queueConnectionString")]
            IAsyncCollector<EventTableRecord> asyncEventTableCollector,
            [Table("samplesfromfunction", Connection = "queueConnectionString")]
            IAsyncCollector<SampleTableRecord> asyncSampleTableCollector,
            [Table("conditionsfromfunction", Connection = "queueConnectionString")]
            IAsyncCollector<ConditionsTableRecord> asyncConditionTableCollector,
            [Blob("streams/{queueTrigger}", FileAccess.Read)]
            Stream blobStream)
        {
            log.Info($"C# Queue trigger function processed: {myQueueItem}");

            try
            {
                var blobContents = string.Empty;

                using (var streamReader = new StreamReader(blobStream))
                {
                    blobContents = streamReader.ReadToEnd();
                }

                if (blobContents == string.Empty)
                {
                    return;
                }

                var sampleResult = DeserializeResults<MTConnectStreamsType>(blobContents);

                await PostSamples(log, asyncSampleCollector, asyncSampleTableCollector, sampleResult);

                await PostEvents(log, asyncEventCollector, asyncEventTableCollector, sampleResult);

                await ConditionsEvents(log, asyncConditionCollector, asyncConditionTableCollector, sampleResult);
            }
            catch (Exception e)
            {
                log.Error("Processing samples", e);
            }
        }

        private static async Task ConditionsEvents(
            TraceWriter log,
            IAsyncCollector<string> asyncEventCollector,
            IAsyncCollector<ConditionsTableRecord> asyncConditionTableCollector,
            MTConnectStreamsType sampleResult)
        {
            var conditions = default(IEnumerable<ConditionsRecord>);

            if (sampleResult.Streams == null)
            {
                conditions = new List<ConditionsRecord>();
            }
            else
            {
                var nonEmptyConditions = sampleResult.Streams.Where(
                        s => s.ComponentStream != null
                             && s.ComponentStream.All(cs => cs.Condition != null && cs.Condition.Any()))
                    .ToList();

                conditions = nonEmptyConditions.SelectMany(
                    s => s.ComponentStream.SelectMany(
                        cs => cs.Condition.Select(
                            c => new ConditionsRecord
                            {
                                HourWindow =
                                    new DateTime(
                                        c.timestamp.Year,
                                        c.timestamp.Month,
                                        c.timestamp.Day,
                                        c.timestamp.Hour,
                                        0,
                                        0),
                                Id = Guid.NewGuid().ToString(),
                                DeviceName = s?.name,
                                DeviceId = s?.uuid,
                                Component = cs?.component,
                                ComponentName = cs?.name,
                                ComponentId = cs?.componentId,
                                ConditionDataItemId = c?.dataItemId,
                                ConditionTimestamp = c?.timestamp,
                                ConditionName = c?.name,
                                ConditionSequence = c?.sequence,
                                ConditionSubtype = c?.subType,
                                ConditionValue = c?.Value,
                                ConditionNativeCode = c?.nativeCode,
                                ConditionNativeSeverity = c?.nativeSeverity,
                                ConditionQualifier = c?.qualifier,
                                ConditionQualifierSpecified = c?.qualifierSpecified,
                                ConditionStatistic = c?.statistic,
                                ConditionType = c?.type
                            }))).OrderBy(r => r.ConditionSequence).ToList();
            }

            log.Info($"{conditions.Count()} conditions found.");
            foreach (var conditionsRecord in conditions)
            {
                var flatCondition = JsonConvert.SerializeObject(conditionsRecord);

                await asyncEventCollector.AddAsync(flatCondition);
                //await asyncConditionTableCollector.AddAsync(new ConditionsTableRecord(conditionsRecord));
            }
        }

        private static T DeserializeResults<T>(string contents)
        {
            T result;

            contents = contents.Trim();

            if (contents == string.Empty)
            {
                return default(T);
            }

            using (var stringReader = new StringReader(contents))
            {
                var serializer = new XmlSerializer(typeof(T));

                result = (T)serializer.Deserialize(stringReader);
            }

            return result;
        }

        private static async Task PostEvents(
            TraceWriter log,
            IAsyncCollector<string> asyncEventCollector,
            IAsyncCollector<EventTableRecord> asyncEventTableCollector,
            MTConnectStreamsType sampleResult)
        {
            var events = default(IEnumerable<EventRecord>);

            if (sampleResult.Streams == null)
            {
                events = new List<EventRecord>();
            }
            else
            {
                var nonEmptyEvents = sampleResult.Streams.Where(
                        s => s.ComponentStream != null
                             && s.ComponentStream.All(cs => cs.Events != null && cs.Events.Any()))
                    .ToList();

                events = nonEmptyEvents.SelectMany(
                    s => s.ComponentStream.SelectMany(
                        cs => cs.Events.Select(
                            e => new EventRecord()
                            {
                                HourWindow =
                                    new DateTime(
                                        e.timestamp.Year,
                                        e.timestamp.Month,
                                        e.timestamp.Day,
                                        e.timestamp.Hour,
                                        0,
                                        0),
                                Id = Guid.NewGuid().ToString(),
                                DeviceName = s?.name,
                                DeviceId = s?.uuid,
                                Component = cs?.component,
                                ComponentName = cs?.name,
                                ComponentId = cs?.componentId,
                                EventDataItemId = e?.dataItemId,
                                EventTimestamp = e?.timestamp,
                                EventName = e?.name,
                                EventType = e.GetType().Name,
                                EventSequence = e?.sequence,
                                EventSubtype = e?.subType,
                                EventValue = e?.Value
                            }))).OrderBy(r => r.EventSequence).ToList();
            }

            log.Info($"{events.Count()} events found.");
            foreach (var eventRecord in events)
            {
                var flatEvent = JsonConvert.SerializeObject(eventRecord);

                await asyncEventCollector.AddAsync(flatEvent);
               // await asyncEventTableCollector.AddAsync(new EventTableRecord(eventRecord));
            }
        }

        private static async Task PostSamples(
            TraceWriter log,
            IAsyncCollector<string> asyncCollector,
            IAsyncCollector<SampleTableRecord> asyncSampleTableCollector,
            MTConnectStreamsType sampleResult)
        {
            var samples = default(IEnumerable<SampleRecord>);
            if (sampleResult.Streams == null)
            {
                samples = new List<SampleRecord>();
            }
            else
            {
                var nonEmptySamples = sampleResult.Streams.Where(
                    s => s.ComponentStream != null
                         && s.ComponentStream.All(cs => cs.Samples != null && cs.Samples.Any())).ToList();

                samples = nonEmptySamples.SelectMany(
                    s => s.ComponentStream.SelectMany(
                        cs => cs.Samples.Select(
                            sample => new SampleRecord()
                            {
                                HourWindow =
                                                  new DateTime(
                                                      sample.timestamp.Year,
                                                      sample.timestamp.Month,
                                                      sample.timestamp.Day,
                                                      sample.timestamp.Hour,
                                                      0,
                                                      0),
                                Id = Guid.NewGuid().ToString(),
                                DeviceName = s?.name,
                                DeviceId = s?.uuid,
                                Component = cs?.component,
                                ComponentName = cs?.name,
                                ComponentId = cs?.componentId,
                                SampleDataItemId = sample?.dataItemId,
                                SampleTimestamp = sample?.timestamp,
                                SampleName = sample?.name,
                                SampleSequence = sample?.sequence,
                                SampleType = sample.GetType().Name,
                                SampleSubtype = sample?.subType,
                                SampleDuration = sample?.duration,
                                SampleDurationSpecified = sample?.durationSpecified,
                                SampleRate = sample?.sampleRate,
                                SampleStatistic = sample?.statistic,
                                SampleValue = sample?.Value
                            }))).OrderBy(r => r.SampleSequence).ToList();
            }

            log.Info($"{samples.Count()} samples found.");
            foreach (var sample in samples)
            {
                var flatSample = JsonConvert.SerializeObject(sample);

                await asyncCollector.AddAsync(flatSample);
                //await asyncSampleTableCollector.AddAsync(new SampleTableRecord(sample));
            }
        }
    }

    public class ConditionsRecord
    {
        public DateTime HourWindow { get; set; }

        public string Id { get; set; }

        public string DeviceName { get; set; }

        public string DeviceId { get; set; }

        public string Component { get; set; }

        public string ComponentName { get; set; }

        public string ComponentId { get; set; }

        public string ConditionDataItemId { get; set; }

        public DateTime? ConditionTimestamp { get; set; }

        public string ConditionName { get; set; }

        public string ConditionSequence { get; set; }

        public string ConditionSubtype { get; set; }

        public string ConditionValue { get; set; }

        public string ConditionNativeCode { get; set; }

        public string ConditionNativeSeverity { get; set; }

        public QualifierType? ConditionQualifier { get; set; }

        public bool? ConditionQualifierSpecified { get; set; }

        public string ConditionStatistic { get; set; }

        public string ConditionType { get; set; }
    }

    public class EventRecord
    {
        public string Component { get; set; }

        public string ComponentId { get; set; }

        public string ComponentName { get; set; }

        public string DeviceId { get; set; }

        public string DeviceName { get; set; }

        public string EventDataItemId { get; set; }

        public string EventName { get; set; }

        public string EventSequence { get; set; }

        public string EventSubtype { get; set; }

        public DateTime? EventTimestamp { get; set; }

        public string EventValue { get; set; }

        public string Id { get; set; }

        public DateTime HourWindow { get; set; }

        public string EventType { get; set; }
    }

    public class SampleRecord
    {
        public SampleRecord()
        {
        }

        public string Component { get; set; }

        public string ComponentId { get; set; }

        public string ComponentName { get; set; }

        public string DeviceId { get; set; }

        public string DeviceName { get; set; }

        public string Id { get; set; }

        public string SampleDataItemId { get; set; }

        public float? SampleDuration { get; set; }

        public bool? SampleDurationSpecified { get; set; }

        public string SampleName { get; set; }

        public string SampleRate { get; set; }

        public string SampleSequence { get; set; }

        public string SampleStatistic { get; set; }

        public string SampleSubtype { get; set; }

        public DateTime? SampleTimestamp { get; set; }

        public string SampleValue { get; set; }

        public DateTime HourWindow { get; set; }

        public string SampleType { get; set; }
    }

    public abstract class EntityAdapter<T> : ITableEntity where T : new()
    {
        public EntityAdapter()
        {
            this.InnerObject = new T();
        }

        public EntityAdapter(T innerObject)
        {
            this.InnerObject = innerObject;
        }

        protected T InnerObject { get; set; }

        public string PartitionKey
        {
            get => this.GetPartitionKey();

            set => this.PartitionKey = value;
        }

        protected abstract string GetPartitionKey();

        public string RowKey
        {
            get => GetRowKey();
            set => this.RowKey = value;
        }

        protected abstract string GetRowKey();

        public DateTimeOffset Timestamp { get; set; }

        public string ETag { get; set; }

        public void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            this.InnerObject = new T();

            TableEntity.ReadUserObject(this.InnerObject, properties, operationContext);
        }

   
        public IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            var properties = TableEntity.WriteUserObject(this.InnerObject, operationContext);

            return properties;
        }
    }

    public class ConditionsTableRecord : EntityAdapter<ConditionsRecord>
    {
        public ConditionsTableRecord(ConditionsRecord innerObject) : base(innerObject)
        {
        }

        protected override string GetPartitionKey()
        {
            return this.InnerObject.HourWindow.ToString();
        }

        protected override string GetRowKey()
        {
            return this.InnerObject.ConditionSequence;
        }
        }

    public class EventTableRecord : EntityAdapter<EventRecord>
    {
        public EventTableRecord(EventRecord innerObject) : base(innerObject)
        {
        }

        protected override string GetPartitionKey()
        {
            return this.InnerObject.HourWindow.ToString();
        }

        protected override string GetRowKey()
        {
            return this.InnerObject.EventSequence;
        }
    }

    public class SampleTableRecord : EntityAdapter<SampleRecord>
    {
        public SampleTableRecord(SampleRecord innerObject) : base(innerObject)
        {
        }

        protected override string GetPartitionKey()
        {
            return this.InnerObject.HourWindow.ToString();
        }

        protected override string GetRowKey()
        {
            return this.InnerObject.SampleSequence;
        }
    }
}