using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DupeDetectorWorkerService
{
    public class FileHasherService
    {
        public static IConfiguration? _config { get; private set; }
        public static ILogger<FileHasherService> _logger { get; private set; }
        private string _connString;
        private static string _inFolder;
        private static string _outFolder;
        private static string _errorsFolder;
        private volatile bool _stayInLoop;
        private bool _filePathError;

        public FileHasherService(ILogger<FileHasherService> logger)
        {
            _stayInLoop = true;
            _logger = logger;

            _config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            _connString = _config.GetSection("ConnectionStrings:DupeConn").Value ?? ""; // ConfigurationManager["DupeConn"].ConnectionString;
            _inFolder = _config.GetSection("AppSettings:InFolder").Value ?? "c:\\temp\\";
            _outFolder = _config.GetSection("AppSettings:OutFolder").Value ?? "c:\\temp\\";
            _errorsFolder = _config.GetSection("AppSettings:ErrorsFolder").Value ?? "c:\\temp\\";

            if (!_inFolder.EndsWith("\\"))
            {
                _inFolder += "\\";
            }

            if (!_outFolder.EndsWith("\\"))
            {
                _outFolder += "\\";
            }
            if (!string.IsNullOrEmpty(_inFolder) && _inFolder.Length > 5)
            {
                _filePathError = false;
            }
        }

        public void RequestStop()
        {
            _stayInLoop = false;
        }

        public void DoLoop()
        {
            if (_filePathError)
            {
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogError("Error - Input file path is dangerous");
                }
                return;
            }
            int jobCount = 0;

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("******* Starting DupeCheck Loop ************************");
            }

            do
            {
                try
                {
                    DirectoryInfo di = new DirectoryInfo(_inFolder);
                    FileSystemInfo[] files = di.GetFileSystemInfos("*.*", SearchOption.TopDirectoryOnly);
                    var orderedFiles = files.OrderBy(f => f.LastWriteTime);

                    foreach (FileSystemInfo file in orderedFiles)
                    {
                        string justFileName = Path.GetFileName(file.FullName);
                        if (justFileName != null)
                        {
                            string hash = GetFileMD5Hash(file.FullName);

                            bool exists = ItemExistsInDB(justFileName, hash);
                            if (!exists)
                            {
                                try
                                {
                                    DataInsert(justFileName, hash);
                                    string targetFilePath = Path.Combine(_outFolder, justFileName);

                                    File.Copy(file.FullName, targetFilePath, true);
                                    File.Delete(file.FullName);
                                    Console.WriteLine(string.Format("File {0} was moved to output as it is a new file", justFileName));
                                    if (_logger.IsEnabled(LogLevel.Information))
                                    {
                                        _logger.Info(string.Format("File {0} was moved to output as it is a new file", justFileName));
                                    }
                                }
                                catch (Exception exception)
                                {
                                    Console.WriteLine(exception);
                                    if (_logger.IsEnabled(LogLevel.Error))
                                    {
                                        _logger.LogError("Data Insert or copy failed : " + exception.Message);
                                    }
                                }

                            }
                            else
                            {
                                try
                                {
                                    if (!string.IsNullOrEmpty(_errorsFolder))
                                    {
                                        string targetFilePath = Path.Combine(_errorsFolder, justFileName);
                                        File.Delete(targetFilePath);
                                        File.Move(file.FullName, targetFilePath);
                                        string logMessage = string.Format(
                                            "File {0} was moved to errors due to the file already being present in the DB",
                                            justFileName);
                                        Console.WriteLine(logMessage
                                            );
                                        if (_logger.IsEnabled(LogLevel.Information))
                                        {
                                            _logger.LogInformation(logMessage);
                                        }
                                    }
                                    else
                                    {
                                        File.Delete(file.FullName);
                                        string logMessage = string.Format(
                                                                                    "File {0} was deleted due to the file already being present in the DB",
                                                                                    justFileName);
                                        Console.WriteLine(logMessage
                                            );
                                        if (_logger.IsEnabled(LogLevel.Information))
                                        {
                                            _logger.LogInformation(logMessage);
                                        }
                                    }
                                }
                                catch (Exception exception)
                                {
                                    Console.WriteLine(exception);
                                    if (_logger.IsEnabled(LogLevel.Error))
                                    {
                                        _logger.LogError("Data move to error or delete failed : " + exception.Message);
                                    }
                                }
                            }

                            jobCount++;
                            Thread.Sleep(10);

                        }

                    }

                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                    if (_logger.IsEnabled(LogLevel.Error))
                    {
                        _logger.LogError("Get file listing failed : " + exception.Message);
                    }
                }

                Thread.Sleep(500);

            } while (_stayInLoop);

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("******* Ending DupeCheck Loop ************************");
            }
        }

        protected string GetFileMD5Hash(string fileName)
        {
            string hash = "";
            try
            {
                using (var fsSource = new BufferedStream(File.OpenRead(fileName), 1000000))
                {

                    // Read the source file into a byte array.
                    byte[] bytes = new byte[fsSource.Length];
                    int numBytesToRead = (int)fsSource.Length;
                    int numBytesRead = 0;
                    while (numBytesToRead > 0)
                    {
                        // Read may return anything from 0 to numBytesToRead.
                        int n = fsSource.Read(bytes, numBytesRead, numBytesToRead);

                        // Break when the end of the file is reached.
                        if (n == 0)
                            break;

                        numBytesRead += n;
                        numBytesToRead -= n;
                    }

                    var sha1 = System.Security.Cryptography.SHA1.Create();

                    hash = Convert.ToBase64String(sha1.ComputeHash(bytes));
                }
            }
            catch (FileNotFoundException ioEx)
            {
                Console.WriteLine(ioEx.Message);
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogError("Cannot find file : " + ioEx.Message);
                }
            }
            return hash;

        }

        protected void DataInsert(string fileName, string md5CheckSum)
        { }

        protected bool ItemExistsInDB(string fileName, string hash)
        {
            bool ret = false;

            return ret;
        }
    }
}
