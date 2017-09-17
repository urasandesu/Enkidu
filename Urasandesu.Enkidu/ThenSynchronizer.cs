/* 
 * File: ThenSynchronizer.cs
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



using System.Threading.Tasks;

namespace Urasandesu.Enkidu
{
    public class ThenSynchronizer : BinarySynchronizer
    {
        public ThenSynchronizer(ISynchronizer lhs, ISynchronizer rhs) :
            base(lhs, rhs)
        { }

        public override bool WillBegin(object obj, SynchronousOptions opts = null)
        {
            if (LeftSynchronizer is BinarySynchronizer && LeftSynchronizer.WillBegin(obj, opts))
                return true;
            else
                return RightSynchronizer.WillBegin(obj, opts);
        }

        public override async Task Begin(object obj, SynchronousOptions opts = null)
        {
            if (LeftSynchronizer is BinarySynchronizer && LeftSynchronizer.WillBegin(obj, opts))
                await LeftSynchronizer.Begin(obj, opts);
            else
                await RightSynchronizer.Begin(obj, opts);
        }

        public override bool WillEnd(object obj, SynchronousOptions opts = null)
        {
            var willLeftBegin = LeftSynchronizer.WillBegin(obj);
            var willRightBegin = RightSynchronizer.WillBegin(obj);
            if (willLeftBegin && willRightBegin)
                return false;
            else if (willLeftBegin)
                return RightSynchronizer.WillEnd(obj, SynchronousOptions.UpdateInternalOptions(opts, new InternalSynchronousOptions().WithHandlingCondition()));
            else if (willRightBegin)
                return LeftSynchronizer.WillEnd(obj, SynchronousOptions.UpdateInternalOptions(opts, new InternalSynchronousOptions().WithHandlingCondition()));
            else
                return LeftSynchronizer.WillEnd(obj, opts);
        }

        public override async Task End(object obj, SynchronousOptions opts = null)
        {
            var willLeftBegin = LeftSynchronizer.WillBegin(obj);
            var willRightBegin = RightSynchronizer.WillBegin(obj);
            if (willLeftBegin && willRightBegin)
                await Task.CompletedTask;
            else if (willLeftBegin)
                await RightSynchronizer.End(obj, SynchronousOptions.UpdateInternalOptions(opts, new InternalSynchronousOptions().WithHandlingCondition()));
            else if (willRightBegin)
                await LeftSynchronizer.End(obj, SynchronousOptions.UpdateInternalOptions(opts, new InternalSynchronousOptions().WithHandlingCondition()));
            else
                await LeftSynchronizer.End(obj, opts);
        }

        public override async Task NotifyAll(bool state)
        {
            await LeftSynchronizer.NotifyAll(state);
            await RightSynchronizer.NotifyAll(state);
        }
    }
}
