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

namespace Calo_OFE_Control_v._10._0._0
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

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

           /* rec_packet.Header0 = 'V';
            rec_packet.Header1 = 'G';
            rec_packet.Header2 = 'K';
            rec_packet.Header3 = 'H';
            rec_packet.Func           =20;      
            rec_packet.Data_Le        =20;
            rec_packet.Cmd            =20;
            rec_packet.ADCvalue       =20;
            rec_packet.MeMin          =20;
            rec_packet.MeMax          =20;
            rec_packet.getPWM_value   =20;
            rec_packet.DBG1           =20;
            rec_packet.DBG2           =20;
            rec_packet.DBG3           =20;
            rec_packet.StatusReg      =20;
            rec_packet.Current        =20;
            rec_packet.SizeArr        =20;
            rec_packet.DbgArr0        =20;
            rec_packet.DbgArr1        =20;
            rec_packet.DbgArr2        =20;
            rec_packet.DbgArr3        =20;
            rec_packet.DbgArr4        =20;
            rec_packet.DbgArr5        =20;
            rec_packet.DbgArr6        =20;
            rec_packet.DbgArr7        =20;
            rec_packet.DbgArr8        =20;
            rec_packet.DbgArr9 = 20;*/


        }

        CalControll_Settings set_packet = new CalControll_Settings();
        CalControll_RecData rec_packet = new CalControll_RecData();
        Grid grid1 = new Grid();

        TcpClient client = new TcpClient();
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Соединяемся с сервером
                client.Connect("192.168.111.80", 501); 
            }
            catch (SocketException ex)
            {
                MessageBox.Show("Exception: " + ex.ToString());
            }
            NetworkStream tcpStream = client.GetStream();

            byte[] packet = RawSerialize(set_packet);

            tcpStream.Write(packet, 0, packet.Length);
            int rawsize = Marshal.SizeOf(rec_packet);
            byte[] bytes = new byte[rawsize];
            int bytesRead = tcpStream.Read(bytes, 0, bytes.Length);

            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            CalControll_RecData uu = Marshal.PtrToStructure<CalControll_RecData>(handle.AddrOfPinnedObject());
            handle.Free();
            rec_packet = uu;

        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CalControll_Settings
        {
            public char Header0;
            public char Header1;
            public char Header2;
            public char Header3;
            public char[] Header;
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
        [StructLayout(LayoutKind.Sequential)]
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

        public static byte[] RawSerialize(object anything)
        {
            int rawsize = Marshal.SizeOf(anything);
            byte[] rawdata = new byte[rawsize];
            GCHandle handle = GCHandle.Alloc(rawdata, GCHandleType.Pinned);
            Marshal.StructureToPtr(anything, handle.AddrOfPinnedObject(), false);
            handle.Free();
            return rawdata;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
