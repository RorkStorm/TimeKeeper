using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using Topshelf;
using Topshelf.Logging;
using Timer = System.Timers.Timer;

namespace TimeKeeper
{
    internal class TimeKeeperServiceOld2
    {
        private readonly LogWriter logger;
        public Dictionary<string, TimeCounter> users; 

        public TimeKeeperServiceOld2()
        {
            logger = HostLogger.Get<TimeKeeperServiceOld2>();
            configureUsers();
        }

        private void configureUsers()
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json");

            IConfiguration configuration = builder.Build();

            var timeCountersSection = configuration.GetSection("TimeCounters").GetChildren();

            users = new Dictionary<string, TimeCounter>();

            foreach (var item in timeCountersSection)
            {
                int defaultMinutes;
                if (int.TryParse(item.Value, out defaultMinutes))
                {
                    users.Add(item.Key, new TimeCounter { Day = DateTime.Today, Minutes = defaultMinutes, DefaultMinutes = defaultMinutes, LastLogOn = DateTime.Now });
                    logger.Debug($"Loading User : {item.Key} with Values : {users[item.Key]}");
                }
            }
        }

        public void Start()
        {
            logger.Debug("TimeKeeper service started.");
        }

        public void Stop()
        {
            logger.Debug("TimeKeeper service stopped.");
        }

        public void SessionSwitch(SessionChangedArguments chg)
        {
            string currentUser = WindowsUserFinderOld2.GetUsernameBySessionId(chg.SessionId, false);
            
            logger.Debug($"Session Changed for current user: {currentUser}");
            logger.Debug($"SessionChangeReasonCode = {chg.ReasonCode}");

            Timer timer = new Timer() { AutoReset = true };

            if ((chg.ReasonCode == SessionChangeReasonCode.SessionLogon || chg.ReasonCode == SessionChangeReasonCode.SessionUnlock) && users.ContainsKey(currentUser))
            {
                // A new session has started
                logger.Debug("A new session has started.");

                //If the session starts another day than the Svc startup date, we reset the time counter
                if (users[currentUser].Day != DateTime.Today)
                {
                    users[currentUser].Day = DateTime.Today;
                    users[currentUser].Minutes = users[currentUser].DefaultMinutes;
                }

                users[currentUser].LastLogOn = DateTime.Now;

                if (checkForKidsLogon(currentUser))
                {
                    logger.Debug($"Loading User : {currentUser} with Values : {users[currentUser]}");

                    timer.Interval = users[currentUser].Minutes * 60 * 1000;  // Convert minutes to milliseconds
                    timer.Elapsed += (sender, eventArgs) => forceLogout(currentUser, chg.SessionId);
                    timer.Start();
                }
                else
                {
                    forceLogout(currentUser, chg.SessionId);
                }
            }

            if (users.ContainsKey(currentUser) && (chg.ReasonCode == SessionChangeReasonCode.SessionLogoff || chg.ReasonCode == SessionChangeReasonCode.SessionLock))
            {
                logger.Debug("A session is closed.");
                users[currentUser].Minutes = calculateRemainingMinutes(users[currentUser]);
                timer.Stop();
            }
        }

        private void forceLogout(string currentUser, int sessionId)
        {
            logger.Debug($"Logout operation triggered for User {currentUser} and Session : {sessionId}");
            users[currentUser].Minutes = 0;

            bool result = WindowsUserFinderOld2.ForceLogout(sessionId);

            logger.Debug($"Logout operation status : {result}");
        }

        private bool checkForKidsLogon(string currentUser)
        {
            if (users[currentUser].Minutes == 0)
                return false;
            else
                return true;
        }

        private int calculateRemainingMinutes(TimeCounter timeCounter)
        {
            TimeSpan remainingTime = timeCounter.LastLogOn.AddMinutes(timeCounter.Minutes) - DateTime.Now;
            return (int)remainingTime.TotalMinutes > 0 ? (int)remainingTime.TotalMinutes : 0;
        }
    }
}


