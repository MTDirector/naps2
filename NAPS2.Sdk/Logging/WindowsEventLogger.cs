﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NAPS2.Config.Experimental;

namespace NAPS2.Logging
{
    public class WindowsEventLogger : IEventLogger
    {
        private const string SOURCE_NAME = "NAPS2";
        private const string LOG_NAME = "Application";

        private readonly ConfigProvider<CommonConfig> configProvider;

        public WindowsEventLogger(ConfigProvider<CommonConfig> configProvider)
        {
            this.configProvider = configProvider;
        }

        public void CreateEventSource()
        {
            if (!EventLog.SourceExists(SOURCE_NAME))
            {
                EventLog.CreateEventSource(SOURCE_NAME, LOG_NAME);
            }
        }

        public void LogEvent(EventType eventType, EventParams eventParams)
        {
            if (!configProvider.Get(c => c.EventLogging).HasFlag(eventType)) return;
            try
            {
                EventLog.WriteEntry(SOURCE_NAME, eventParams.ToString(), EventLogEntryType.Information);
            }
            catch (Exception ex)
            {
                Log.ErrorException("Error writing to windows event log", ex);
            }
        }
    }
}