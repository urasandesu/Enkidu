/* 
 * File: EventIntegrationTest.cs
 * 
 * Author: Akira Sugiura (urasandesu@gmail.com)
 * 
 * 
 * Copyright (c) 2017 Akira Sugiura
 *  
 *  This software is MIT License.
 *  
 *  Permission is hereby granted, free of charge, to any person obtaining a copy
 *  of this software and associated documentation files (the "Software"), to deal
 *  in the Software without restriction, including without limitation the rights
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *  copies of the Software, and to permit persons to whom the Software is
 *  furnished to do so, subject to the following conditions:
 *  
 *  The above copyright notice and this permission notice shall be included in
 *  all copies or substantial portions of the Software.
 *  
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 *  THE SOFTWARE.
 */



using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Urasandesu.Enkidu;
using ST = System.Threading;

namespace Test.Urasandesu.Enkidu
{
    [TestFixture]
    public class EventIntegrationTest
    {
        [Test]
        public void Can_restrain_tasks_to_wait_until_ending_process_in_order_of_all_tasks()
        {
            // Arrange
            var processes = new ConcurrentBag<int>();
            var waiter1 = Synchronizable.EventWait(obj => (int)obj == 1);
            var waiter2 = Synchronizable.EventWait(obj => (int)obj == 2);
            var waiter3 = Synchronizable.EventWait(obj => (int)obj == 3);


            var sync = default(ISynchronizer);
            try
            {
                // Act
                sync = waiter1.Then(waiter2).Then(waiter3).GetSynchronizer();
                var task1 = Task.Run(() =>
                {
                    sync.Begin(1).Wait();
                    processes.Add(1);
                    sync.End(1).Wait();
                });

                var task2 = Task.Run(() =>
                {
                    sync.Begin(2).Wait();
                    processes.Add(2);
                    sync.End(2).Wait();
                });

                var task3 = Task.Run(() =>
                {
                    sync.Begin(3).Wait();
                    processes.Add(3);
                    sync.End(3).Wait();
                });

                sync.NotifyAll(false).Wait();


                // Assert
                CollectionAssert.AreEqual(new[] { 1, 2, 3 }, processes);
                Task.WaitAll(task1, task2, task3);
            }
            finally
            {
                sync?.Dispose();
            }
        }

        [Test]
        public void Can_restrain_tasks_to_wait_until_beginning_start_of_all_tasks()
        {
            // Arrange
            var starts = new ConcurrentBag<int>();
            var processes = new ConcurrentBag<int>();
            var setter1 = Synchronizable.EventSet(obj => (int)obj == 1);
            var setter2 = Synchronizable.EventSet(obj => (int)obj == 2);


            var sync = default(ISynchronizer);
            try
            {
                // Act
                sync = setter1.And(setter2).GetSynchronizer();

                var mre1 = new ST::ManualResetEventSlim(false);
                var task1 = Task.Run(() =>
                {
                    starts.Add(1);
                    sync.Begin(1).Wait();
                    mre1.Wait(10000);
                    processes.Add(1);
                    sync.End(1).Wait();
                });

                var mre2 = new ST::ManualResetEventSlim(false);
                var task2 = Task.Run(() =>
                {
                    starts.Add(2);
                    sync.Begin(2).Wait();
                    mre2.Wait(10000);
                    processes.Add(2);
                    sync.End(2).Wait();
                });

                sync.NotifyAll(false).Wait();


                // Assert
                CollectionAssert.AreEquivalent(new[] { 1, 2 }, starts);
                CollectionAssert.IsEmpty(processes);
                mre1.Set();
                mre2.Set();
                Task.WaitAll(task1, task2);
            }
            finally
            {
                sync?.Dispose();
            }
        }

