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
using System.Xml;
using System.Xml.Serialization;

namespace FileTransfer.Configs
{
    public class ConfigHelper
    {
        #region 只读变量
        private static ILog _logger = LogManager.GetLogger(typeof(ConfigHelper));
        #endregion

        #region 变量
        private string _settingPath;
        #endregion

        #region 单例
        private static ConfigHelper _instance;

        public static ConfigHelper Instance
        {
            get { return _instance ?? (_instance = new ConfigHelper()); }
        }

        #endregion

        #region 属性
        private List<MonitorModel> _monitorSettings;
        public List<MonitorModel> MonitorSettings
        {
            get { return _monitorSettings ?? (_monitorSettings = new List<MonitorModel>()); }
        }
        private List<SubscribeModel> _subscribeSettings;
        public List<SubscribeModel> SubscribeSettings
        {
            get { return _subscribeSettings ?? (_subscribeSettings = new List<SubscribeModel>()); }
        }
        private int _listenPort;
        public int ListenPort
        {
            get { return _listenPort; }
        }

        private int _scanPerid;

        public int ScanPeriod
        {
            get { return _scanPerid; }
        }

        #endregion

        #region 构造函数
        public ConfigHelper()
        {
            _settingPath = Path.Combine(Environment.CurrentDirectory, "Configs", "settings.xml");
        }
        #endregion

        #region 方法
        private string ObjectToXmlString(object obj)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(obj.GetType());
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("", "");
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                settings.Encoding = Encoding.UTF8;
                MemoryStream ms = new MemoryStream();
                using (XmlWriter xmlWriter = XmlWriter.Create(ms, settings))
                {
                    serializer.Serialize(xmlWriter, obj, ns);
                }
                return Encoding.UTF8.GetString(ms.ToArray());
            }
            catch (Exception e)
            {
                _logger.Error(string.Format("配置项序列化时发生错误：{0}", e.Message), e);
                return string.Empty;
            }
        }

        private object XmlStringToObject(string xml)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(ConfigClass));

            }
            catch (Exception)
            {

                throw;
            }
            return null;
        }

        private void ExportXml(string filePath, object obj)
        {
            try
            {
                string xmlContent = ObjectToXmlString(obj);
                using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                {
                    using (StreamWriter sw = new StreamWriter(fs))
                    {
                        sw.Write(xmlContent);
                    }
                }
                _logger.Info(string.Format("配置项转换为Xml成功，存于{0}", filePath));
            }
            catch (Exception e)
            {
                _logger.Error(string.Format("配置项转换为Xml时出错：{0}", e.Message), e);
            }
        }

        private object ImportXml(string filePath)
        {
            try
            {
                object result = null;
                XmlSerializer serializer = new XmlSerializer(typeof(ConfigClass));
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (XmlReader xmlReader = XmlReader.Create(fs))
                    {
                        result = serializer.Deserialize(xmlReader);
                    }
                }
                return result;
            }
            catch (Exception e)
            {
                _logger.Error(string.Format("配置文件反序列化过程中出错：{0}", e.Message), e);
                return null;
            }
        }

        public void SaveSettings(List<MonitorModel> monitors, List<SubscribeModel> subscribes, int port, int scanPeriod)
        {
            _monitorSettings = monitors;
            _subscribeSettings = subscribes;
            _listenPort = port;
            _scanPerid = scanPeriod;
            var config = new ConfigClass(monitors, subscribes, port, scanPeriod);
            ExportXml(_settingPath, config);
        }

        public void Initial()
        {
            ConfigClass config = ImportXml(_settingPath) as ConfigClass;
            if (config == null)
            {
                _logger.Info("加载配置文件转换异常！采用默认配置。");
                _monitorSettings = new List<MonitorModel>();
                _subscribeSettings = new List<SubscribeModel>();
                _listenPort = 8888;
                _scanPerid = 5;
                return;
            }
            else
            {
                _monitorSettings = config.MonitorSettings;
                _subscribeSettings = config.SubscribeSettings;
                _listenPort = config.ListenPort;
                _scanPerid = config.ScanPeriod;
            }
        }

        #endregion
    }
}
