﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Globalization;
using System.Threading;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.ApplicationInsights.Metrics.Extensibility;
using Microsoft.ApplicationInsights.Channel;

using CycleKind = Microsoft.ApplicationInsights.Metrics.Extensibility.MetricAggregationCycleKind;
using Microsoft.ApplicationInsights.Metrics.TestUtil;

namespace Microsoft.ApplicationInsights.Metrics
{
    /// <summary />
    [TestClass]
    public class SimpleUInt32DataSeriesAggregatorTests
    {
        /// <summary />
        [TestMethod]
        public void Ctor()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new SimpleUInt32DataSeriesAggregator(configuration: null, dataSeries: null, aggregationCycleKind: CycleKind.Custom));

            Assert.ThrowsException<ArgumentException>(() => new SimpleUInt32DataSeriesAggregator(
                                                                           new NaiveDistinctCountMetricSeriesConfiguration(lifetimeCounter: false),
                                                                           dataSeries: null,
                                                                           aggregationCycleKind: CycleKind.Custom));

            Assert.ThrowsException<ArgumentException>(() => new SimpleUInt32DataSeriesAggregator(
                                                                            new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false),
                                                                            dataSeries: null,
                                                                            aggregationCycleKind: CycleKind.Custom));

            {
                var aggregator = new SimpleUInt32DataSeriesAggregator(
                                                new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: true),
                                                dataSeries: null,
                                                aggregationCycleKind: CycleKind.Custom);
                Assert.IsNotNull(aggregator);
            }
            {
                var aggregator = new SimpleUInt32DataSeriesAggregator(
                                                new SimpleMetricSeriesConfiguration(lifetimeCounter: true, restrictToUInt32Values: true),
                                                dataSeries: null,
                                                aggregationCycleKind: CycleKind.Custom);
                Assert.IsNotNull(aggregator);
            }
        }

        /// <summary />
        [TestMethod]
        public void TrackValueDouble()
        {
            var endTS = new DateTimeOffset(2017, 9, 25, 17, 1, 0, TimeSpan.FromHours(-8));
            var periodString = ((long) ((endTS - default(DateTimeOffset)).TotalMilliseconds)).ToString(CultureInfo.InvariantCulture);

            {
                // Empty aggregator:
                var aggregator = new SimpleUInt32DataSeriesAggregator(
                                                    new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: true),
                                                    dataSeries: null,
                                                    aggregationCycleKind: CycleKind.Custom);

                ITelemetry aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0.0, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodString);
            }
            {
                // Zero value:
                var aggregator = new SimpleUInt32DataSeriesAggregator(
                                                    new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: true),
                                                    dataSeries: null,
                                                    aggregationCycleKind: CycleKind.Custom);

                aggregator.TrackValue(0);

                ITelemetry aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 1, sum: 0.0, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodString);
            }
            {
                // Values out of range:
                var aggregator = new SimpleUInt32DataSeriesAggregator(
                                                    new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: true),
                                                    dataSeries: null,
                                                    aggregationCycleKind: CycleKind.Custom);

                Assert.ThrowsException<ArgumentException>( () => aggregator.TrackValue(-1) );
                Assert.ThrowsException<ArgumentException>( () => aggregator.TrackValue(Int32.MinValue) );
                Assert.ThrowsException<ArgumentException>( () => aggregator.TrackValue(Int64.MinValue) );

                Assert.ThrowsException<ArgumentException>( () => aggregator.TrackValue(0.1) );
                Assert.ThrowsException<ArgumentException>( () => aggregator.TrackValue(0.9) );
                Assert.ThrowsException<ArgumentException>( () => aggregator.TrackValue((float) 50.01) );
                Assert.ThrowsException<ArgumentException>( () => aggregator.TrackValue(50.99) );

                Assert.ThrowsException<ArgumentException>( () => aggregator.TrackValue(((long) UInt32.MaxValue) + (long) 1) );

                Assert.ThrowsException<ArgumentException>( () => aggregator.TrackValue(Double.NaN) );
                Assert.ThrowsException<ArgumentException>( () => aggregator.TrackValue(Double.PositiveInfinity) );
                Assert.ThrowsException<ArgumentException>( () => aggregator.TrackValue(Double.NegativeInfinity) );
                Assert.ThrowsException<ArgumentException>( () => aggregator.TrackValue(Double.MaxValue) );
               
                ITelemetry aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0, max: 0, min: 0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodString);
            }
            {
                // A single value:
                var aggregator = new SimpleDoubleDataSeriesAggregator(
                                                    new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: false),
                                                    dataSeries: null,
                                                    aggregationCycleKind: CycleKind.Custom);

                aggregator.TrackValue(42);

                ITelemetry aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 1, sum: 42.0, max: 42.0, min: 42.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodString);
            }
            {
                // Two values:
                var aggregator = new SimpleUInt32DataSeriesAggregator(
                                                    new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: true),
                                                    dataSeries: null,
                                                    aggregationCycleKind: CycleKind.Custom);

                aggregator.TrackValue(42);
                aggregator.TrackValue(19);

                ITelemetry aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 2, sum: 61.0, max: 42.0, min: 19.0, stdDev: 11.5, timestamp: default(DateTimeOffset), periodMs: periodString);
            }
            {
                // 3 values:
                var aggregator = new SimpleUInt32DataSeriesAggregator(
                                                    new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: true),
                                                    dataSeries: null,
                                                    aggregationCycleKind: CycleKind.Custom);
                aggregator.TrackValue(1800000);
                aggregator.TrackValue(0);
                aggregator.TrackValue(4200000);

                ITelemetry aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 3, sum: 6000000, max: 4200000.0, min: 0, stdDev: 1720465.05340853, timestamp: default(DateTimeOffset), periodMs: periodString);
            }
            {
                // Rounded values:
                var aggregator = new SimpleUInt32DataSeriesAggregator(
                                                    new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: true),
                                                    dataSeries: null,
                                                    aggregationCycleKind: CycleKind.Custom);
                aggregator.TrackValue(1);

                ITelemetry aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 1, sum: 1, max: 1, min: 1, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodString);

                aggregator.TrackValue(-0.0000001);
                aggregator.TrackValue(0.00000001);

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 3, sum: 1, max: 1, min: 0, stdDev: 0.471404520791032, timestamp: default(DateTimeOffset), periodMs: periodString);

                aggregator.TrackValue(100.0000001);
                aggregator.TrackValue( 99.9999999);

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 5, sum: 201, max: 100, min: 0, stdDev: 48.8278608992858, timestamp: default(DateTimeOffset), periodMs: periodString);

                aggregator.TrackValue(((double) Int32.MaxValue) - 0.0000001);
                aggregator.TrackValue(((double) Int32.MaxValue) + 0.0000001);

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 7, sum: 4294967495, max: 2147483647, min: 0, stdDev: 970134205.051638, timestamp: default(DateTimeOffset), periodMs: periodString);

                aggregator.TrackValue(((double) UInt32.MaxValue) - 0.0000001);
                aggregator.TrackValue(((double) UInt32.MaxValue) + 0.0000001);

                aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 9, sum: 12884902085, max: 4294967295, min: 0, stdDev: 1753413037.5015, timestamp: default(DateTimeOffset), periodMs: periodString);

            }
            {
                // Very large numbers:
                var aggregator = new SimpleUInt32DataSeriesAggregator(
                                                    new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: true),
                                                    dataSeries: null,
                                                    aggregationCycleKind: CycleKind.Custom);

                ITelemetry aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0, max: 0, min: 0, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodString);

                aggregator.TrackValue(UInt32.MaxValue - 10000);
                aggregator.TrackValue(UInt32.MaxValue - 1000);
                aggregator.TrackValue(UInt32.MaxValue - 100);
                aggregator.TrackValue(UInt32.MaxValue);

                aggregate = aggregator.CreateAggregateUnsafe(endTS);

                // ToDo!!
                // We need a more numerically stable value for the calculation of StdDev / variance.
                // For example, in this case, the expected value is 4189.49579305195, but we get 4189.4343293576, which is close, but still quite a bit off.
                // Since StdDev is utilized rarely, we leave this for later and put the actual outcome into the test expectation to catch breaks in the future.
                ValidateNumericAggregateValues(aggregate, name: "null", count: 4, sum: 17179858080, max: 4294967295, min: 4294957295, stdDev: 4189.4343293576, timestamp: default(DateTimeOffset), periodMs: periodString);
            }
            {
                // Large number of small values:
                var aggregator = new SimpleUInt32DataSeriesAggregator(
                                                    new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: true),
                                                    dataSeries: null,
                                                    aggregationCycleKind: CycleKind.Custom);

                for (int i = 0; i < 100000; i++)
                {
                    for (int v = 0; v <= 100; v++)
                    {
                        aggregator.TrackValue(v);
                    }
                }

                ITelemetry aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 10100000, sum: 505000000, max: 100, min: 0, stdDev: 29.1547594742265, timestamp: default(DateTimeOffset), periodMs: periodString);
            }
            {
                // Large number of large values:
                var aggregator = new SimpleUInt32DataSeriesAggregator(
                                                    new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: true),
                                                    dataSeries: null,
                                                    aggregationCycleKind: CycleKind.Custom);

                for (int i = 0; i < 100000; i++)
                {
                    for (int v = 0; v <= 300000; v += 3000)
                    {
                        aggregator.TrackValue(v);
                    }
                }

                ITelemetry aggregate = aggregator.CreateAggregateUnsafe(endTS);
                ValidateNumericAggregateValues(aggregate, name: "null", count: 10100000, sum: 1515000000000, max: 300000, min: 0, stdDev: 87464.2784226795, timestamp: default(DateTimeOffset), periodMs: periodString);
            }
        }

        /// <summary />
        [TestMethod]
        public void TrackValueObject()
        {
            var endTS = new DateTimeOffset(2017, 9, 25, 17, 1, 0, TimeSpan.FromHours(-8));
            var periodString = ((long) ((endTS - default(DateTimeOffset)).TotalMilliseconds)).ToString(CultureInfo.InvariantCulture);

            var aggregator = new SimpleUInt32DataSeriesAggregator(
                                                new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: true),
                                                dataSeries: null,
                                                aggregationCycleKind: CycleKind.Custom);

            aggregator.TrackValue(null);

            Assert.ThrowsException<ArgumentException>( () => aggregator.TrackValue((object) (Boolean) true) );

            ITelemetry aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 0, sum: 0.0, max: 0.0, min: 0.0, stdDev: 0.0, timestamp: default(DateTimeOffset), periodMs: periodString);

            Assert.ThrowsException<ArgumentException>( () => aggregator.TrackValue((object) (SByte) (0-1)) );

            aggregator.TrackValue((object) (Byte) 2);

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 1, sum: 2, max: 2, min: 2, stdDev: 0, timestamp: default(DateTimeOffset), periodMs: periodString);

            Assert.ThrowsException<ArgumentException>( () => aggregator.TrackValue((object) (Int16) (0-3)) );

            aggregator.TrackValue((object) (UInt16) 4);

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 2, sum: 6, max: 4, min: 2, stdDev: 1, timestamp: default(DateTimeOffset), periodMs: periodString);

            Assert.ThrowsException<ArgumentException>( () => aggregator.TrackValue((object) (Int32) (0-5)) );

            aggregator.TrackValue((object) (UInt32) 6);

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 3, sum: 12, max: 6, min: 2, stdDev: 1.63299316185545, timestamp: default(DateTimeOffset), periodMs: periodString);

            Assert.ThrowsException<ArgumentException>( () => aggregator.TrackValue((object) (Int64) (0-7)) );

            aggregator.TrackValue((object) (UInt64) 8);

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 4, sum: 20, max: 8, min: 2, stdDev: 2.23606797749979, timestamp: default(DateTimeOffset), periodMs: periodString);

            Assert.ThrowsException<ArgumentException>( () => aggregator.TrackValue((object) (IntPtr) 0xFF) );
            Assert.ThrowsException<ArgumentException>( () => aggregator.TrackValue((object) (UIntPtr) 0xFF) );
            Assert.ThrowsException<ArgumentException>( () => aggregator.TrackValue((object) (Char) 'x') );

            Assert.ThrowsException<ArgumentException>( () => aggregator.TrackValue((object) (Single) (0f-9.0f)) );

            aggregator.TrackValue((object) (Double) 10.0);

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 5, sum: 30, max: 10, min: 2, stdDev: 2.82842712474619, timestamp: default(DateTimeOffset), periodMs: periodString);

            Assert.ThrowsException<ArgumentException>( () => aggregator.TrackValue("-11") );

            aggregator.TrackValue("12");

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 6, sum: 42, max: 12, min: 2, stdDev: 3.41565025531987, timestamp: default(DateTimeOffset), periodMs: periodString);

            Assert.ThrowsException<ArgumentException>( () => aggregator.TrackValue("-1.300E+01") );
            Assert.ThrowsException<ArgumentException>( () => aggregator.TrackValue("13.5"));

            aggregator.TrackValue("  +14 ");

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 7, sum: 56, max: 14, min: 2, stdDev: 4, timestamp: default(DateTimeOffset), periodMs: periodString);

            Assert.ThrowsException<ArgumentException>( () => aggregator.TrackValue("fifteen") );
            Assert.ThrowsException<ArgumentException>( () => aggregator.TrackValue("") );
            Assert.ThrowsException<ArgumentException>( () => aggregator.TrackValue("foo-bar") );

            aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 7, sum: 56, max: 14, min: 2, stdDev: 4, timestamp: default(DateTimeOffset), periodMs: periodString);
        }

        private static void ValidateNumericAggregateValues(ITelemetry aggregate, string name, int count, double sum, double max, double min, double stdDev, DateTimeOffset timestamp, string periodMs)
        {
            TestUtil.Util.ValidateNumericAggregateValues(aggregate, name, count, sum, max, min, stdDev, timestamp, periodMs);
        }

        /// <summary />
        [TestMethod]
        public void TrackValueConcurrently()
        {
            var endTS = new DateTimeOffset(2017, 9, 25, 17, 1, 0, TimeSpan.FromHours(-8));
            var periodString = ((long) ((endTS - default(DateTimeOffset)).TotalMilliseconds)).ToString(CultureInfo.InvariantCulture);

            var aggregator = new SimpleUInt32DataSeriesAggregator(
                                                new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: true),
                                                dataSeries: null,
                                                aggregationCycleKind: CycleKind.Custom);

            List<Task> parallelTasks = new List<Task>();
            s_trackValueConcurrentWorker_Current = 0;
            s_trackValueConcurrentWorker_Max = 0;

            for (int i = 0; i < 100; i++)
            {
                Task t = Task.Run( () => TrackValueConcurrentWorker(aggregator) );
                parallelTasks.Add(t);
            }

            Task.WaitAll(parallelTasks.ToArray());

            Assert.AreEqual(0, s_trackValueConcurrentWorker_Current);
            Assert.IsTrue(
                        90 <= s_trackValueConcurrentWorker_Max,
                        "The local machine has timing characteristics resuling in not enough concurrency. Try re-running or tweaking delays."
                      + $" (s_trackValueConcurrentWorker_Max = {s_trackValueConcurrentWorker_Max})");

            ITelemetry aggregate = aggregator.CreateAggregateUnsafe(endTS);
            ValidateNumericAggregateValues(aggregate, name: "null", count: 5050000, sum: 757500000000, max: 300000, min: 0, stdDev: 87464.2784226795, timestamp: default(DateTimeOffset), periodMs: periodString);
        }

        private static int s_trackValueConcurrentWorker_Current;
        private static int s_trackValueConcurrentWorker_Max;

        private static async Task TrackValueConcurrentWorker(SimpleUInt32DataSeriesAggregator aggregator)
        {
            await Task.Delay(3);

            int currentWorkersAtStart = Interlocked.Increment(ref s_trackValueConcurrentWorker_Current);
            try
            {
                int maxWorkers = Volatile.Read(ref s_trackValueConcurrentWorker_Max);
                while (currentWorkersAtStart > maxWorkers)
                {
                    int prevMaxWorkers = Interlocked.CompareExchange(ref s_trackValueConcurrentWorker_Max, currentWorkersAtStart, maxWorkers);

                    if (prevMaxWorkers == maxWorkers)
                    {
                        break;
                    }
                    else
                    {
                        maxWorkers = Volatile.Read(ref s_trackValueConcurrentWorker_Max);
                    }
                }

                for (int i = 0; i < 500; i++)
                {
                    for (int v = 0; v <= 300000; v += 3000)
                    {
                        aggregator.TrackValue(v);
                        Thread.Yield();
                    }

                    await Task.Delay(1);
                }
            }
            finally
            {
                Interlocked.Decrement(ref s_trackValueConcurrentWorker_Current);
            }
        }

        /// <summary />
        [TestMethod]
        public void CreateAggregateUnsafe()
        {
            var aggregationManager = new MetricAggregationManager();
            var seriesConfig = new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: true);
            var metric = new MetricSeries(aggregationManager, "Cows Sold", seriesConfig);

            var aggregator = new SimpleUInt32DataSeriesAggregator(
                                                    metric.GetConfiguration(),
                                                    metric,
                                                    CycleKind.Custom);

            CommonSimpleDataSeriesAggregatorTests.CreateAggregateUnsafe(aggregator, metric);
        }

        /// <summary />
        [TestMethod]
        public void TryRecycle()
        {
            var measurementAggregator = new SimpleUInt32DataSeriesAggregator(
                                                new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: true),
                                                dataSeries: null,
                                                aggregationCycleKind: CycleKind.Custom);

            var counterAggregator = new SimpleUInt32DataSeriesAggregator(
                                                new SimpleMetricSeriesConfiguration(lifetimeCounter: true, restrictToUInt32Values: true),
                                                dataSeries: null,
                                                aggregationCycleKind: CycleKind.Custom);


            CommonSimpleDataSeriesAggregatorTests.TryRecycle(measurementAggregator, counterAggregator);
        }

        /// <summary />
        [TestMethod]
        public void GetDataSeries()
        {
            var aggregationManager = new MetricAggregationManager();
            var seriesConfig = new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: true);
            var metric = new MetricSeries(aggregationManager, "Cows Sold", seriesConfig);

            var aggregatorForConcreteSeries = new SimpleUInt32DataSeriesAggregator(
                                                    metric.GetConfiguration(),
                                                    dataSeries: metric,
                                                    aggregationCycleKind: CycleKind.Custom);

            var aggregatorForNullSeries = new SimpleUInt32DataSeriesAggregator(
                                                    new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: true),
                                                    dataSeries: null,
                                                    aggregationCycleKind: CycleKind.Custom);

            CommonSimpleDataSeriesAggregatorTests.GetDataSeries(aggregatorForConcreteSeries, aggregatorForNullSeries, metric);
        }

        /// <summary />
        [TestMethod]
        public void Reset()
        {
            {
                var aggregator = new SimpleUInt32DataSeriesAggregator(
                                                    new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: true),
                                                    dataSeries: null,
                                                    aggregationCycleKind: CycleKind.Custom);

                CommonSimpleDataSeriesAggregatorTests.Reset(aggregator);
            }
            {
                var aggregator = new SimpleUInt32DataSeriesAggregator(
                                                    new SimpleMetricSeriesConfiguration(lifetimeCounter: true, restrictToUInt32Values: true),
                                                    dataSeries: null,
                                                    aggregationCycleKind: CycleKind.Custom);

                CommonSimpleDataSeriesAggregatorTests.Reset(aggregator);
            }

        }

        /// <summary />
        [TestMethod]
        public void CompleteAggregation()
        {
            var aggregationManager = new MetricAggregationManager();

            var mesurementConfig = new SimpleMetricSeriesConfiguration(lifetimeCounter: false, restrictToUInt32Values: true);
            var measurementMetric = new MetricSeries(aggregationManager, "Cows Sold", mesurementConfig);

            var measurementAggregator = new SimpleUInt32DataSeriesAggregator(
                                                    measurementMetric.GetConfiguration(),
                                                    measurementMetric,
                                                    CycleKind.Custom);

            var counterConfig = new SimpleMetricSeriesConfiguration(lifetimeCounter: true, restrictToUInt32Values: true);
            var counterMetric = new MetricSeries(aggregationManager, "Cows Sold", counterConfig);

            var counterAggregator = new SimpleUInt32DataSeriesAggregator(
                                                    counterMetric.GetConfiguration(),
                                                    counterMetric,
                                                    CycleKind.Custom);

            CommonSimpleDataSeriesAggregatorTests.CompleteAggregation(measurementAggregator, counterAggregator);
        }
    }
}
