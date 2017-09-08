/* 
 * File: SystemWideEventSetter.cs
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
using System.Threading.Tasks;

namespace Urasandesu.Enkidu
{
    public class SystemWideEventSetter : SystemWideEventSynchronizer
    {
        public SystemWideEventSetter(SynchronousId id, string name, Predicate<object> willHandle,
            HandledCallback begun = null, HandledCallback ended = null, AllNotifiedCallback allNotified = null) :
            base(id, name, willHandle, begun, ended, allNotified)
        { }

        public override Task Begin(object obj, SynchronousOptions opts = null)
        {
            return Task.Run(() =>
            {
                if (opts?.InternalOptions?.IgnoresHandlingCondition == true || WillHandle(obj))
                {
                    OnBegun(obj, opts);
                    WaitHandle.Set();
                }
            });
        }

        public override Task End(object obj, SynchronousOptions opts = null)
        {
            OnEnded(obj, opts);
            return Task.CompletedTask;
        }
    }
}
