namespace Database
{

    public class Timer
    {
        public string Id { get; set; }
        public string Channel { get; set; }
        public string TimerName { get; set; }
        public double ExpireTime { get; set; } // Unix timestamp when the timer expires
        public string Message { get; set; }

        public Timer(string id, string channel, string timerName, double expireDate, string message)
        {
            this.Id = id;
            this.Channel = channel;
            this.TimerName = timerName;
            this.ExpireTime = expireDate;
            this.Message = message;
        }

        public Timer() { }
    }
}