﻿using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileTransfer.Models
{
    public class SendLogModel : ObservableObject
    {
        #region 属性
        private string _sendFileName;

        public string SendFileName
        {
            get { return _sendFileName; }
            set
            {
                _sendFileName = value;
                RaisePropertyChanged("SendFileName");
            }
        }

        private DateTime _sendFileTime;

        public DateTime SendFileTime
        {
            get { return _sendFileTime; }
            set
            {
                _sendFileTime = value;
                RaisePropertyChanged("SendFileTime");
            }
        }

        private string _subscribeIP;

        public string SubscribeIP
        {
            get { return _subscribeIP; }
            set
            {
                _subscribeIP = value;
                RaisePropertyChanged("SubscribeIP");
            }
        }

        private string _sendFileState;

        public string SendFileState
        {
            get { return _sendFileState; }
            set
            {
                _sendFileState = value;
                RaisePropertyChanged("SendFileState");
            }
        }
        
        #endregion
    }
}
