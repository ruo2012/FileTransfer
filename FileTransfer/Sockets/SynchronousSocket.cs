﻿using FileTransfer.ViewModels;
using GalaSoft.MvvmLight.Ioc;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FileTransfer.Sockets
{
    class SynchronousSocket
    {
        #region 常量
        private const int LISTEN_BACKLOG = 100;
        private const int CONNECTED_WAITTIME = 50;

        #endregion

        #region 变量
        private static ILog _logger = LogManager.GetLogger(typeof(SynchronousSocket));
        private Socket _listenSocket;
        #endregion

        #region 事件
        public delegate void SocketConnectedEventHandler(Socket socket);
        public event SocketConnectedEventHandler SocketConnected;
        #endregion

        #region 构造函数
        public SynchronousSocket()
        { }
        #endregion

        #region 方法
        public void StartListening(IPAddress hostIP, int port)
        {
            try
            {
                _logger.Info(string.Format("开启本地端口{0}的侦听", port));
                _listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint ep = new IPEndPoint(hostIP, port);
                _listenSocket.Bind(ep);
                _listenSocket.Listen(LISTEN_BACKLOG);
                while (true)
                {
                    Socket connectedSocket = _listenSocket.Accept();
                    Thread.Sleep(CONNECTED_WAITTIME);
                    if (SocketConnected != null)
                        Task.Factory.StartNew(() => { SocketConnected(connectedSocket); });
                }
            }
            catch (SocketException se)
            {
                //ErrorCode:10004 表示操作被取消，即阻塞的监听被取消
                if (se.ErrorCode == 10004)
                    return;
                _logger.Fatal(string.Format("启动本地端口{0}侦听发生套接字异常！SocketException ErrorCode:{1}", port, se.ErrorCode));
                CloseSocket(_listenSocket);
                MessageBox.Show("启动本地侦听时发生套接字异常！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception e)
            {
                _logger.Error(string.Format("启动本地侦听时发生异常！异常：{0}", e.Message));
                CloseSocket(_listenSocket);
                MessageBox.Show("启动本地侦听时发生异常！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void StopListening()
        {
            try
            {
                CloseSocket(_listenSocket);
            }
            catch (SocketException se)
            {
                _logger.Fatal(string.Format("关闭本地监听发生套接字异常！SocketException ErrorCode:{0}", se.ErrorCode));
                MessageBox.Show("关闭本地侦听时发生套接字异常！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception e)
            {
                _logger.Error(string.Format("关闭本地监听发生异常！异常：{0}", e.Message));
                MessageBox.Show("关闭本地侦听时发生异常！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public Socket StartConnecting(IPEndPoint ep)
        {
            try
            {
                Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                client.Connect(ep);
                Thread.Sleep(CONNECTED_WAITTIME);
                if (client.Connected)
                    //if (SocketConnected != null)
                    //    SocketConnected(client);
                    return client;
                else
                    return null;
            }
            catch (SocketException se)
            {
                _logger.Error(string.Format("向远端{0}:{1}发起连接时发生套接字异常！SocketException ErrorCode:{1}", ep.Address, ep.Port, se.ErrorCode));
                return null;
            }
            catch (Exception e)
            {
                _logger.Error(string.Format("向远端{0}:{1}发起连接时发生异常！异常：{1}", ep.Address, ep.Port, e.Message));
                return null;
            }
        }

        private void CloseSocket(Socket socket)
        {
            if (_listenSocket == null) return;
            _listenSocket.Close();
            _listenSocket = null;
            _logger.Info(string.Format("关闭本地侦听Socket并释放所有关联资源"));
        }
        #endregion

    }
}
