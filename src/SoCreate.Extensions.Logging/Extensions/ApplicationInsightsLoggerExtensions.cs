﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.ApplicationInsights.Sinks.ApplicationInsights.TelemetryConverters;
using SoCreate.Extensions.Logging.ActivityLogger.LoggingProvider;

namespace SoCreate.Extensions.Logging.Extensions
{
    static class ApplicationInsightsLoggerExtensions
    {
        public static LoggerConfiguration WithApplicationInsights(this LoggerConfiguration config,
            string instrumentationKey, IUserProvider? userProvider = null)
        {
            config.WriteTo.ApplicationInsights(instrumentationKey, new CustomTelemetryConvertor(userProvider));
            return config;
        }
    }

    class CustomTelemetryConvertor : TraceTelemetryConverter
    {
        private readonly Func<int>? _getUserIdFromContext;

        public CustomTelemetryConvertor(IUserProvider? userProvider = null)
        {
            if (userProvider != null)
            {
                _getUserIdFromContext = userProvider.GetUserId;
            }
        }

        public override IEnumerable<ITelemetry> Convert(LogEvent logEvent, IFormatProvider formatProvider)
        {
            foreach (ITelemetry telemetry in base.Convert(logEvent, formatProvider))
            {
                // Add Operation Id
                if (Activity.Current?.RootId != null)
                {
                    telemetry.Context.Operation.Id = Activity.Current?.RootId;
                }

                if (_getUserIdFromContext != null)
                {
                    telemetry.Context.User.Id = _getUserIdFromContext().ToString();
                }

                yield return telemetry;
            }
        }


        public override void ForwardPropertiesToTelemetryProperties(LogEvent logEvent, ISupportProperties telemetryProperties, IFormatProvider formatProvider)
        {
            base.ForwardPropertiesToTelemetryProperties(logEvent, telemetryProperties, formatProvider,
                includeLogLevel: true,
                includeRenderedMessage: true,
                includeMessageTemplate: false);
        }
    }
}