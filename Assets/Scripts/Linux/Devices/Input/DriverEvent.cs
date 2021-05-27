using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Linux.Devices.Input
{
    public class DriverEvent<T> {
        List<ConcurrentQueue<T>> _listeners;

        public DriverEvent() {
            _listeners = new List<ConcurrentQueue<T>>();
        }

        public void Receive(T key) {
            _listeners.ForEach(queue => {
                queue.Enqueue(key);
            });
        }

        public void Subscribe(ConcurrentQueue<T> queue) {
            _listeners.Add(queue);
        }
    }
}