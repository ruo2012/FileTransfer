﻿using FileTransfer.Models;
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
            var monitors = SimpleIoc.Default.GetInstance<MainViewModel>().MonitorCollection.ToList();
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
            PauseMonitor();
            List<string> keyList = _monitorDirectoryChanges.Keys.ToList();
            List<MonitorChanges> changes = new List<MonitorChanges>();
            foreach (string monitorDirectory in keyList)
            {
                if (!IOHelper.Instance.HasMonitorDirectory(monitorDirectory))
                    continue;
                //获取当前监控文件夹是否有订阅者
                List<string> subscribeIPs = SimpleIoc.Default.GetInstance<MainViewModel>().MonitorCollection.Where(m => !string.IsNullOrEmpty(m.SubscribeIP)).Where(m => m.MonitorDirectory == monitorDirectory).Select(m => m.SubscribeIP).ToList();
                //如果没有订阅者就继续循环（不用检测文件夹内的文件变化）
                if (subscribeIPs == null || subscribeIPs.Count <= 0) continue;
                //获取现有文件信息
                List<string> nowFiles = IOHelper.Instance.GetAllFiles(monitorDirectory);
                //获取原有文件信息
                List<string> oldFiles = _monitorDirectoryChanges[monitorDirectory];
                //相比之前文件信息集的增量
                List<string> incrementFiles = nowFiles.Except(oldFiles).ToList();
                //记录被其他线程占用的文件信息
                List<string> usedIncrementFiles = new List<string>();
                foreach (var increment in incrementFiles)
                {
                    if (TryAccessFile(increment)) continue;
                    usedIncrementFiles.Add(increment);
                }
                //更新：nowFiles记录当前未被占用的文件集，incrementFiles记录当前未被占用的增量集
                nowFiles = nowFiles.Except(usedIncrementFiles).ToList();
                incrementFiles = incrementFiles.Except(usedIncrementFiles).ToList();
                //记录现在监控文件夹内的信息
                _monitorDirectoryChanges[monitorDirectory] = nowFiles;
                //如果没有增量，则继续遍历
                if (incrementFiles == null || incrementFiles.Count <= 0)
                    continue;
                _logger.Info(string.Format("监控文件夹{0}内新增{1}个文件", monitorDirectory, incrementFiles.Count));
                changes.Add(new MonitorChanges() { MonitorDirectory = monitorDirectory, SubscribeIPs = subscribeIPs, FileChanges = incrementFiles });
            }
            RecoverMonitor();
            if (changes == null || changes.Count <= 0) return;
            if (NotifyMonitorChanges != null)
                NotifyMonitorChanges(changes);
        }

        private bool TryAccessFile(string file)
        {
            try
            {
                using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                { }
                return true;
            }
            catch (Exception )
            {
                return false;
            }
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
