﻿/*
 * MIT License
 *
 * Copyright (c) 2018 Clark Yang
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of 
 * this software and associated documentation files (the "Software"), to deal in 
 * the Software without restriction, including without limitation the rights to 
 * use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies 
 * of the Software, and to permit persons to whom the Software is furnished to do so, 
 * subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all 
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE 
 * SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace Loxodon.Framework.Messaging
{
    public abstract class SubjectBase
    {
        public abstract void Publish(object message);
    }

    public class Subject<T> : SubjectBase
    {
        private readonly object _lock = new object();
        private readonly Dictionary<string, WeakReference> subscriptions = new Dictionary<string, WeakReference>();
        public bool IsEmpty() { return subscriptions.Count <= 0; }

        public override void Publish(object message)
        {
            this.Publish((T)message);
        }

        public void Publish(T message)
        {
            WeakReference[] array = null;
            lock (_lock)
            {
                if (subscriptions.Count <= 0)
                    return;

                array = this.subscriptions.Values.ToArray();
            }

            if (array == null || array.Length <= 0)
                return;

            for (int i = 0; i < array.Length; i++)
            {
                Subscription subscription = array[i].Target as Subscription;
                if (subscription != null)
                    subscription.Publish(message);
            }
        }

        public IDisposable Subscribe(Action<T> action)
        {
            return new Subscription(this, action);
        }

        void Add(Subscription subscription)
        {
            lock (_lock)
            {
                var reference = new WeakReference(subscription, false);
                this.subscriptions.Add(subscription.Key, reference);
            }
        }

        void Remove(Subscription subscription)
        {
            lock (_lock)
            {
                this.subscriptions.Remove(subscription.Key);
            }
        }

        class Subscription : IDisposable
        {
            private readonly object _lock = new object();
            private Subject<T> parent;
            private Action<T> action;
            public string Key { get; private set; }

            public Subscription(Subject<T> parent, Action<T> action)
            {
                this.parent = parent;
                this.action = action;
                this.Key = Guid.NewGuid().ToString();
                this.parent.Add(this);
            }

            public void Publish(T message)
            {
                try
                {
                    action(message);
                }
                catch (Exception) { }
            }

            #region IDisposable Support
            private bool disposed = false;

            protected virtual void Dispose(bool disposing)
            {
                if (this.disposed)
                    return;

                lock (_lock)
                {
                    try
                    {
                        if (this.disposed)
                            return;

                        if (parent != null)
                        {
                            parent.Remove(this);
                            action = null;
                            parent = null;
                        }
                    }
                    catch (Exception) { }
                    disposed = true;
                }
            }

            ~Subscription()
            {
                Dispose(false);
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            #endregion
        }
    }
}
