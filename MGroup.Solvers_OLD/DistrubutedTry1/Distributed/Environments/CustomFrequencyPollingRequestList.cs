using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using MPI;

namespace MGroup.Solvers_OLD.DistributedTry1.Distributed.Environments
{
    public class CustomFrequencyPollingRequestList
    {
        private readonly int pollingInterval;
        private readonly List<(Request, Action)> requests = new List<(Request, Action)>();

        /// <summary>
        /// Initializes a new instance of <see cref="CustomFrequencyPollingRequestList"/>.
        /// </summary>
        /// <param name="pollingInterval">The interval in milliseconds between 2 polling attempts.</param>
        public CustomFrequencyPollingRequestList(int pollingInterval = 100)
        {
            this.pollingInterval = pollingInterval;
        }

        /// <summary>
        /// Register a new request to keep track of and a callback that will be executed, once this request has completed.
        /// </summary>
        /// <param name="request">A request to keep track of.</param>
        /// <param name="action">A callback that will be executed, once <paramref name="request"/> has completed.</param>
        public void Register(Request request, Action action) => requests.Add((request, action));

        /// <summary>
        /// Register a new request to keep track of.
        /// </summary>
        /// <param name="request">A request to keep track of.</param>
        public void Register(Request request) => requests.Add((request, () => { }));

        public void WaitAll()
        {
            while (true)
            {
                if (requests.Count == 0) return;

                //requests.RemoveAll(req => req.Test() != null); // This would be nice, but how can I execute the callback?
                var indicesToRemove = new List<int>();
                for (int r = 0; r < requests.Count; ++r)
                {
                    (Request req, Action action) = requests[r];
                    if (req.Test() != null)
                    {
                        action();
                        indicesToRemove.Add(r);
                    }
                }
                for (int i = 0; i < indicesToRemove.Count; ++i)
                {
                    // Each time we remove an entry, the next index to remove must be decreased by 1.
                    requests.RemoveAt(indicesToRemove[i] - i);
                }

                if (requests.Count == 0) return;
                Thread.Sleep(pollingInterval);
            }
        }
    }
}
