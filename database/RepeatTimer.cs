namespace Database
{

    public class RepeatTimer
    {
        public string Id { get; set; }
        public string Channel { get; set; }
        public string TimerName { get; set; }
        public DateTime NextExpireDate { get; set; }
        public int TimerInterval { get; set; }
        public string Message { get; set; }

        public RepeatTimer(string id, string channel, string timerName, DateTime nextExpireDate, int timerInterval, string message)
        {
            this.Id = id;
            this.Channel = channel;
            this.TimerName = timerName;
            this.NextExpireDate = nextExpireDate;
            this.TimerInterval = timerInterval;
            this.Message = message;
        }
    }
}