        [Test]
        public void Can_restrain_tasks_to_wait_until_beginning_start_of_task1_or_task2_and_until_ending_process_of_task3_and_task4_in_order()
        {
            // Arrange
            var starts = new ConcurrentBag<int>();
            var processes = new ConcurrentBag<int>();
            var setter1 = Synchronizable.EventSet(obj => (int)obj == 1);
            var setter2 = Synchronizable.EventSet(obj => (int)obj == 2);
            var waiter3 = Synchronizable.EventWait(obj => (int)obj == 3);
            var waiter4 = Synchronizable.EventWait(obj => (int)obj == 4);


            var sync = default(ISynchronizer);
            try
            {
                // Act
                sync = setter1.Or(setter2).And(waiter3.Then(waiter4)).GetSynchronizer();

                var mre1 = new ST::ManualResetEventSlim(false);
                var task1 = Task.Run(() =>
                {
                    mre1.Wait(10000);
                    starts.Add(1);
                    sync.Begin(1).Wait();
                    processes.Add(1);
                    sync.End(1).Wait();
                });

                var task2 = Task.Run(() =>
                {
                    starts.Add(2);
                    sync.Begin(2).Wait();
                    processes.Add(2);
                    sync.End(2).Wait();
                });

                var task3 = Task.Run(() =>
                {
                    starts.Add(3);
                    sync.Begin(3).Wait();
                    processes.Add(3);
                    sync.End(3).Wait();
                });

                var task4 = Task.Run(() =>
                {
                    starts.Add(4);
                    sync.Begin(4).Wait();
                    processes.Add(4);
                    sync.End(4).Wait();
                });

                sync.NotifyAll(false).Wait();


                // Assert
                CollectionAssert.DoesNotContain(starts, 1);
                CollectionAssert.AreEqual(new[] { 3, 4 }, processes.Intersect(new[] { 3, 4 }));
                mre1.Set();
                Task.WaitAll(task1, task2, task3, task4);
            }
            finally
            {
                sync?.Dispose();
            }
        }

        [Test]
        public void Should_ignore_empty_when_restraining_tasks()
        {
            // Arrange
            var starts = new ConcurrentBag<int>();
            var processes = new ConcurrentBag<int>();
            var empty = Synchronizable.Empty();
            var setter1 = Synchronizable.EventSet(obj => (int)obj == 1);
            var setter2 = Synchronizable.EventSet(obj => (int)obj == 2);
            var waiter3 = Synchronizable.EventWait(obj => (int)obj == 3);
            var waiter4 = Synchronizable.EventWait(obj => (int)obj == 4);


            var sync = default(ISynchronizer);
            try
            {
                // Act
                sync = setter1.Or(setter2).Or(empty).And(empty.Then(waiter3).Then(empty).Then(waiter4).Then(empty)).And(empty).GetSynchronizer();

                var mre1 = new ST::ManualResetEventSlim(false);
                var task1 = Task.Run(() =>
                {
                    mre1.Wait(10000);
                    starts.Add(1);
                    sync.Begin(1).Wait();
                    processes.Add(1);
                    sync.End(1).Wait();
                });

                var task2 = Task.Run(() =>
                {
                    starts.Add(2);
                    sync.Begin(2).Wait();
                    processes.Add(2);
                    sync.End(2).Wait();
                });

                var task3 = Task.Run(() =>
                {
                    starts.Add(3);
                    sync.Begin(3).Wait();
                    processes.Add(3);
                    sync.End(3).Wait();
                });

                var task4 = Task.Run(() =>
                {
                    starts.Add(4);
                    sync.Begin(4).Wait();
                    processes.Add(4);
                    sync.End(4).Wait();
                });

                sync.NotifyAll(false).Wait();


                // Assert
                CollectionAssert.DoesNotContain(starts, 1);
                CollectionAssert.AreEqual(new[] { 3, 4 }, processes.Intersect(new[] { 3, 4 }));
                mre1.Set();
                Task.WaitAll(task1, task2, task3, task4);
            }
            finally
            {
                sync?.Dispose();
            }
        }

        [Test]
        public void Can_pause_tasks_by_the_passed_time_span()
        {
            // Arrange
            var processes = new ConcurrentBag<int>();
            var waiter1 = Synchronizable.EventWait(obj => (int)obj == 1);
            var waiter2 = Synchronizable.EventWait(obj => (int)obj == 2);


            var sync = default(ISynchronizer);
            try
            {
                // Act
                var task1EndTime = default(DateTimeOffset);
                var task2StartTime = default(DateTimeOffset);
                sync = waiter1.Then(waiter2.Pause(TimeSpan.FromMilliseconds(1000))).GetSynchronizer();
                var task1 = Task.Run(() =>
                {
                    sync.Begin(1).Wait();
                    task1EndTime = DateTimeOffset.Now;
                    processes.Add(1);
                    sync.End(1).Wait();
                });

                var task2 = Task.Run(() =>
                {
                    sync.Begin(2).Wait();
                    task2StartTime = DateTimeOffset.Now;
                    processes.Add(2);
                    sync.End(2).Wait();
                });

                sync.NotifyAll(false).Wait();


                // Assert
                CollectionAssert.AreEqual(new[] { 1, 2 }, processes);
                Assert.GreaterOrEqual(task2StartTime - task1EndTime, TimeSpan.FromMilliseconds(1000));
                Task.WaitAll(task1, task2);
            }
            finally
            {
                sync?.Dispose();
            }
        }
    }
}
