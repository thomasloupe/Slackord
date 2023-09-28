namespace Slackord.Classes
{
    using System.Collections.Generic;

    public class Thread
    {
        public string ThreadTs { get; set; }
        public List<Message> Messages { get; set; } = new List<Message>();
    }

    public class ThreadsManager
    {
        private readonly List<Thread> threads = new();

        public void AddMessage(Message message, string threadTs)
        {
            var thread = threads.Find(t => t.ThreadTs == threadTs);
            if (thread == null)
            {
                thread = new Thread { ThreadTs = threadTs };
                threads.Add(thread);
            }
            thread.Messages.Add(message);
        }

        public List<Thread> GetAllThreads()
        {
            return threads;
        }
    }
}
