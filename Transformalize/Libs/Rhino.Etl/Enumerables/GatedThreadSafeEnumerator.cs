#region License

// /*
// Transformalize - Replicate, Transform, and Denormalize Your Data...
// Copyright (C) 2013 Dale Newman
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
// */

#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Transformalize.Libs.Rhino.Etl.Enumerables
{
    /// <summary>
    ///     An iterator to be consumed by concurrent threads only which supplies an element of the decorated enumerable one by one
    /// </summary>
    /// <typeparam name="T">The type of the decorated enumerable</typeparam>
    public class GatedThreadSafeEnumerator<T> : WithLoggingMixin, IEnumerable<T>, IEnumerator<T>
    {
        private readonly IEnumerator<T> innerEnumerator;
        private readonly int numberOfConsumers;
        private readonly object sync = new object();
        private int callsToMoveNext;
        private int consumersLeft;
        private T current;
        private bool moveNext;

        /// <summary>
        ///     Creates a new instance of <see cref="GatedThreadSafeEnumerator{T}" />
        /// </summary>
        /// <param name="numberOfConsumers">The number of consumers that will be consuming this iterator concurrently</param>
        /// <param name="source">The decorated enumerable that will be iterated and fed one element at a time to all consumers</param>
        public GatedThreadSafeEnumerator(int numberOfConsumers, IEnumerable<T> source)
        {
            this.numberOfConsumers = numberOfConsumers;
            consumersLeft = numberOfConsumers;
            innerEnumerator = source.GetEnumerator();
        }

        /// <summary>
        ///     Number of consumers    left that have not yet completed
        /// </summary>
        public int ConsumersLeft
        {
            get { return consumersLeft; }
        }

        /// <summary>
        ///     Get    the    enumerator
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator()
        {
            return this;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        ///     Dispose    the    enumerator
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.Decrement(ref consumersLeft) == 0)
            {
                Debug("Disposing inner enumerator");
                innerEnumerator.Dispose();
            }
        }

        /// <summary>
        ///     MoveNext the enumerator
        /// </summary>
        /// <returns></returns>
        public bool MoveNext()
        {
            lock (sync)
                if (Interlocked.Increment(ref callsToMoveNext) == numberOfConsumers)
                {
                    callsToMoveNext = 0;
                    moveNext = innerEnumerator.MoveNext();
                    current = innerEnumerator.Current;

                    Debug("Pulsing all waiting threads");

                    Monitor.PulseAll(sync);
                }
                else
                {
                    Monitor.Wait(sync);
                }

            return moveNext;
        }

        /// <summary>
        ///     Reset the enumerator
        /// </summary>
        public void Reset()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///     The    current    value of the enumerator
        /// </summary>
        public T Current
        {
            get { return current; }
        }

        object IEnumerator.Current
        {
            get { return ((IEnumerator<T>) this).Current; }
        }
    }
}