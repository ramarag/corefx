// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Runtime;
using System.Threading.Tasks;
using Xunit;

namespace System.Tests
{
    public static partial class GCTests
    {
        private const long maxGarbage = 1000;
        public static void MakeSomeGarbage()
        {
            Version vt;

            for (int i = 0; i < maxGarbage; i++)
            {
                // Create objects and release them to fill up memory
                // with unused objects.
                vt = new Version();
            }
        }
        [Fact]
        public static void GetGeneration_WeakReference()
        {
            var myobj = new Version();
            MakeSomeGarbage();
            WeakReference wkref = new WeakReference(myobj);
            var currentgen = GC.GetGeneration(wkref);
            myobj = null;

            Assert.True(currentgen >= 0);
            GC.Collect(currentgen, GCCollectionMode.Forced, true, true);

            Assert.Throws<ArgumentNullException>(() => GC.GetGeneration(wkref));

        }
        [Fact]
        public static void GCNotifiicationNEgTests()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => GC.RegisterForFullGCNotification(-1, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => GC.RegisterForFullGCNotification(100, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => GC.RegisterForFullGCNotification(-1, 100));

            Assert.Throws<ArgumentOutOfRangeException>(() => GC.RegisterForFullGCNotification(10, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => GC.RegisterForFullGCNotification(-1, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => GC.RegisterForFullGCNotification(100, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => GC.RegisterForFullGCNotification(10, 100));


            Assert.Throws<ArgumentOutOfRangeException>(() => GC.WaitForFullGCApproach(-2));
            Assert.Throws<ArgumentOutOfRangeException>(() => GC.WaitForFullGCComplete(-2));
        }
        [Fact]
        public static void GCNotifiicationTests()
        {
            Assert.True(TestWait(true, -1));
            Assert.True(TestWait(false, -1));
            Assert.True(TestWait(true, 0));
            Assert.True(TestWait(false, 0));
            Assert.True(TestWait(true, 1000));
            Assert.True(TestWait(false, 1000));
            Assert.True(TestWait(true, int.MaxValue));
            Assert.True(TestWait(false, int.MaxValue));

        }
        [Fact]
        public static void TryStartNoGCRegionTest()
        {

            Assert.Throws<InvalidOperationException>(() => GC.EndNoGCRegion());
            Assert.True(GC.TryStartNoGCRegion(1024));
            Assert.Throws<InvalidOperationException>(() => GC.TryStartNoGCRegion(1024));

            Assert.True(GC.TryStartNoGCRegion(1024, true));
            Assert.Throws<InvalidOperationException>(() => GC.TryStartNoGCRegion(1024, true));

            Assert.True(GC.TryStartNoGCRegion(1024, 1024));
            Assert.Throws<InvalidOperationException>(() => GC.TryStartNoGCRegion(1024, 1024));

            Assert.True(GC.TryStartNoGCRegion(1024, 1024, true));
            Assert.Throws<InvalidOperationException>(() => GC.TryStartNoGCRegion(1024, 1024, true));

            Assert.True(GC.TryStartNoGCRegion(1024));
            Assert.Equal(GCSettings.LatencyMode, GCLatencyMode.NoGCRegion);
            GC.EndNoGCRegion();

            Assert.True(GC.TryStartNoGCRegion(1024, true));
            Assert.Equal(GCSettings.LatencyMode, GCLatencyMode.NoGCRegion);
            GC.EndNoGCRegion();

            Assert.True(GC.TryStartNoGCRegion(1024, 1024));
            Assert.Equal(GCSettings.LatencyMode, GCLatencyMode.NoGCRegion);
            GC.EndNoGCRegion();

            Assert.True(GC.TryStartNoGCRegion(1024, 1024, true));
            Assert.Equal(GCSettings.LatencyMode, GCLatencyMode.NoGCRegion);
            GC.EndNoGCRegion();

            Assert.True(GC.TryStartNoGCRegion(1024, true));
            Assert.Equal(GCSettings.LatencyMode, GCLatencyMode.NoGCRegion);

            Assert.Throws<InvalidOperationException>(() => GCSettings.LatencyMode = GCLatencyMode.LowLatency);

            // To ensure that We are not in NoGCRegion
            if (GCSettings.LatencyMode == GCLatencyMode.NoGCRegion)
                GC.EndNoGCRegion();
        }

        public static bool TestWait(bool approach, int timeout)
        {
            GCNotificationStatus result = GCNotificationStatus.Failed;
            Thread cancelProc = null;

            // Since we need to test an infinite (or very large) wait but the API won't return, spawn off a thread which
            // will cancel the wait after a few seconds
            //
            bool cancelTimeout = (timeout == -1) || (timeout > 10000);

            GC.RegisterForFullGCNotification(20, 20);

            try
            {
                if (cancelTimeout)
                {
                    cancelProc = new Thread(new ThreadStart(CancelProc));
                    cancelProc.Start();
                }

                if (approach)
                    result = GC.WaitForFullGCApproach(timeout);
                else
                    result = GC.WaitForFullGCComplete(timeout);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error - Unexpected exception received: {0}", e.ToString());
                return false;
            }
            finally
            {
                if (cancelProc != null)
                    cancelProc.Join();
            }

            if (cancelTimeout)
            {
                if (result != GCNotificationStatus.Canceled)
                {
                    Console.WriteLine("Error - WaitForFullGCApproach result not Cancelled");
                    return false;
                }
            }
            else
            {
                if (result != GCNotificationStatus.Timeout)
                {
                    Console.WriteLine("Error - WaitForFullGCApproach result not Timeout");
                    return false;
                }
            }

            return true;
        }

        public static void CancelProc()
        {
            Thread.Sleep(5000);
            GC.CancelFullGCNotification();
        }
    }
}
