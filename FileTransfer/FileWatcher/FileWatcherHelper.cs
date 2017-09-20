using FileTransfer.Models;
using FileTransfer.ViewModels;
using GalaSoft.MvvmLight.Ioc;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace FileTransfer.FileWatcher
{
    public class FileWatcherHelper
    {
        #region 变量
        private static ILog _logger = LogManager.GetLogger(typeof(FileWatcherHelper));
        //记录监控文件夹发生的文件信息变化
        private Dictionary<string, List<string>> _monitorDirectoryChanges;
        //定时器，用于定时监控
        private Timer _timer;
        #endregion

        #region 属性

        #endregion

        #region 单例
        private static FileWatcherHelper _instance;
        public static FileWatcherHelper Instance
        {
            get { return _instance ?? (_instance = new FileWatcherHelper()); }
        }
        #endregion

        #region 事件
        public delegate void NotifyMonitorChangesEventHandle(List<MonitorChanges> increments);
        public NotifyMonitorChangesEventHandle NotifyMonitorChanges;
        #endregion

        #region 方法
        public void StartMoniter()
        {
            //初始化监控文件夹的文件信息
            InitialMonitorChanges();
            //初始化定时器并启动
            _timer = new Timer();
            _timer.Interval = SimpleIoc.Default.GetInstance<MainViewModel>().ScanPeriod * 1000;
            _timer.Elapsed += _timer_Elapsed;
            _timer.Start();
            _logger.Info(string.Format("启动监控文件夹的扫描定时器，定时刷新时间间隔为{0}毫秒", _timer.Interval));
        }

        private void InitialMonitorChanges()
        {
            var monitors = SimpleIoc.Default.GetInstance<MainViewModel>().MonitorCollection.Where(m => !string.IsNullOrEmpty(m.SubscribeIP)).ToList();
            List<string> monitorDirectories = monitors.Select(m => m.MonitorDirectory).Distinct().ToList();
            _monitorDirectoryChanges = new Dictionary<string, List<string>>();
            foreach (var monitor in monitorDirectories)
            {
                //IOHelper中配置各监控文件夹删除文件和删除子文件夹的配置
                bool deleteFile = monitors.Where(m => m.MonitorDirectory == monitor).Any(m => m.DeleteFiles == true);
                bool deleteSubdirectory = monitors.Where(m => m.MonitorDirectory == monitor).Any(m => m.DeleteSubdirectory == true);
                IOHelper.Instance.SetDeleteSetting(monitor, deleteFile, deleteSubdirectory);
                //获取监控文件夹内的初始文件状态
                List<string> files = IOHelper.Instance.GetAllFiles(monitor);
                if (files == null || files.Count <= 0)
                    files = new List<string>();
                _monitorDirectoryChanges.Add(monitor, files);
            }
        }

        public void StopMoniter()
        {
            //关闭定时器
            if (_timer == null) return;
            _timer.Stop();
            _timer.Elapsed -= _timer_Elapsed;
            _timer = null;
            _logger.Info(string.Format("关闭监控文件夹的扫描定时器"));
        }

        //定时处理任务
        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            List<string> keyList = _monitorDirectoryChanges.Keys.ToList();
            List<MonitorChanges> changes = new List<MonitorChanges>();
            foreach (string monitorDirectory in keyList)
            {
                if (!IOHelper.Instance.HasMonitorDirectory(monitorDirectory))
                    continue;
                List<string> nowFiles = IOHelper.Instance.GetAllFiles(monitorDirectory);
                //相比之前文件信息集的增量
                List<string> incrementFiles = nowFiles.Except(_monitorDirectoryChanges[monitorDirectory]).ToList();
                //记录现在监控文件夹内的信息
                _monitorDirectoryChanges[monitorDirectory] = nowFiles;
                //如果没有增量，则继续遍历
                if (incrementFiles == null || incrementFiles.Count <= 0)
                    continue;
                _logger.Info(string.Format("监控文件夹{0}内新增{1}个文件", monitorDirectory, incrementFiles.Count));
                //通知注册事件的类处理增量信息
                List<string> subscribeIPs = SimpleIoc.Default.GetInstance<MainViewModel>().MonitorCollection.Where(m => !string.IsNullOrEmpty(m.SubscribeIP)).Where(m => m.MonitorDirectory == monitorDirectory).Select(m => m.SubscribeIP).ToList();
                changes.Add(new MonitorChanges() { MonitorDirectory = monitorDirectory, SubscribeIPs = subscribeIPs, FileChanges = incrementFiles });
            }
            if (changes == null || changes.Count <= 0) return;
            if (NotifyMonitorChanges != null)
                NotifyMonitorChanges(changes);
        }

        //public void AddNewMonitor(string monitorDirectory)
        //{
        //    _timer.Stop();
        //    if (!_monitorDirectoryChanges.Keys.Contains(monitorDirectory))
        //    {
        //        List<string> files = IOHelper.Instance.GetAllFiles(monitorDirectory);
        //        _monitorDirectoryChanges.Add(monitorDirectory, files);
        //        _logger.Info(string.Format("新增监控文件夹{0}(已被订阅)", monitorDirectory));
        //    }
        //    _timer.Start();
        //}

        public void PauseMonitor()
        {
            if (_timer == null) return;
            _timer.Stop();
        }

        public void RecoverMonitor()
        {
            if (_timer == null) return;
            _timer.Start();
        }

        #endregion
    }
}
