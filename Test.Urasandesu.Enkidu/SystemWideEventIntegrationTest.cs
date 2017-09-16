/* 
 * File: SystemWideEventIntegrationTest.cs
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
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Urasandesu.Enkidu;
using Urasandesu.NAnonym.Mixins.System;
using ST = System.Threading;

namespace Test.Urasandesu.Enkidu
{
    [TestFixture]
    public class SystemWideEventIntegrationTest
    {
        [Test]
        public void Can_restrain_apps_to_wait_until_ending_process_in_order_of_all_apps()
        {
            // Arrange
            var processes = new ConcurrentBag<int>();
            void Synchronize(Action<ISynchronizer> action)
            {
                var begun = new HandledCallback((id, obj, opts) =>
                {
                    var threadId = ST::Thread.CurrentThread.ManagedThreadId;
                    Debug.WriteLine($"Begun Id: { id }, Obj: { obj }, Thread: { threadId }");
                });
                var ended = new HandledCallback((id, obj, opts) =>
                {
                    var threadId = ST::Thread.CurrentThread.ManagedThreadId;
                    Debug.WriteLine($"Ended Id: { id }, Obj: { obj }, Thread: { threadId }");
                });
                var waiter1 = Synchronizable.SystemWideEventWait("Foo", obj => (int)obj == 1, begun, ended);
                var waiter2 = Synchronizable.SystemWideEventWait("Bar", obj => (int)obj == 2, begun, ended);
                var waiter3 = Synchronizable.SystemWideEventWait("Baz", obj => (int)obj == 3, begun, ended);
                using (var sync = waiter1.Then(waiter2).Then(waiter3).GetSynchronizer())
                    action(sync);
            }


            // Act
            Synchronize(sync =>
            {
                var result_processes = new MarshalByRefAction<int>(i => processes.Add(i));
                var task1 = Task.Run(() =>
                AppDomain.CurrentDomain.RunAtIsolatedDomain(result_processes_ =>
                Synchronize(sync_ =>
                {
                    sync_.Begin(1).Wait();
                    result_processes_.Invoke(1);
                    sync_.End(1).Wait();
                }), result_processes));

                var task2 = Task.Run(() =>
                AppDomain.CurrentDomain.RunAtIsolatedDomain(result_processes_ =>
                Synchronize(sync_ =>
                {
                    sync_.Begin(2).Wait();
                    result_processes_.Invoke(2);
                    sync_.End(2).Wait();
                }), result_processes));

                var task3 = Task.Run(() =>
                AppDomain.CurrentDomain.RunAtIsolatedDomain(result_processes_ =>
                Synchronize(sync_ =>
                {
                    sync_.Begin(3).Wait();
                    result_processes_.Invoke(3);
                    sync_.End(3).Wait();
                }), result_processes));

                sync.NotifyAll(false).Wait();


                // Assert
                CollectionAssert.AreEqual(new[] { 1, 2, 3 }, processes);
                Task.WaitAll(task1, task2, task3);
            });
        }

        [Test]
        public void Can_restrain_apps_to_wait_until_beginning_start_of_all_apps()
        {
            // Arrange
            var starts = new ConcurrentBag<int>();
            var processes = new ConcurrentBag<int>();
            void Synchronize(Action<ISynchronizer> action)
            {
                var setter1 = Synchronizable.SystemWideEventSet("Foo", obj => (int)obj == 1);
                var setter2 = Synchronizable.SystemWideEventSet("Bar", obj => (int)obj == 2);
                using (var sync = setter1.And(setter2).GetSynchronizer())
                    action(sync);
            }


            // Act
            Synchronize(sync =>
            {
                var result_starts = new MarshalByRefAction<int>(i => starts.Add(i));
                var result_processes = new MarshalByRefAction<int>(i => processes.Add(i));
                var mre1 = new ST::ManualResetEvent(false);
                var task1 = Task.Run(() =>
                AppDomain.CurrentDomain.RunAtIsolatedDomain((result_starts_, result_processes_, mre1_) =>
                Synchronize(sync_ =>
                {
                    result_starts_.Invoke(1);
                    sync_.Begin(1).Wait();
                    mre1_.WaitOne(10000);
                    result_processes_.Invoke(1);
                    sync_.End(1).Wait();
                }), result_starts, result_processes, mre1));

                var mre2 = new ST::ManualResetEvent(false);
                var task2 = Task.Run(() =>
                AppDomain.CurrentDomain.RunAtIsolatedDomain((result_starts_, result_processes_, mre2_) =>
                Synchronize(sync_ =>
                {
                    result_starts_.Invoke(2);
                    sync_.Begin(2).Wait();
                    mre2_.WaitOne(10000);
                    result_processes_.Invoke(2);
                    sync_.End(2).Wait();
                }), result_starts, result_processes, mre2));

                sync.NotifyAll(false).Wait();


                // Assert
                CollectionAssert.AreEquivalent(new[] { 1, 2 }, starts);
                CollectionAssert.IsEmpty(processes);
                mre1.Set();
                mre2.Set();
                Task.WaitAll(task1, task2);
            });
        }

        [Test]
        public void Can_restrain_apps_to_wait_until_beginning_start_of_app1_or_app2_and_until_ending_process_of_app3_and_app4_in_order()
        {
            // Arrange
            var starts = new ConcurrentBag<int>();
            var processes = new ConcurrentBag<int>();
            void Synchronize(Action<ISynchronizer> action)
            {
                var setter1 = Synchronizable.SystemWideEventSet("Foo", obj => (int)obj == 1);
                var setter2 = Synchronizable.SystemWideEventSet("Bar", obj => (int)obj == 2);
                var waiter3 = Synchronizable.SystemWideEventWait("Baz", obj => (int)obj == 3);
                var waiter4 = Synchronizable.SystemWideEventWait("Qux", obj => (int)obj == 4);
                using (var sync = setter1.Or(setter2).And(waiter3.Then(waiter4)).GetSynchronizer())
                    action(sync);
            }


            // Act
            Synchronize(sync =>
            {
                var result_starts = new MarshalByRefAction<int>(i => starts.Add(i));
                var result_processes = new MarshalByRefAction<int>(i => processes.Add(i));
                var mre1 = new ST::ManualResetEvent(false);
                var task1 = Task.Run(() =>
                AppDomain.CurrentDomain.RunAtIsolatedDomain((result_starts_, result_processes_, mre1_) =>
                Synchronize(sync_ =>
                {
                    mre1_.WaitOne(10000);
                    result_starts_.Invoke(1);
                    sync_.Begin(1).Wait();
                    result_processes_.Invoke(1);
                    sync_.End(1).Wait();
                }), result_starts, result_processes, mre1));

                var task2 = Task.Run(() =>
                AppDomain.CurrentDomain.RunAtIsolatedDomain((result_starts_, result_processes_) =>
                Synchronize(sync_ =>
                {
                    result_starts_.Invoke(2);
                    sync_.Begin(2).Wait();
                    result_processes_.Invoke(2);
                    sync_.End(2).Wait();
                }), result_starts, result_processes));

                var task3 = Task.Run(() =>
                AppDomain.CurrentDomain.RunAtIsolatedDomain((result_starts_, result_processes_) =>
                Synchronize(sync_ =>
                {
                    result_starts_.Invoke(3);
                    sync_.Begin(3).Wait();
                    result_processes_.Invoke(3);
                    sync_.End(3).Wait();
                }), result_starts, result_processes));

                var task4 = Task.Run(() =>
                AppDomain.CurrentDomain.RunAtIsolatedDomain((result_starts_, result_processes_) =>
                Synchronize(sync_ =>
                {
                    result_starts_.Invoke(4);
                    sync_.Begin(4).Wait();
                    result_processes_.Invoke(4);
                    sync_.End(4).Wait();
                }), result_starts, result_processes));

                sync.NotifyAll(false).Wait();


                // Assert
                CollectionAssert.DoesNotContain(starts, 1);
                CollectionAssert.AreEqual(new[] { 3, 4 }, processes.Intersect(new[] { 3, 4 }));
                mre1.Set();
                Task.WaitAll(task1, task2, task3, task4);
            });
        }

        [Test]
        public void Should_ignore_empty_when_restraining_apps()
        {
            // Arrange
            var starts = new ConcurrentBag<int>();
            var processes = new ConcurrentBag<int>();
            void Synchronize(Action<ISynchronizer> action)
            {
                var empty = Synchronizable.Empty();
                var setter1 = Synchronizable.SystemWideEventSet("Foo", obj => (int)obj == 1);
                var setter2 = Synchronizable.SystemWideEventSet("Bar", obj => (int)obj == 2);
                var waiter3 = Synchronizable.SystemWideEventWait("Baz", obj => (int)obj == 3);
                var waiter4 = Synchronizable.SystemWideEventWait("Qux", obj => (int)obj == 4);
                using (var sync = setter1.Or(setter2).Or(empty).And(empty.Then(waiter3).Then(empty).Then(waiter4).Then(empty)).And(empty).GetSynchronizer())
                    action(sync);
            }


            // Act
            Synchronize(sync =>
            {
                var result_starts = new MarshalByRefAction<int>(i => starts.Add(i));
                var result_processes = new MarshalByRefAction<int>(i => processes.Add(i));
                var mre1 = new ST::ManualResetEvent(false);
                var task1 = Task.Run(() =>
                AppDomain.CurrentDomain.RunAtIsolatedDomain((result_starts_, result_processes_, mre1_) =>
                Synchronize(sync_ =>
                {
                    mre1_.WaitOne(10000);
                    result_starts_.Invoke(1);
                    sync_.Begin(1).Wait();
                    result_processes_.Invoke(1);
                    sync_.End(1).Wait();
                }), result_starts, result_processes, mre1));

                var task2 = Task.Run(() =>
                AppDomain.CurrentDomain.RunAtIsolatedDomain((result_starts_, result_processes_) =>
                Synchronize(sync_ =>
                {
                    result_starts_.Invoke(2);
                    sync_.Begin(2).Wait();
                    result_processes_.Invoke(2);
                    sync_.End(2).Wait();
                }), result_starts, result_processes));

                var task3 = Task.Run(() =>
                AppDomain.CurrentDomain.RunAtIsolatedDomain((result_starts_, result_processes_) =>
                Synchronize(sync_ =>
                {
                    result_starts_.Invoke(3);
                    sync_.Begin(3).Wait();
                    result_processes_.Invoke(3);
                    sync_.End(3).Wait();
                }), result_starts, result_processes));

                var task4 = Task.Run(() =>
                AppDomain.CurrentDomain.RunAtIsolatedDomain((result_starts_, result_processes_) =>
                Synchronize(sync_ =>
                {
                    result_starts_.Invoke(4);
                    sync_.Begin(4).Wait();
                    result_processes_.Invoke(4);
                    sync_.End(4).Wait();
                }), result_starts, result_processes));

                sync.NotifyAll(false).Wait();


                // Assert
                CollectionAssert.DoesNotContain(starts, 1);
                CollectionAssert.AreEqual(new[] { 3, 4 }, processes.Intersect(new[] { 3, 4 }));
                mre1.Set();
                Task.WaitAll(task1, task2, task3, task4);
            });
        }
    }
}
