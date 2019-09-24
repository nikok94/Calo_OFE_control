using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Net.NetworkInformation;
using System.ComponentModel;

namespace Calo_OFE_Control_v._10._0._0
{ 
    public partial class MainWindow : Window
    {
        bool run;
        Thread tcpListenThread;
        const uint
            FD_CONNECT = 0x10,
            FD_READ = 0x01,
            WAIT_TIMEOUT = 0x102;
        [DllImport("Ws2_32.dll")]
        static extern int WSAEventSelect(IntPtr socket, IntPtr hEventObject, int lNetworkEvents);
        Queue<byte[]> queue;
        volatile int stop_Flag;
        ManualResetEvent data_Evt;
        public MainWindow()
        {
            InitializeComponent();
            var converter = new BrushConverter();
            var PWMButBackBrush = (Brush)converter.ConvertFromString("#FFF5E4E4");
            PWMButton.Foreground = PWMButBackBrush;
            //SetTimer();
            TextBoxOfTime.Content = "initcompleat";
            if (Properties.Settings.Default.SecondFormStatus)
            {
                MainGrid.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
                MainGrid.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);
                Button1.Content = "Roll";
            }
            else
            {
                Button1.Content = "Deploy";
                MainGrid.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
                MainGrid.RowDefinitions[1].Height = new GridLength(0, GridUnitType.Star);
            }

            if (Properties.Settings.Default.PWM_Button_sts)
            {
                PWMButton.Content = "PWM Fixed";
                PWMButton.Background = Brushes.Gray;
            }
            else
            {
                PWMButton.Content = "PWM Auto";
                PWMButton.Background = Brushes.Green;
            }

            set_packet.Func = 1;
            set_packet.Cmd = 1;
            set_packet.Data_Len = 0;
            set_packet.Header0 = 'V';
            set_packet.Header1 = 'G';
            set_packet.Header2 = 'K';
            set_packet.Header3 = 'H';
            set_packet.Npics = 1;
            set_packet.ADCDelay = 16;
            set_packet.AvLbit = 0;
            set_packet.dD = 26;
            set_packet.DL = 32;
            set_packet.AvNbit = 1;
            set_packet.ExpLav = 16;
            set_packet.setPWM_value = 500;
            set_packet.CalibrTime = 1;
            set_packet.Control = 0;
            set_packet.MenPos = 700;
            set_packet.KP = 750;
            set_packet.KI = 2;
            set_packet.KD = -350;
            set_packet.PIDfreq = 200;
            set_packet.DIFfreq = 20;
            set_packet.MenLow = 20;
            set_packet.MenHigh = 20;
            set_packet.MaxCCD = 20;
            set_packet.AveInt = 20;


            TCPSockedStatus();

            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 0, 0, 1);
            timer.IsEnabled = true;
            //timer.Tick += (o, e) => { TextBoxOfTime.Content = ("{0}:{1}:{2}.{3} <{4}>", timer.Interval.Hours, timer.Interval.Minutes, timer.Interval.Seconds, timer.Interval.Milliseconds, timer.Interval.Days); };
            timer.Tick += (o, e) => { TextBoxOfTime.Content = DateTime.Now.ToString(); };
            timer.Tick += new EventHandler(TCPSockedStatusCheck);
            timer.Start();
            queue = new Queue<byte[]>();

            var TCPCheckTimer = new System.Windows.Threading.DispatcherTimer();
            TCPCheckTimer.Interval = new TimeSpan(0, 0, 0, 1);
            TCPCheckTimer.IsEnabled = true;
            TCPCheckTimer.Tick += new EventHandler(TCPSockedStatusCheck);
            TCPCheckTimer.Start();

          /*  Socket s = new Socket(SocketType.Stream, ProtocolType.IP);
            s.ReceiveTimeout = 1;
            s.Connect(new IPEndPoint(IPAddress.Parse(address), port));
            Dispatcher.BeginInvoke((Action)(() => { StatusTextBox.Text = ("Connected"); }));

            byte[] send_send = RawSerialize(set_packet);
            byte[] res_buff = new byte[1024];

            ExecTCPCommand(s, send_send, res_buff);*/



