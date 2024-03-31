﻿using bg3_modders_multitool;
using System.Windows;
using System;
using bg3_modders_multitool.Views;
using CommandLine;
using System.Threading.Tasks;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Collections.Generic;

/// <summary>
/// Controls the application lifecycle to allow for on the fly language selection
/// </summary>
public class LocalizationController : Application
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Length > 0)
        {
            App app = new App();
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var wnd = new bg3_modders_multitool.ViewModels.MainWindow();
            app.MainWindow = new Window { DataContext = wnd };

            AttachConsole(-1);

            App.Current.Properties["console_app"] = 1;

            Parser.Default.ParseArguments<Cli>(args)
                .WithParsedAsync(Cli.Run)
                .ContinueWith(_ => Console.WriteLine(wnd.ConsoleOutput)).Wait();

            App.Current.Shutdown();
        }
        else
        {
            App app = new App();
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            MainWindow wnd = new MainWindow();
            wnd.Closed += Wnd_Closed;
            app.Run(wnd);
        }
    }

    private static void Wnd_Closed(object sender, EventArgs e)
    {
        MainWindow wnd = sender as MainWindow;
        var dataContext = (bg3_modders_multitool.ViewModels.MainWindow)wnd.DataContext;
        if (!string.IsNullOrEmpty(dataContext.SelectedLanguage))
        {
            bg3_modders_multitool.Properties.Settings.Default.selectedLanguage = dataContext.SelectedLanguage;
            bg3_modders_multitool.Properties.Settings.Default.Save();

            wnd.Closed -= Wnd_Closed;

            wnd = new MainWindow();
            wnd.Closed += Wnd_Closed;
            wnd.Show();
        }
        else
        {
            App.Current.Shutdown();
        }
    }

    [DllImport("kernel32.dll")]
    static extern bool AttachConsole(int dwProcessId);

    [DllImport("kernel32.dll")]
    private static extern bool FreeConsole();

    /// <summary>
    /// The available cli arguments
    /// </summary>
    private class Cli // TODO - set up translation resources for these
    {
        /// <summary>
        /// The source folder/file
        /// </summary>
        [Option('s', "source", Required = true, HelpText = "Input folder/file")]
        public string Source { get; set; }

        /// <summary>
        /// The destination folder/file
        /// </summary>
        [Option('d', "destination", Required = true, HelpText = "Output folder/file")]
        public string Destination { get; set; }

        /// <summary>
        /// The compression level to use (0-4)
        /// </summary>
        [Option('c', "compression", HelpText = "0: None\r\n1: LZ4\r\n2: LZ4 HC\r\n3: Zlib Fast\r\n4: Zlib Optimal")]
        public int Compression { get; set; }

        public string GetUsage()
        {
            return "Read wiki for usage instructions...";
        }

        /// <summary>
        /// Runs the multitool cli commands
        /// </summary>
        /// <param name="options">The cli options</param>
        /// <returns>The task</returns>
        public static async Task Run(Cli options)
        {
            var source = Path.GetFullPath(options.Source);
            var destination = Path.GetFullPath(options.Destination);
            var compression = bg3_modders_multitool.ViewModels.MainWindow.AvailableCompressionTypes.FirstOrDefault(c => c.Id == options.Compression);

            // Check source (must exist)
            var sourceIsDirectory = System.IO.Path.GetExtension(source) == string.Empty;
            if ((sourceIsDirectory && !Directory.Exists(source)) || (!sourceIsDirectory && !File.Exists(source)))
            {
                Console.WriteLine("Invalid source folder/pak, does not exist");
                return;
            }

            // Check destination
            var destinationExtension = System.IO.Path.GetExtension(destination);
            var destinationIsDirectory = destinationExtension == string.Empty;

            // Set global config
            App.Current.Properties["cli_source"] = source;
            App.Current.Properties["cli_destination"] = destination;
            App.Current.Properties["cli_compression"] = compression.Id;
            App.Current.Properties["cli_zip"] = destinationExtension == ".zip";

            if (sourceIsDirectory && !destinationIsDirectory)
            {
                Console.WriteLine("Packing mod...");
                
                DataObject data = new DataObject(DataFormats.FileDrop, new string[] { source });
                var dadVm = new bg3_modders_multitool.ViewModels.DragAndDropBox();
                await dadVm.ProcessDrop(data);
            }
            else if(destinationIsDirectory && !sourceIsDirectory)
            {
                var sourceExtension = System.IO.Path.GetExtension(source);
                if(sourceExtension == ".pak")
                {
                    Console.WriteLine("Unpacking mod...");

                    var vm = new bg3_modders_multitool.ViewModels.MainWindow();
                    await vm.Unpacker.UnpackPakFiles(new List<string> { source }, false);
                }
                else
                {
                    Console.WriteLine("Invalid source extension. File must be a .pak!");
                }
            }
            else
            {
                Console.WriteLine($"Invalid operation; source and destination cannot both be {(sourceIsDirectory&&destinationIsDirectory?"directories":"files")}!");
            }
        }
    }
}