using log4net;
using log4net.Config;
using NUnit.Framework;
using System.IO;
using System.Reflection;

/*
 * ===================================================================================
 * RAPPORTAGE OVERZICHT (TestSetup - Globale Configuratie)
 * ===================================================================================
 * Dit bestand beheert de eenmalige, globale configuratie van het NUnit-testproject
 * VOORDAT de eerste test begint.
 * * Wat wordt gerapporteerd:
 * * 1. Logging Initialisatie: De [OneTimeSetUp] methode zorgt ervoor dat het 
 * log4net framework wordt geconfigureerd door het 'log4net.config' bestand 
 * in te lezen.
 * * 2. Audit Trail Framework: Dit garandeert dat alle individuele tests 
 * hun output correct wegschrijven naar het centrale logbestand, wat cruciaal is 
 * voor de foutopsporing en de audit-rapportage (zoals gevalideerd in BE-2.2.1).
 * * 3. Test Run Status: Registreert de start en het einde van de gehele testsuite
 * (via [OneTimeSetUp] en [OneTimeTearDown]).
 * ===================================================================================
 */

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
        // log als de hele test run klaar is
        var log = LogManager.GetLogger(typeof(TestSetup));
        log.Info("==========================================");
        log.Info("Test Run Voltooid.");
        log.Info("==========================================");
    }
}