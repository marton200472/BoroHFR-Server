using System.Collections;
using System.Collections.Concurrent;
using BoroHFR.Models;

namespace BoroHFR.Services
{
    public class EmailService
    {
        private ConcurrentQueue<Email> SendQueue = new();

        public bool Any => SendQueue.Count > 0;

        public event Action? EmailAdded;

        public virtual void Enqueue(Email email)
        {
            SendQueue.Enqueue(email);
            EmailAdded?.Invoke();
        }

        public Email Dequeue()
        {
            Email? email;
            var result = SendQueue.TryDequeue(out email);
            if (result)
                return email!;

            throw new InvalidOperationException();
        }

        public Email[] Snapshot()
        {
            return SendQueue.ToArray();
        }
    }
}
