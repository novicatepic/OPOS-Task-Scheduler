using DokanLab;
using DokanNet;
using DokanNet.Logging;
char driveLetter = 'Y';

using (ConsoleLogger consoleLogger = new("[Dokan]"))
using (Dokan dokan = new(consoleLogger))
{
    string mountPoint = $"{driveLetter}:\\";
    MyFs myFs = new();
    DokanInstanceBuilder dokanInstanceBuilder = new DokanInstanceBuilder(dokan)
        .ConfigureLogger(() => consoleLogger)
        .ConfigureOptions(options =>
        {
            options.Options = DokanOptions.DebugMode;
            options.MountPoint = mountPoint;
        });
    using DokanInstance dokanInstance = dokanInstanceBuilder.Build(myFs);
    Console.ReadLine();
}
