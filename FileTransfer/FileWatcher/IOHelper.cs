using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileTransfer.FileWatcher
{
    public class IOHelper
    {
        #region 变量
        private static ILog _logger = LogManager.GetLogger(typeof(IOHelper));
        private Dictionary<string, bool> _deleteFilesDic;
        private Dictionary<string, bool> _deleteSubdirectoryDic;
        #endregion

        #region 单例
        private static IOHelper _instance;

        public static IOHelper Instance
        {
            get { return _instance ?? (_instance = new IOHelper()); }
        }

        #endregion

        #region 构造函数
        public IOHelper()
        {
            _deleteFilesDic = new Dictionary<string, bool>();
            _deleteSubdirectoryDic = new Dictionary<string, bool>();
        }
        #endregion

        #region 方法

        public bool HasMonitorDirectory(string path)
        {
            if (Directory.Exists(path))
                return true;
            else
                return false;
        }

        public List<string> GetAllFiles(string path)
        {
            try
            {
                DirectoryInfo dirInfo = new DirectoryInfo(path);
                List<string> result = new List<string>();
                FileInfo[] fileInfos = dirInfo.GetFiles();
                if (fileInfos.Length > 0)
                    result.AddRange(fileInfos.Select(f => f.FullName));
                foreach (DirectoryInfo subDirInfo in dirInfo.GetDirectories())
                {
                    result.AddRange(GetAllFiles(subDirInfo.FullName));
                }
                return result;
            }
            catch (Exception e)
            {
                _logger.Error(string.Format("获取文件夹{0}下的文件信息时发生错误！错误信息:{1}", path, e.Message));
                return null;
            }
        }

        public void DeleteFiles(List<string> filesPath)
        {
            foreach (var file in filesPath)
            {
                DeleteFile(file);
            }
        }

        private void DeleteFile(string file)
        {
            try
            {
                File.Delete(file);
            }
            catch (Exception e)
            {
                _logger.Error(string.Format(string.Format("删除文件{0}时发生错误！错误信息：{1}", file, e.Message)));
            }
        }

        public void TryDeleteFile(string monitorDirectory, string file)
        {
            if (!_deleteFilesDic.Keys.Contains(monitorDirectory) || _deleteFilesDic[monitorDirectory] == false) return;
            DeleteFile(file);
        }

        public List<string> GetAllSubDirectories(string path)
        {
            try
            {
                DirectoryInfo dirInfo = new DirectoryInfo(path);
                return dirInfo.GetDirectories().Select(d => d.FullName).ToList();
            }
            catch (Exception e)
            {
                _logger.Error(string.Format("获取{0}下的子文件夹信息时发生错误！错误信息为：{1}", path, e.Message));
                return null;
            }
        }

        public void DeleteDirectories(List<string> directoriesPath)
        {
            foreach (var directory in directoriesPath)
            {
                DeleteDirectory(directory);
            }
        }

        private void DeleteDirectory(string directory)
        {
            try
            {
                Directory.Delete(directory, true);
            }
            catch (Exception e)
            {
                _logger.Error(string.Format(string.Format("删除文件夹{0}时发生错误！错误信息：{1}", directory, e.Message)));
            }
        }

        //删除增量文件对应的子文件夹
        public void TryDeleteSubdirectories(string monitorDirectory, List<string> incrementFiles)
        {
            List<string> subdirectories = new List<string>();
            List<string> dirs = incrementFiles.Select(f => (new FileInfo(f)).DirectoryName).ToList();
            foreach (var d in dirs)
            {
                var temp = d.Replace(monitorDirectory, "");
                if (string.IsNullOrEmpty(temp)) continue;
                string[] strs = temp.Split('\\');
                if (strs.Length < 2) continue;
                temp = Path.Combine(monitorDirectory, strs[1]);
                subdirectories.Add(temp);
            }
            if (subdirectories.Count <= 0) return;
            subdirectories = subdirectories.Distinct().ToList();
            DeleteDirectories(subdirectories);
        }

        public void CheckAndCreateDirectory(string fileName)
        {
            FileInfo fileInfo = new FileInfo(fileName);
            if (!Directory.Exists(fileInfo.DirectoryName))
                Directory.CreateDirectory(fileInfo.DirectoryName);
        }

        public void SetDeleteSetting(string monitor, bool deleteFile, bool deleteSubdirectory)
        {
            if (_deleteFilesDic.Keys.Contains(monitor))
                _deleteFilesDic[monitor] = deleteFile;
            else
                _deleteFilesDic.Add(monitor, deleteFile);
            if (_deleteSubdirectoryDic.Keys.Contains(monitor))
                _deleteSubdirectoryDic[monitor] = deleteSubdirectory;
            else
                _deleteSubdirectoryDic.Add(monitor, deleteSubdirectory);
        }

        #endregion
    }
}
