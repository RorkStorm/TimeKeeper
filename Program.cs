// See https://aka.ms/new-console-template for more information
using Microsoft.Win32;
using TimeKeeper;
using Topshelf;

var rc = HostFactory.Run(x =>                                   
{
    x.Service<TimeKeeperService>(s =>                                   
    {
        s.ConstructUsing(name => new TimeKeeperService());               
        s.WhenStarted(tc => tc.Start());                         
        s.WhenStopped(tc => tc.Stop());
        s.WhenSessionChanged((tc, chg) => tc.SessionSwitch(chg));
    });

    x.RunAsLocalSystem();
    x.EnableSessionChanged();
    x.UseNLog();

    x.SetDescription("A Windows Service able to Log Out your kids after a certain amount of time");                   
    x.SetDisplayName("The TimeKeeper Service");                                  
    x.SetServiceName("TimeKeeperSvc");          
    
    x.StartAutomatically();
});                                                             

var exitCode = (int)Convert.ChangeType(rc, rc.GetTypeCode());  
Environment.ExitCode = exitCode;
