namespace NLog.Targets.Wrappers
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using NLog.Common;

    /// <summary>
    /// A target that buffers log events and sends them in batches to the wrapped target.
    /// </summary>
    [Target("QueuedWrapper", IsWrapper = true)]
    public class QueuedTargetWrapper : WrapperTargetBase
    {
        /// <summary>
        /// Event queue
        /// </summary>
        private Queue<AsyncLogEventInfo> _queuedEvents;

        /// <summary>
        /// Log level at which queued events are written to the log
        /// </summary>
        private LogLevel _triggerLevel;

        /// <summary>
        /// Maximum number of events in queue; excess events are dequeued and discarded
        /// </summary>
        [DefaultValue(100)]
        public int QueueSize { get; set; }

        /// <summary>
        /// Log level at which queued events are written to the log
        /// </summary>
        [DefaultValue("Error")]
        public string TriggerLevel { get; set; }

        /// <summary>
        /// Enqueues the log event; if the log level is the trigger level or more severe, all queued events are written
        /// </summary>
        /// <param name="logEvent"></param>
        protected override void Write(AsyncLogEventInfo logEvent)
        {
            lock (this.SyncRoot)
            {
                _queuedEvents.Enqueue(logEvent);

                if (logEvent.LogEvent.Level >= _triggerLevel)
                {
                    while (_queuedEvents.Count > 0)
                    {
                        this.WrappedTarget.WriteAsyncLogEvent(_queuedEvents.Dequeue());
                    }
                }

                if (_queuedEvents.Count > QueueSize)
                {
                    _queuedEvents.Dequeue();
                }
            }
        }

        protected override void InitializeTarget()
        {
            base.InitializeTarget();
            _queuedEvents = new Queue<AsyncLogEventInfo>(QueueSize);
            _triggerLevel = LogLevel.FromString(TriggerLevel);
        }

    }
}
