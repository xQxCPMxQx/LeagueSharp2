#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 ProducerConsumer.cs is part of SFXWard.

 SFXWard is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXWard is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXWard. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace SFXWard.Library
{
    public abstract class ProducerConsumer<T> : IDisposable
    {
        private readonly int _checkInterval;
        private readonly int _maxConsumers;
        private readonly int _minConsumers;

        private readonly Dictionary<CancellationTokenSource, Task> _pool =
            new Dictionary<CancellationTokenSource, Task>();

        private readonly int _producersPerConsumer;
        private readonly BlockingCollection<T> _queue = new BlockingCollection<T>();
        private int _lastCheck;
        private int _requestedStarting;
        private int _requestedStopping;
        private int _started;

        protected ProducerConsumer(int minConsumers = 1,
            int maxConsumers = 10,
            int producersPerConsumer = 5,
            int checkInterval = 10000)
        {
            _minConsumers = minConsumers;
            _maxConsumers = maxConsumers;
            _producersPerConsumer = producersPerConsumer;
            _checkInterval = checkInterval;
            ManageConsumers();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ProducerConsumer()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_pool != null)
                {
                    foreach (var consumer in _pool.ToList())
                    {
                        try
                        {
                            if (consumer.Key != null && !consumer.Key.IsCancellationRequested)
                            {
                                consumer.Key.Cancel();
                                consumer.Key.Dispose();
                            }
                        }
                        catch
                        {
                            //ignored
                        }
                    }
                }
                _queue.Dispose();
            }
        }

        public void AddItem(T item)
        {
            if (_queue != null && !_queue.IsAddingCompleted)
            {
                _queue.Add(item);
                ManageConsumers();
            }
        }

        public void CompleteAdding()
        {
            if (_queue != null && !_queue.IsAddingCompleted)
            {
                _queue.CompleteAdding();
            }
        }

        private void StartConsumers(int count)
        {
            _requestedStarting += count;
            for (var i = 0; count > i; i++)
            {
                var token = new CancellationTokenSource();
                _pool.Add(token, Task.Factory.StartNew(() => Consume(token), token.Token));
            }
        }

        private void StopConsumers(int count)
        {
            _requestedStopping += count;
            var i = 0;
            foreach (var consumer in _pool)
            {
                if (i >= count)
                {
                    break;
                }
                consumer.Key.Cancel();
                i++;
            }
        }

        private void ManageConsumers()
        {
            if (_queue.IsAddingCompleted && _pool.Count == 0)
            {
                return;
            }

            var consumers = _started + _requestedStarting - _requestedStopping;

            if (_queue.IsAddingCompleted)
            {
                StopConsumers(consumers);
            }
            else
            {
                if (_minConsumers > consumers)
                {
                    StartConsumers(_minConsumers - consumers);
                }
                else if (_maxConsumers < consumers)
                {
                    var consumersToRun = Convert.ToInt32(Math.Ceiling((double) _queue.Count / _producersPerConsumer));
                    consumersToRun = consumersToRun < _minConsumers
                        ? _minConsumers
                        : (consumersToRun > _maxConsumers ? _maxConsumers : consumersToRun);
                    if (consumersToRun > consumers)
                    {
                        StartConsumers(consumersToRun - consumers);
                    }
                    else if (consumersToRun < consumers)
                    {
                        if (_checkInterval + _lastCheck <= Environment.TickCount)
                        {
                            StopConsumers(consumers - consumersToRun);
                            _lastCheck = Environment.TickCount;
                        }
                    }
                }
            }
        }

        private void Consume(CancellationTokenSource token)
        {
            _started++;
            _requestedStarting--;
            foreach (var item in _queue.GetConsumingEnumerable())
            {
                ProcessItem(item);
                if (token.IsCancellationRequested)
                {
                    _pool.Remove(token);
                    _requestedStopping--;
                    _started--;
                    break;
                }
                ManageConsumers();
            }
        }

        protected abstract void ProcessItem(T item);
    }
}