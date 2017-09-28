/* 
 * File: EventSynchronizer.cs
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



using System;
using System.Threading;
using System.Threading.Tasks;

namespace Urasandesu.Enkidu
{
    public abstract class EventSynchronizer : IIdentifiableSynchronizer
    {
        readonly HandledCallback m_begun;
        readonly HandledCallback m_ended;
        readonly AllNotifiedCallback m_allNotified;

        protected EventSynchronizer(SynchronousId id, Predicate<object> willHandle,
            HandledCallback begun = null, HandledCallback ended = null, AllNotifiedCallback allNotified = null)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            if (willHandle == null)
                throw new ArgumentNullException(nameof(willHandle));

            Id = id;
            WillHandle = willHandle;
            m_begun = begun;
            m_ended = ended;
            m_allNotified = allNotified;
        }

        public SynchronousId Id { get; }
        protected ManualResetEventSlim WaitHandle { get; } = new ManualResetEventSlim(false);
        protected Predicate<object> WillHandle { get; }

        public abstract bool WillBegin(object obj, SynchronousOptions opts = null);

        public abstract Task Begin(object obj, SynchronousOptions opts = null);

        protected void OnBegun(object obj, SynchronousOptions opts = null)
        {
            m_begun?.Invoke(Id, obj, opts);
        }

        public abstract bool WillEnd(object obj, SynchronousOptions opts = null);

        public abstract Task End(object obj, SynchronousOptions opts = null);

        protected void OnEnded(object obj, SynchronousOptions opts = null)
        {
            m_ended?.Invoke(Id, obj, opts);
        }

        public Task NotifyAll(bool state)
        {
            if (state)
            {
                return Task.Run(() =>
                {
                    m_allNotified?.Invoke(Id, state);
                    WaitHandle.Set();
                });
            }
            else
            {
                return Task.Run(() =>
                {
                    m_allNotified?.Invoke(Id, state);
                    WaitHandle.Wait();
                });
            }
        }

        bool m_disposed;


        protected virtual void Dispose(bool disposing)
        {
            if (!m_disposed)
            {
                if (disposing)
                {
                    WaitHandle.Dispose();
                }

                m_disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
