/* 
 * File: Synchronizable.cs
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

namespace Urasandesu.Enkidu
{
    public static class Synchronizable
    {
        public static ISynchronizable EventWait(Predicate<object> willHandle,
            HandledCallback begun = null, HandledCallback ended = null, AllNotifiedCallback allNotified = null)
        {
            if (willHandle == null)
                throw new ArgumentNullException(nameof(willHandle));

            return new EventWaitable(willHandle, begun, ended, allNotified);
        }

        public static ISynchronizable EventSet(Predicate<object> willHandle,
            HandledCallback begun = null, HandledCallback ended = null, AllNotifiedCallback allNotified = null)
        {
            if (willHandle == null)
                throw new ArgumentNullException(nameof(willHandle));

            return new EventSettable(willHandle, begun, ended, allNotified);
        }

        public static ISynchronizable SystemWideEventWait(string name, Predicate<object> willHandle,
            HandledCallback begun = null, HandledCallback ended = null, AllNotifiedCallback allNotified = null)
        {
            if (willHandle == null)
                throw new ArgumentNullException(nameof(willHandle));

            return new SystemWideEventWaitable(name, willHandle, begun, ended, allNotified);
        }

        public static ISynchronizable SystemWideEventSet(string name, Predicate<object> willHandle,
            HandledCallback begun = null, HandledCallback ended = null, AllNotifiedCallback allNotified = null)
        {
            if (willHandle == null)
                throw new ArgumentNullException(nameof(willHandle));

            return new SystemWideEventSettable(name, willHandle, begun, ended, allNotified);
        }

        public static ISynchronizable Then(this ISynchronizable lhs, ISynchronizable rhs)
        {
            if (lhs == null)
                throw new ArgumentNullException(nameof(lhs));

            if (rhs == null)
                throw new ArgumentNullException(nameof(rhs));

            return new ThenSynchronizable(lhs, rhs);
        }

        public static ISynchronizable And(this ISynchronizable lhs, ISynchronizable rhs)
        {
            if (lhs == null)
                throw new ArgumentNullException(nameof(lhs));

            if (rhs == null)
                throw new ArgumentNullException(nameof(rhs));

            return new AndSynchronizable(lhs, rhs);
        }

        public static ISynchronizable Or(this ISynchronizable lhs, ISynchronizable rhs)
        {
            if (lhs == null)
                throw new ArgumentNullException(nameof(lhs));

            if (rhs == null)
                throw new ArgumentNullException(nameof(rhs));

            return new OrSynchronizable(lhs, rhs);
        }
    }
}
