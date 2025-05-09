﻿using DupeDetectorWorkerService.database;
using Microsoft.EntityFrameworkCore;

namespace DupeDetectorWorkerService
{
    public class FileHasherService
    {
        public IConfiguration? _config { get; private set; }
        public ILogger<FileHasherService> _logger { get; private set; }
        private string _connString;
        private string _inFolder;
        private string _outFolder;
        private string _errorsFolder;
        private volatile bool _stayInLoop;
        private bool _filePathError;
        IDbContextFactory<DupeDBContext> _dupeDbContextFactory;

        public FileHasherService(ILogger<FileHasherService> logger, IDbContextFactory<DupeDBContext> dupeDbContextFactory)
        {
            _stayInLoop = true;
            _logger = logger;

            _dupeDbContextFactory = dupeDbContextFactory;

            using (var dbContext = _dupeDbContextFactory.CreateDbContext())
            {

                dbContext.Database.Migrate();
            }

            _config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            _connString = _config.GetSection("ConnectionStrings:DupeConn").Value ?? ""; // ConfigurationManager["DupeConn"].ConnectionString;
            _inFolder = _config.GetSection("AppSettings:InFolder").Value ?? "c:\\temp\\";
            _outFolder = _config.GetSection("AppSettings:OutFolder").Value ?? "c:\\temp\\";
            _errorsFolder = _config.GetSection("AppSettings:ErrorsFolder").Value ?? String.Empty;

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

        public async void DoLoop()
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
                                    bool isError = await DataInsert(justFileName, hash);
                                    if (isError == false)
                                    {
                                        string targetFilePath = Path.Combine(_outFolder, justFileName);

                                        File.Copy(file.FullName, targetFilePath, true);
                                        File.Delete(file.FullName);
                                        Console.WriteLine(string.Format("File {0} was moved to output as it is a new file", justFileName));
                                        if (_logger.IsEnabled(LogLevel.Information))
                                        {
                                            _logger.LogInformation(string.Format("File {0} was moved to output as it is a new file", justFileName));
                                        }
                                    }
                                    else
                                    {
                                        File.Delete(file.FullName);
                                        if (_logger.IsEnabled(LogLevel.Error))
                                        {
                                            _logger.LogError(string.Format("File {0} caused an error on DB insertion and has been deleted.", justFileName));
                                        }
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

        protected async Task<bool> DataInsert(string fileName, string md5CheckSum)
        {
            bool isError = false;
            try
            {
                DuplicateFile dupeFile = new DuplicateFile(fileName, md5CheckSum);

                using (var dbContext = _dupeDbContextFactory.CreateDbContext())
                {
                    await dbContext.AddAsync(dupeFile);

                    await dbContext.SaveChangesAsync();
                }
            }
            catch (Exception)
            {
                isError = true;
            }
            return isError;
        }

        protected bool ItemExistsInDB(string fileName, string hash)
        {
            bool ret = false;
            using (var dbContext = _dupeDbContextFactory.CreateDbContext())
            {
                ret = dbContext.DuplicateFile.Where(a => a.FileName == fileName && a.Md5CheckSum == hash).Any();
            }

            return ret;
        }
    }
}
