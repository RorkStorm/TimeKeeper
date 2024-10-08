﻿using Microsoft.Extensions.Configuration;
using Topshelf;
using Topshelf.Logging;
using Timer = System.Timers.Timer;

namespace TimeKeeper
{
    internal class TimeKeeperService
    {
        private readonly LogWriter logger;
        private readonly Dictionary<string, TimeCounter> users;
        private Timer sessionTimer;

        public TimeKeeperService()
        {
            logger = HostLogger.Get<TimeKeeperService>();
            users = new Dictionary<string, TimeCounter>();
            ConfigureUsers();
        }

        private void ConfigureUsers()
        {
            try
            {
                var builder = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                IConfiguration configuration = builder.Build();
                var timeCountersSection = configuration.GetSection("TimeCounters").GetChildren();

                foreach (var item in timeCountersSection)
                {
                    if (int.TryParse(item.Value, out int defaultMinutes))
                    {
                        users.Add(item.Key, new TimeCounter
                        {
                            Day = DateTime.Today,
                            Minutes = defaultMinutes,
                            DefaultMinutes = defaultMinutes,
                            LastLogOn = DateTime.Now
                        });
                        logger.Debug($"Loading User: {item.Key} with Values: {users[item.Key]}");
                    }
                }
            }
            catch (FileNotFoundException ex)
            {
                logger.Error("Configuration file not found.", ex);
                throw;
            }
            catch (Exception ex)
            {
                logger.Error("Error configuring users.", ex);
                throw;
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

        public void SessionSwitch(SessionChangedArguments sessionArgs)
        {
            try
            {
                string currentUser = WindowsUserFinder.GetUsernameBySessionId(sessionArgs.SessionId, false);
                logger.Debug($"Session Changed for current user: {currentUser}");
                logger.Debug($"SessionChangeReasonCode = {sessionArgs.ReasonCode}");

                if (users.ContainsKey(currentUser))
                {
                    HandleSessionChange(sessionArgs, currentUser);
                }
            }
            catch (Exception ex)
            {
                logger.Error("Error handling session switch.", ex);
            }
        }

        private void HandleSessionChange(SessionChangedArguments sessionArgs, string currentUser)
        {
            try
            {
                if (sessionArgs.ReasonCode == SessionChangeReasonCode.SessionLogon || sessionArgs.ReasonCode == SessionChangeReasonCode.SessionUnlock)
                {
                    HandleSessionStart(currentUser, sessionArgs.SessionId);
                }
                else if (sessionArgs.ReasonCode == SessionChangeReasonCode.SessionLogoff || sessionArgs.ReasonCode == SessionChangeReasonCode.SessionLock)
                {
                    HandleSessionEnd(currentUser);
                }
            }
            catch (Exception ex)
            {
                logger.Error("Error handling session change.", ex);
            }
        }

        private void HandleSessionStart(string currentUser, int sessionId)
        {
            logger.Debug("A new session has started.");

            if (users[currentUser].Day != DateTime.Today)
            {
                ResetUserTimeCounter(currentUser);
            }

            users[currentUser].LastLogOn = DateTime.Now;

            if (IsUserAllowedToLogon(currentUser))
            {
                StartSessionTimer(currentUser, sessionId);
            }
            else
            {
                ForceLogout(currentUser, sessionId);
            }
        }

        private void HandleSessionEnd(string currentUser)
        {
            logger.Debug("A session is closed.");
            users[currentUser].Minutes = CalculateRemainingMinutes(users[currentUser]);
            sessionTimer?.Stop();
        }

        private void ResetUserTimeCounter(string currentUser)
        {
            users[currentUser].Day = DateTime.Today;
            users[currentUser].Minutes = users[currentUser].DefaultMinutes;
        }

        private bool IsUserAllowedToLogon(string currentUser)
        {
            return users[currentUser].Minutes > 0;
        }

        private void StartSessionTimer(string currentUser, int sessionId)
        {
            sessionTimer = new Timer(users[currentUser].Minutes * 60 * 1000) { AutoReset = false };
            sessionTimer.Elapsed += (sender, eventArgs) => ForceLogout(currentUser, sessionId);
            sessionTimer.Start();
            logger.Debug($"Loading User: {currentUser} with Values: {users[currentUser]}");
        }

        private void ForceLogout(string currentUser, int sessionId)
        {
            logger.Debug($"Logout operation triggered for User {currentUser} and Session: {sessionId}");
            users[currentUser].Minutes = 0;

            //bool result = WindowsUserFinder.ForceLogout(sessionId);
            bool result = WindowsUserFinder.ForceLockFromSessionId(sessionId);

            /*if(!result)
            {
                logger.Debug($"Lock operation failed. Trying a LogOff operation.");

                result = WindowsUserFinder.ForceLogout(sessionId);
            }*/

            logger.Debug($"Logout operation status: {result}");
        }

        private int CalculateRemainingMinutes(TimeCounter timeCounter)
        {
            TimeSpan remainingTime = timeCounter.LastLogOn.AddMinutes(timeCounter.Minutes) - DateTime.Now;
            return Math.Max((int)remainingTime.TotalMinutes, 0);
        }
    }
}