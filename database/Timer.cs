namespace Database
{

    public class Timer
    {
        public string Id { get; set; }
        public string Channel { get; set; }
        public string TimerName { get; set; }
        public double ExpireTime { get; set; } // Unix timestamp when the timer expires
        public string Message { get; set; }

        public Timer(string id, string channel, string timerName, double expireTime, string message)
        {
            this.Id = id;
            this.Channel = channel;
            this.TimerName = timerName;
            this.ExpireTime = expireTime;
            this.Message = message;
        }
    }
}