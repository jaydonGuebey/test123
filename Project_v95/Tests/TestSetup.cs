using log4net;
using log4net.Config;
using NUnit.Framework;
using System.IO;
using System.Reflection;

// Deze attribute vertelt NUnit dat dit een setup-klasse is
[SetUpFixture]
public class TestSetup
{
    // Deze methode draait één keer voordat de allereerste test start
    [OneTimeSetUp]
    public void GlobalSetup()
    {
        // 1. Zoek de log4net.config
        var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
        
        // 2. Laad de configuratie
        XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

        // 3. Schrijf een startbericht
        var log = LogManager.GetLogger(typeof(TestSetup));
        log.Info("==========================================");
        log.Info("Test Run Gestart. Log4Net geconfigureerd.");
        log.Info("==========================================");
    }

    [OneTimeTearDown]
    public void GlobalTeardown()
    {
        // Optioneel: log als de hele test run klaar is
        var log = LogManager.GetLogger(typeof(TestSetup));
        log.Info("==========================================");
        log.Info("Test Run Voltooid.");
        log.Info("==========================================");
    }
}