            //    Thread tcpListenThread = new Thread(ControlTCPState);
            //    tcpListenThread.Start();
        }

        CalControll_Settings set_packet = new CalControll_Settings();
        CalControll_RecData rec_packet = new CalControll_RecData();
        Ping p = new Ping();
        // static TcpClient client = new TcpClient();

        string address = "192.168.111.80";
        int port = 501;

        protected override void OnClosing(CancelEventArgs e)
        {
            run = false;
            if (tcpListenThread != null)
            {
                if (tcpListenThread.IsAlive) tcpListenThread.Join();
            }
            base.OnClosing(e);
        }
        private void TCPSockedStatusCheck(object sender, EventArgs e)
        {
            TCPSockedStatus();
        }

        int ExecTCPCommand(Socket s, ManualResetEvent dat_evt, byte[] cmd_buff, byte[] res_buff)
        {
            int len = 0;
            try
            {
                dat_evt.Reset();
                s.Send(cmd_buff);
                System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
                if (dat_evt.WaitOne(100))
                {
                    if (s.Available == 0)
                    {
                        return 0;
                    }
                    if (s.Available > res_buff.Length) res_buff = new byte[s.Available];
                    len = s.Receive(res_buff, res_buff.Length, SocketFlags.None);

                }

                else
                {
                    sw.Stop();
                //    Logger.AppendLog("Timeout occured. dT = {0}", sw.Elapsed);
                    if (s.Available == 0)
                    {
                        return -1;
                    }
                    if (s.Available > res_buff.Length) res_buff = new byte[s.Available];
                    len = s.Receive(res_buff, res_buff.Length, SocketFlags.None);
                }

                if (len == 0)
                {
           //         Logger.AppendLog("0 length packet received!!!");
                }

            }
            catch (Exception ex)
            {
           //     if (log_ON) Logger.AppendLog("Exception occured during execution command: {0}", ex.Message);
                len = 0;
            }
            return len;
        }

        void TCPSockedStatus()
        {
            if (run != true)
            {
                PingReply rr = p.Send(address, 10);
                if (rr.Status == IPStatus.Success)
                {
                    Socket s = new Socket(SocketType.Stream, ProtocolType.IP);
                    s.ReceiveTimeout = 1;
                    s.Connect(new IPEndPoint(IPAddress.Parse(address), port));
                    Dispatcher.BeginInvoke((Action)(() => { StatusTextBox.Text = ("Connected"); }));
                    if (tcpListenThread == null)
                    {
                        tcpListenThread = new Thread(CommandLoop);
                        run = true;
                        tcpListenThread.Start(s);
                        data_Evt = new ManualResetEvent(false);
                        WSAEventSelect(s.Handle, data_Evt.SafeWaitHandle.DangerousGetHandle(), (int)FD_READ);
                    }
                    run = true;

                }
                else
                {
                    Dispatcher.BeginInvoke((Action)(() => { StatusTextBox.Text = ("Not connection"); }));
                }
            }
            else
            {
                PingReply rr = p.Send(address, 10);
                if (rr.Status == IPStatus.Success)
                {
                    Dispatcher.BeginInvoke((Action)(() => { StatusTextBox.Text = ("Connected"); }));
                }
                else
                {
                    run = false;
                    Dispatcher.BeginInvoke((Action)(() => { StatusTextBox.Text = ("Not connection"); }));
                }
            }
        }


        void CommandLoop(object ss)
        {
            Socket s = ss as Socket;
            if (s == null) return;
            byte[] cmd;
            tcp_head comm_cmd = new tcp_head();
            comm_cmd.Func = 1;
            comm_cmd.Cmd = 1;
            comm_cmd.Data_Len = 0;
            comm_cmd.Header0 = 'V';
            comm_cmd.Header1 = 'G';
            comm_cmd.Header2 = 'K';
            comm_cmd.Header3 = 'H';
            byte[] comm_cmd_buff = Serialize(comm_cmd);



            while (true)
            {
                if (!run) break;

                byte[] send_send = Serialize(set_packet);
                byte[] res_buff = new byte[1024];

                cmd = null;

                if (queue.Count > 0) { cmd = queue.Dequeue(); }
                if (cmd == null)
                {
                    if (stop_Flag != 0) break;
                    cmd = comm_cmd_buff;
                }


                int t = ExecTCPCommand(s, data_Evt, cmd, res_buff);
                CalControll_RecData data2 = Deserialize<CalControll_RecData>(res_buff);
                Dispatcher.BeginInvoke((Action)(() => { sl1Value.Value = data2.getPWM_value; }));
                


             /*   set_packet.Func = 2;
                set_packet.setPWM_value = 8000;
                res_buff = new byte[32];
                t = ExecTCPCommand(s, data_Evt, Serialize(set_packet), res_buff);
                tcp_head data3 = Deserialize<tcp_head>(res_buff);
                t = ExecTCPCommand(s, data_Evt, cmd, res_buff);
                CalControll_RecData data4 = Deserialize<CalControll_RecData>(res_buff);
                
                /*      byte[] packet = RawSerialize(set_packet);
                      s.Send(packet);
                      byte[] rPack = new byte[1024];
                      int len = s.Receive(rPack);*/

                Thread.Sleep(10);
            }

            if (s.Connected) s.Disconnect(false);
            s.Shutdown(SocketShutdown.Both);
            s.Close();
            s = null;
        }



        [StructLayout(LayoutKind.Sequential)]
        struct tcp_head
        {
            public char Header0;
            public char Header1;
            public char Header2;
            public char Header3;
            public short Func;
            public short Data_Len;
            public UInt16 Cmd;
        }

        private struct CalControll_Settings
        {
            public char Header0;
            public char Header1;
            public char Header2;
            public char Header3;
            public short Func;
            public short Data_Len;
            public UInt16 Cmd;
            public UInt16 Npics;
            public UInt16 ADCDelay;
            public UInt16 AvLbit;
            public UInt16 dD;
            public UInt16 DL;
            public UInt16 AvNbit;
            public UInt16 ExpLav;
            public UInt16 setPWM_value;
            public UInt16 CalibrTime;
            public UInt16 Control;
            public float MenPos;
            public float KP;
            public float KI;
            public float KD;
            public UInt16 PIDfreq;
            public UInt16 DIFfreq;
            public UInt16 MenLow;
            public UInt16 MenHigh;
            public float MaxCCD;
            public float AveInt;
        }
        private struct CalControll_RecData
        {
            public char Header0;
            public char Header1;
            public char Header2;
            public char Header3;
            public short Func;
            public short Data_Le;
            public UInt16 Cmd;
            public UInt16 ADCvalue;
            public float MeMin;
            public float MeMax;
            public UInt16 getPWM_value;
            public float DBG1;
            public float DBG2;
            public float DBG3;
            public UInt32 StatusReg;
            public UInt16 Current;
            public UInt32 SizeArr;
            public float DbgArr0;
            public float DbgArr1;
            public float DbgArr2;
            public float DbgArr3;
            public float DbgArr4;
            public float DbgArr5;
            public float DbgArr6;
            public float DbgArr7;
            public float DbgArr8;
            public float DbgArr9;
        }

      /*  public static byte[] RawSerialize(object anything)
        {
            int rawsize = Marshal.SizeOf(anything);
            byte[] rawdata = new byte[rawsize];
            GCHandle handle = GCHandle.Alloc(rawdata, GCHandleType.Pinned);
            Marshal.StructureToPtr(anything, handle.AddrOfPinnedObject(), false);
            handle.Free();
            return rawdata;
        }*/


        public static byte[] Serialize<T>(T s)
        where T : struct
        {
            var size = Marshal.SizeOf(typeof(T));
            var array = new byte[size];
            var ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(s, ptr, true);
            Marshal.Copy(ptr, array, 0, size);
            Marshal.FreeHGlobal(ptr);
            return array;
        }


        public static T Deserialize<T>(byte[] array)
            where T : struct
        {
            var size = Marshal.SizeOf(typeof(T));
            var ptr = Marshal.AllocHGlobal(size);
            Marshal.Copy(array, 0, ptr, size);
            var s = (T)Marshal.PtrToStructure(ptr, typeof(T));
            Marshal.FreeHGlobal(ptr);
            return s;
        }

        private void ClickButton1(object sender, RoutedEventArgs e)
        {
            if (Properties.Settings.Default.SecondFormStatus)
            {
                Properties.Settings.Default["SecondFormStatus"] = false;
                Properties.Settings.Default.Save();
                var s = MainGrid.RowDefinitions[1].ActualHeight;
                MainGrid.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
                MainGrid.RowDefinitions[1].Height = new GridLength(0, GridUnitType.Star);
                MainWin.Height = MainWin.Height - s;
                Button1.Content = "Deploy";
            }
            else
            {
                Properties.Settings.Default["SecondFormStatus"] = true;
                Properties.Settings.Default.Save();
                var s = MainGrid.RowDefinitions[0].ActualHeight;
                MainWin.Height = MainWin.Height + s;
                MainGrid.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
                MainGrid.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);
                Button1.Content = "Roll";
            }

        }

        private void MsWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                slValue.Value = slValue.Value + 1;
            }
            else
            {
                slValue.Value = slValue.Value - 1;
            }


        }

        private void Ms1Wheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                sl1Value.Value = sl1Value.Value + 1;
            }
            else
            {
                sl1Value.Value = sl1Value.Value - 1;
            }


        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
         
            if (Properties.Settings.Default.PWM_Button_sts)
            {
                Properties.Settings.Default["PWM_Button_sts"] = false;
                Properties.Settings.Default.Save();
                PWMButton.Content = "PWM Auto";
                PWMButton.Background = Brushes.Green;

            }
            else
            {
                Properties.Settings.Default["PWM_Button_sts"] = true;
                Properties.Settings.Default.Save();
                PWMButton.Content = "PWM Fixed";
                PWMButton.Background = Brushes.Gray;
            }

        }
    }
}
