using BZ10.Common;
using cn.bmob.api;
using cn.bmob.io;
using cn.bmob.tools;
using MvCamCtrl.NET;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Media;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using ToupTek;
using Emgu.CV;
using Emgu.CV.Util;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System.Timers;
using System.Threading.Tasks;
using System.Speech.Synthesis;
using System.Globalization;

namespace BZ10
{
    public delegate void UpdataConfigShow();
    public partial class MainForm : Form
    {
        //运行主流程控制
        int inTimer1 = 0;
        System.Timers.Timer timer1 = new System.Timers.Timer();
        //各段时间是否到达控制
        int inTimer2 = 0;
        System.Timers.Timer timer2 = new System.Timers.Timer();
        //设备启动初始化判断是否有载玻片及设备复位做准备
        int inTimer3 = 0;
        System.Timers.Timer timer3 = new System.Timers.Timer();
        //判断是否可以开始新的流程
        int inTimer4 = 0;
        System.Timers.Timer timer4 = new System.Timers.Timer();
        //如果模式为定时运行，运行此定时器
        int inTimer5 = 0;
        System.Timers.Timer timer5 = new System.Timers.Timer();
        //腔体环境、外部环境读取
        int inTimer6 = 0;
        System.Timers.Timer timer6 = new System.Timers.Timer();
        //镜头点击到PC9限位故障检测
        int inTimer7 = 0;
        System.Timers.Timer timer7 = new System.Timers.Timer();
        //时间刷新、重启
        int inTimer8 = 0;
        System.Timers.Timer timer8 = new System.Timers.Timer();
        //弹窗倒计时
        int inTimer9 = 0;
        System.Timers.Timer timer9 = new System.Timers.Timer();
        //任务计划
        int inTimer10 = 0;
        System.Timers.Timer timer10 = new System.Timers.Timer();
        //连续上传
        int inTimer11 = 0;
        System.Timers.Timer timer11 = new System.Timers.Timer();
        //检测相机摄像头是否打开
        int inTimer12 = 0;
        System.Timers.Timer timer12 = new System.Timers.Timer();

        public static UpdataConfigShow updataConfigShow;
        List<string> ImgNames = new List<string>();
        //设备状态信息
        bool berror = false; //故障
        double lat = 0.00d;
        double lon = 0.00d;
        BrushConverter brushConverter = new BrushConverter();
        private ObservableCollection<ImageItem> collectionDispData = new ObservableCollection<ImageItem>();
        //列表分页
        string configfileName = "Config.ini";//当前配置文件
        string configfileName_Old = "Config_Old.ini";//旧版本配置文件
        private int currentPage = 1;//当前页
        private int pageCount = 1;//总页数
        private const int perPageCount = 2;//每页显示数量
        private string statusInfo = "正常";
        private int recordnum = 0;//记录条数
        private int runmode = 0; //0：正常工作模式 1：调试模式
        //历史数据集
        DataSet RecordDt = new DataSet();

        DevStatus devstatus = new DevStatus();
        //正常流程中步数标志
        public int bStep = -1;
        //镜头电机
        public int bStep_ = -1;
        //页面初始化标志
        public int nFinishStep = 0;
        List<byte> list = new List<byte>(); //待查询状态

        DateTime startTime;
        //镜头电机计时
        DateTime startTime_;
        /// <summary>
        /// 移动至PC9限位是否由于外因导致中途必须停止
        /// </summary>
        bool pc9IsStop = false;
        //DateTime endTime;
        PictureBox[] ctrls = null;

        //手动/自动标记
        bool autoFlag = false;//true 开机自动运行  false：定时运行 
        bool isSingleProcess = false;//是否正在执行单流程运行
        bool bstop = false; //停止标志位
        //显微镜变量
        private delegate void DelegateOnError();
        private delegate void DelegateOnImage(int[] roundrobin);
        private delegate void DelegateOnExposure();
        private delegate void DelegateOnTempTint();
        private Bitmap bmp1_ = null;
        private Bitmap bmp2_ = null;

        private Object locker_ = new Object();
        //private DelegateOnError everror_ = null;
        //private DelegateOnImage evimage_ = null;
        private DelegateOnExposure evexposure_ = null;
        private int roundrobin_ = 2;
        //private String StatusInfo = "正常";
        public TcpClient tcpclient = null;
        private bool _reconnection = true;
        GlobalParam global = new GlobalParam();
        private double wd;
        private double sd;
        private int locaiton = -1;

        //是否是为远程调试  是：true        不是false
        public static bool isLongRangeDebug = false;
        //设备是否开机   是：true      不是false
        public bool devIsStart = true;
        //启动timer1的时间
        public DateTime startTimer1Time = new DateTime();
        string cameraErrStr = "";//相机启动失败原因
        int countdown = 0;
        AutoSizeFormClass asc = new AutoSizeFormClass();
        //是否已创建定时任务
        bool isCreatedTimedTasks = false;
        //当天任务是否已经被执行  false 未被执行  true已被执行
        bool isItExecuted = false;
        #region 新版海康相机参数
        private MyCamera m_pMyCamera = null;//相机对象
        ComboBox cbDeviceList = new ComboBox();
        MyCamera.MV_CC_DEVICE_INFO_LIST m_pDeviceList;

        //bool m_bGrabbing;
        UInt32 m_nBufSizeForDriver = 3072 * 2048 * 3;
        byte[] m_pBufForDriver = new byte[3072 * 2048 * 3];
        UInt32 m_nBufSizeForSaveImage = 3072 * 2048 * 3 * 3 + 2048;
        byte[] m_pBufForSaveImage = new byte[3072 * 2048 * 3 * 3 + 2048];
        #endregion 新版海康相机参数

        #region 老版U口相机参数

        private Object oldLocker_ = new Object();

        private delegate void OldDelegateOnError();
        private delegate void OldDelegateOnImage(int[] roundrobin);
        private delegate void OldDelegateOnExposure();
        private OldDelegateOnError oldEverror = null;
        private OldDelegateOnImage oldEvimage = null;
        private OldDelegateOnExposure oldEvexposure = null;

        private ToupCam oldToupCam = null;
        private Bitmap oldBmp1 = null;
        private Bitmap oldBmp2 = null;

        private int odlRoundrobin_ = 2;

        #endregion 老版U口相机参数

        public MainForm()
        {
            InitializeComponent();
            timer1.Elapsed += new ElapsedEventHandler(timer1_Elapsed);//运行主流程控制
            timer1.Interval = 1000;
            timer2.Elapsed += new ElapsedEventHandler(timer2_Elapsed);//各段时间是否到达控制
            timer2.Interval = 1000;
            timer3.Elapsed += new ElapsedEventHandler(timer3_Elapsed);//设备启动初始化判断是否有载玻片及设备复位做准备
            timer3.Interval = 1000;
            timer4.Elapsed += new ElapsedEventHandler(timer4_Elapsed);//判断是否可以开始新的流程
            timer4.Interval = 1000;
            timer5.Elapsed += new ElapsedEventHandler(timer5_Elapsed);//如果模式为定时、时段运行，运行此定时器
            timer5.Interval = 1000;
            timer6.Elapsed += new ElapsedEventHandler(timer6_Elapsed);//腔体环境、外部环境读取
            timer6.Interval = 60000;
            timer7.Elapsed += new ElapsedEventHandler(timer7_Elapsed); //镜头电机到PC9限位故障检测
            timer7.Interval = 1000;
            timer8.Elapsed += new ElapsedEventHandler(timer8_Elapsed);//时间刷新、重启
            timer8.Interval = 1000;
            timer9.Elapsed += new ElapsedEventHandler(timer9_Elapsed); //弹窗倒计时
            timer9.Interval = 1000;
            timer10.Elapsed += new ElapsedEventHandler(timer10_Elapsed);//任务计划
            timer10.Interval = 1000;
            timer11.Elapsed += new ElapsedEventHandler(timer11_Elapsed);//图像上传
            timer11.Interval = 1000;
            timer12.Elapsed += new ElapsedEventHandler(timer12_Elapsed); //检测相机摄像头是否打开
            timer12.Interval = 15000;

#if DEBUG
            this.WindowState = FormWindowState.Normal;
#else
            this.WindowState = FormWindowState.Maximized;
#endif
            updataConfigShow = ReadConfig;
            Tools.ClearMemory();
            Tools.AutoStart(true);
        }

        /// <summary>
        /// 连续上传
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer11_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Interlocked.Exchange(ref inTimer11, 1) == 0)
            {
                if (Param.isContinuousUpload == "1")
                    SendCollectionData();
                Interlocked.Exchange(ref inTimer11, 0);
            }
        }
        /// <summary>
        ///  检测相机摄像头是否打开
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer12_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Interlocked.Exchange(ref inTimer12, 1) == 0)
            {
                StartOldNewCamera();
                Interlocked.Exchange(ref inTimer12, 0);
            }
        }

        private void timer10_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Interlocked.Exchange(ref inTimer10, 1) == 0)
            {
                //任务计划
                string currYear = DateTime.Now.Year.ToString();
                string currMonth = DateTime.Now.Month.ToString();
                string currDay = DateTime.Now.Day.ToString();
                if (currYear != Param.recordYear || currMonth != Param.recordMonth || currDay != Param.recordDay)
                {
                    DebOutPut.DebLog("日期发生变化，当前日期：" + currYear + "年" + currMonth + "月" + currDay + "日，记录日期：" + Param.recordYear + "年" + Param.recordMonth + "月" + Param.recordDay + "日");
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "日期发生变化，当前日期：" + currYear + "年" + currMonth + "月" + currDay + "日，记录日期：" + Param.recordYear + "年" + Param.recordMonth + "月" + Param.recordDay + "日");
                    //更新记录时间
                    Param.recordYear = currYear;
                    Param.recordMonth = currMonth;
                    Param.recordDay = currDay;
                    string sql = " update DateRecord Set RecordYear = '" + Param.recordYear + "',RecordMonth = '" + Param.recordMonth
                     + "',RecordDay = '" + Param.recordDay + "' where RecordNumber = '0'";
                    int x = DB.updateDatabase(sql);
                    if (x == -1)
                    {
                        DebOutPut.DebLog("记录日期修改失败！");
                        DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "记录日期修改失败！");
                    }
                    DebOutPut.DebLog("记录日期修改完成！");
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "记录日期修改完成！");
                    isCreatedTimedTasks = false;
                }
                //定时运行，且未创建任务的情况下
                if (!autoFlag && Param.RunFlag == "1" && !isCreatedTimedTasks)
                {
                    CreateTimedTask();
                    isCreatedTimedTasks = true;
                    isItExecuted = false;//该定时任务是否已被执行
                }
                Interlocked.Exchange(ref inTimer10, 0);
            }
        }

        /// <summary>
        /// 创建定时任务
        /// </summary>
        private void CreateTimedTask()
        {
            DebOutPut.DebLog(Param.recordYear + "年" + Param.recordMonth + "月" + Param.recordDay + "日， 开始创建定时运行任务！定时：" + Param.CollectHour + "时" + Param.CollectMinute + "分");
            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, Param.recordYear + "年" + Param.recordMonth + "月" + Param.recordDay + "日， 开始创建定时运行任务！定时：" + Param.CollectHour + "时" + Param.CollectMinute + "分");
            string sql = "select * from TimedTasks where currYear = '" + Param.recordYear + "' and currMonth='" + Param.recordMonth + "' and currDay='" + Param.recordDay + "' and runHour='" + Param.CollectHour + "' and runMinute='" + Param.CollectMinute + "'";
            DataTable dataTable = DB.QueryDatabase(sql).Tables[0];
            //未创建任务
            if (dataTable.Rows.Count == 0)
            {
                string creatTimer = DateTime.Now.ToString();
                sql = "insert into TimedTasks (currYear,currMonth,currDay,runHour,runMinute,runFlag,creationTime) values ('" + Param.recordYear + "','" + Param.recordMonth + "','" + Param.recordDay + "','" + Param.CollectHour + "','" + Param.CollectMinute + "','0','" + creatTimer + "')";
                DB.updateDatabase(sql);
                DebOutPut.DebLog("日期：" + Param.recordYear + "年" + Param.recordMonth + "月" + Param.recordDay + "日，创建任务成功！定时：" + Param.CollectHour + "时" + Param.CollectMinute + "分");
                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "日期：" + Param.recordYear + "年" + Param.recordMonth + "月" + Param.recordDay + "日，创建任务成功！定时：" + Param.CollectHour + "时" + Param.CollectMinute + "分");
            }
            else//已创建任务
            {
                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "日期：" + Param.recordYear + "年" + Param.recordMonth + "月" + Param.recordDay + "日，该任务以创建！定时：" + Param.CollectHour + "时" + Param.CollectMinute + "分");
                DebOutPut.DebLog("日期：" + Param.recordYear + "年" + Param.recordMonth + "月" + Param.recordDay + "日，该任务以创建！定时：" + Param.CollectHour + "时" + Param.CollectMinute + "分");
            }
        }

        private void timer8_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Interlocked.Exchange(ref inTimer8, 1) == 0)
            {
                //自动重启
                if (Param.isWinRestart == "1")
                {
                    string date = DateTime.Parse("00:00:00").ToString("HH:mm:ss", System.Globalization.DateTimeFormatInfo.InvariantInfo);
                    string date1 = DateTime.Now.ToString("HH:mm:ss", System.Globalization.DateTimeFormatInfo.InvariantInfo);
                    if (date == date1)
                        Tools.WinRestart();
                }
                //时间刷新
                this.LabTimeShow.Text = DateTime.Now.ToString(Param.dataType, System.Globalization.DateTimeFormatInfo.InvariantInfo);
                Interlocked.Exchange(ref inTimer8, 0);
            }
        }


        #region 计时器终止
        private void Timer1Stop()
        {
            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "Timer1_Stop");
            timer1.Stop();
            Interlocked.Exchange(ref inTimer1, 0);
        }
        private void Timer2Stop()
        {
            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "Timer2_Stop");
            timer2.Stop();
            Interlocked.Exchange(ref inTimer2, 0);
        }
        private void Timer3Stop()
        {
            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "Timer3_Stop");
            timer3.Stop();
            Interlocked.Exchange(ref inTimer3, 0);
        }
        private void Timer4Stop()
        {
            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "Timer4_Stop");
            timer4.Stop();
            Interlocked.Exchange(ref inTimer4, 0);
        }
        private void Timer5Stop()
        {
            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "Timer5_Stop");
            timer5.Stop();
            Interlocked.Exchange(ref inTimer5, 0);
        }
        private void Timer6Stop()
        {
            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "Timer6_Stop");
            timer6.Stop();
            Interlocked.Exchange(ref inTimer6, 0);
        }
        private void Timer7Stop()
        {
            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "Timer7_Stop");
            timer7.Stop();
            Interlocked.Exchange(ref inTimer7, 0);
        }

        private void Timer11Stop()
        {
            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "Timer11_Stop");
            timer11.Stop();
            Interlocked.Exchange(ref inTimer11, 0);
        }
        private void Timer12Stop()
        {
            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "Timer12_Stop");
            timer12.Stop();
            Interlocked.Exchange(ref inTimer12, 0);
        }
        #endregion

        private void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false;
                label2.ForeColor = System.Drawing.Color.Green;
                asc.controllInitializeSize(this);
                this.LabVersion.Text = "V_" + System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString();
                this.label61.Text = "系统当前登录用户: " + Tools.GetCurrentUser();
                //时间矫正
                TimeManager.SetSysTime();
                timer8.Start();
                //自动更新
                Thread updataSetup = new Thread(UpdataSetup_T);
                updataSetup.IsBackground = true;
                updataSetup.Start();
                if (DebOutPut.isDebView == 1)
                {
                    this.label27.Text = "测试版";
                    this.label27.ForeColor = System.Drawing.Color.Yellow;
                }
                else if (DebOutPut.isDebView == 0)
                {
                    this.label27.Text = "发布版";
                    this.label27.ForeColor = System.Drawing.Color.White;
                }
                this.listView1.View = View.LargeIcon;
                this.listView1.LargeImageList = this.imageList1;

                ctrls = new PictureBox[13] { pictureBox6, pictureBox7, pictureBox8, pictureBox9, pictureBox10, pictureBox11, pictureBox12, pictureBox13, pictureBox14, pictureBox15, pictureBox16, pictureBox17, pictureBox18 };
                hideLocations();
                if (File.Exists(Application.StartupPath + "\\" + configfileName_Old))
                {
                    //加载旧配置文件
                    Param.Init_Param(configfileName_Old);
                    //根据旧配置参数设置新配置参数
                    SetParamTab0(configfileName);
                    SetTimeDuan(configfileName);
                    SetConfig(configfileName);
                    File.Delete(Application.StartupPath + "\\" + configfileName_Old);
                }
                //加载配置文件
                Param.Init_Param(configfileName);

                if (Param.DripDevice == "0")
                {
                    label78.Text = "转";
                    buttonX18.Visible = false;
                    buttonX19.Visible = false;
                }
                else if (Param.DripDevice == "1")
                {
                    label78.Text = "步";
                    buttonX18.Visible = true;
                    buttonX19.Visible = true;
                }
                if (Param.version == "2")
                {
                    cb_Group1.Visible = true;
                    cb_Group2.Visible = true;
                }

                RefeshWindowLabel();
                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "设备被启动！");
                //  this.listView1.Items = collectionDispData;
                //初始化网络连接
                Thread myThread = new Thread(new ThreadStart(NetServerInit));
                myThread.IsBackground = true;
                myThread.Start();
                //RunFlag 0:自动运行 1:定时运行  2:分时运行 
                if (Param.RunFlag == "0")
                    autoFlag = true;
                else if (Param.RunFlag == "1" || Param.RunFlag == "2")
                    autoFlag = false;
                //初始化数据库
                if (DB.DBInit())
                {
                    ReadRecordDate();
                    timer10.Start();
                    timer12.Start();
                    if (Param.isContinuousUpload == "1")
                    {
                        this.timer11.Interval = double.Parse(Param.SearchInterval) * 60000;
                        this.timer11.Start();
                    }

                    if (SerialInit())
                    {
                        Cmd.InitComm(serialPort1);
                        //timer10.Start();
                        Thread thread = new Thread(DevRun_T);
                        thread.IsBackground = true;
                        thread.Start();
                        timer6.Start();
                    }
                }
                else
                {
                    statusInfo = "数据库打开失败,请处理！";
                    label18.Text = "数据库打开失败,请处理！";
                    DebOutPut.DebLog("数据库打开失败");
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "数据库打开失败");
                    label18.ForeColor = System.Drawing.Color.Red;
                }
                this.Show();
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }

        /// <summary>
        /// 读取记录日期
        /// </summary>
        private void ReadRecordDate()
        {
            string sql = "select * from DateRecord";
            DataTable dataTable = DB.QueryDatabase(sql).Tables[0];
            if (dataTable.Rows.Count > 0)
            {
                Param.recordYear = dataTable.Rows[0]["RecordYear"].ToString();
                Param.recordMonth = dataTable.Rows[0]["RecordMonth"].ToString();
                Param.recordDay = dataTable.Rows[0]["RecordDay"].ToString();
            }
            else if (dataTable.Rows.Count == 0)
            {
                DebOutPut.DebLog("记录日期初始化！");
                Param.recordYear = DateTime.Now.Year.ToString();
                Param.recordMonth = DateTime.Now.Month.ToString();
                Param.recordDay = DateTime.Now.Day.ToString();
                sql = "insert into DateRecord (RecordYear,RecordMonth,RecordDay,RecordNumber) values ('" + Param.recordYear + "','" + Param.recordMonth + "','" + Param.recordDay + "','0')";
                int x = DB.updateDatabase(sql);
                if (x == -1)
                {
                    DebOutPut.DebLog("记录日期修改失败！");
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "记录日期修改失败！");
                }
            }
        }

        bool isChangeSize = true;
        private void MainForm_SizeChanged(object sender, EventArgs e)
        {
            DebOutPut.DebLog("是否执行SizeChanged----------->" + isChangeSize);
            if (isChangeSize)
            {
                asc.controlAutoSize(this);
            }
        }
        private void MainForm_Shown(object sender, EventArgs e)
        {
            isChangeSize = false;
        }
        /// <summary>
        /// 设备运行线程函数
        /// </summary>
        private void DevRun_T()
        {
            Thread.Sleep(30000);
            if (cb_switch.Checked)
            {
                DebOutPut.DebLog("调试模式");
                return;
            }
            if (button12.Text == "自动对焦中....")
            {
                DebOutPut.DebLog("自动对焦");
                return;
            }
            if (autoFlag) //开机自动运行
                label26.Text = "当前工作模式:" + "自动运行";
            else if (!autoFlag && Param.RunFlag == "2")
                label26.Text = "当前工作模式:" + "时段运行";
            else if (!autoFlag && Param.RunFlag == "1")
                label26.Text = "当前工作模式:" + "定时运行";
            InitDev(); //判断设备是否就绪，可以开始新的流程了。
            if (!timer3.Enabled)
            {
                if (autoFlag) //开机自动运行
                    timer4.Start();
                else if (!autoFlag)
                {
                    DebOutPut.DebLog("非自动运行_设备初始化");
                    DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, "非自动运行_设备初始化");
                    Cmd.CommunicateDp(0x10, 0);
                    DebOutPut.DebLog("非自动运行_轴一找原点");
                    Cmd.CommunicateDp(0x20, 0);
                    DebOutPut.DebLog("非自动运行_轴二找原点");
                    Cmd.CommunicateDp(0x30, 0);
                    DebOutPut.DebLog("非自动运行_轴三找原点");
                    Cmd.CommunicateDp(0x40, 0);
                    DebOutPut.DebLog("非自动运行_轴四找原点");
                    Cmd.CommunicateDp(0x93, 0);
                    DebOutPut.DebLog("非自动运行_关闭风机和补光");
                    list.Clear();
                    list.Add(0);
                    list.Add(5);
                    list.Add(7);
                    list.Add(9);
                    while (!isReady())
                        Thread.Sleep(50);
                    setLocation(0);
                    timer5.Start();
                }
            }
        }


        /// <summary>
        /// 启动新旧相机
        /// </summary>
        private bool StartOldNewCamera()
        {
            bool bIsOpen = true;
            cameraErrStr = "";
            if (Param.cameraVersion == "1")
            {
                if (oldToupCam == null)
                {
                    //启动老版相机
                    bIsOpen = StartOldCamera();
                }
            }
            else if (Param.cameraVersion == "2")
            {
                if (m_pMyCamera == null || !m_pMyCamera.MV_CC_IsDeviceConnected_NET())
                {
                    //关闭新相机
                    CameraClose();
                    //启动新版相机
                    bIsOpen = StartNewCamera();
                }
            }
            return bIsOpen;
        }


        /// <summary>
        /// 启动老版相机
        /// </summary>
        private bool StartOldCamera()
        {
            try
            {
                int index = 0;
                while (index < 3)
                {
                    Start_OldGrabImage();
                    if (oldToupCam == null)
                    {
                        index++;
                        DebOutPut.DebLog("第 " + (index + 1) + " 次启动相机失败！");
                        Thread.Sleep(10000);
                    }
                    else
                    {
                        DebOutPut.DebLog("启动相机成功！");
                        statusInfo = "正常";
                        if (label18.Text.Contains("相机"))
                        {
                            label18.Text = "无数据";
                            label18.ForeColor = System.Drawing.Color.FromArgb(0, 0, 0);
                        }
                        break;
                    }
                }

                if (index == 3)
                {
                    if (cameraErrStr == "")
                    {
                        cameraErrStr = "启动相机失败";
                    }
                    statusInfo = cameraErrStr;
                    label18.Text = cameraErrStr + "，请处理！";
                    label18.ForeColor = System.Drawing.Color.Red;
                    return false;
                }
            }
            catch (Exception ex)
            {
                statusInfo = "相机启动异常";
                label18.Text = "相机启动异常，请处理！";
                label18.ForeColor = System.Drawing.Color.Red;
                DebOutPut.DebLog("老相机启动异常：" + ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, "老相机启动异常：" + ex.ToString());
                OldOnEventError();
                return false;
            }
            return true;
        }

        /// <summary>
        /// 启动新版相机
        /// </summary>
        private bool StartNewCamera()
        {
            try
            {
                int index = 0;
                while (index < 3)
                {
                    Start_GrabImage();
                    if (m_pMyCamera == null)
                    {
                        index++;
                        DebOutPut.DebLog("第 " + (index + 1) + " 次启动相机失败！");
                        Thread.Sleep(10000);
                    }
                    else
                    {
                        DebOutPut.DebLog("启动相机成功！");
                        statusInfo = "正常";
                        if (label18.Text.Contains("相机"))
                        {
                            label18.Text = "无数据";
                            label18.ForeColor = System.Drawing.Color.FromArgb(0, 0, 0);
                        }
                        break;
                    }
                }

                if (index == 3)
                {
                    if (cameraErrStr == "")
                    {
                        cameraErrStr = "启动相机失败";
                    }
                    statusInfo = cameraErrStr;
                    label18.Text = cameraErrStr + "，请处理！";
                    label18.ForeColor = System.Drawing.Color.Red;
                    return false;
                }
            }
            catch (Exception ex)
            {
                statusInfo = "相机启动异常";
                label18.Text = "相机启动异常，请处理！";
                label18.ForeColor = System.Drawing.Color.Red;
                DebOutPut.DebLog("新相机启动异常：" + ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, "新相机启动异常：" + ex.ToString());
                CameraClose();
                return false;
            }
            return true;
        }

        /// <summary>
        /// 自动更新
        /// </summary>
        private void UpdataSetup_T()
        {
            try
            {
                //程序刚开机时自动检测一次
                //VersionCheck();
                string date = DateTime.Parse("05:00:00").ToString("HH:mm:ss", System.Globalization.DateTimeFormatInfo.InvariantInfo);
                string date1 = DateTime.Parse("22:00:00").ToString("HH:mm:ss", System.Globalization.DateTimeFormatInfo.InvariantInfo);
                string date2 = DateTime.Parse("12:00:00").ToString("HH:mm:ss", System.Globalization.DateTimeFormatInfo.InvariantInfo);
                string date4 = DateTime.Parse("07:00:00").ToString("HH:mm:ss", System.Globalization.DateTimeFormatInfo.InvariantInfo);
                while (true)
                {
                    string currTime = DateTime.Now.ToString("HH:mm:ss", System.Globalization.DateTimeFormatInfo.InvariantInfo);
                    if (date == currTime || date1 == currTime || date2 == currTime)
                    {
                        if (Param.isContinuousUpload == "0")
                        {
                            DebOutPut.DebLog("数据查漏");
                            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "数据查漏");
                            //数据查漏
                            SendCollectionData();
                            Thread.Sleep(2000);
                        }
                        if (date4 == currTime)
                        {
                            DebOutPut.DebLog("版本检测");
                            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "版本检测");
                            //版本检测
                            VersionCheck();
                        }
                    }
                    Thread.Sleep(1000);
                }

            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }


        private void label4_Click(object sender, EventArgs e)
        {

        }
        public void NetServerInit()
        {
            try
            {
                DebOutPut.DebLog("首次连接服务器！");
                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "首次连接服务器！");
                global.devid = Param.DeviceID;
                global.host = Param.UploadIP;
                global.port = Convert.ToInt32(Param.UploadPort);
                tcpclient = new TcpClient(this, global);
                tcpclient.connectToSever();//连接服务器
                if (tcpclient.clientSocket.Connected)
                {
                    tcpclient.sendLocation(lat, lon);
                }
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }


        //本次上传图像是否成功
        bool isUpladCom = false;
        /// <summary>
        /// 锁
        /// </summary>
        static readonly object SendDataLock = new object();
        /// <summary>
        /// 发送采集数据
        /// </summary>
        private void SendCollectionData()
        {
            lock (SendDataLock)
            {
                try
                {
                    if (tcpclient != null && tcpclient.clientSocket != null && tcpclient.clientSocket.Connected && TcpClient.newDateTime.AddMinutes(5) > DateTime.Now)
                    {
                        DebOutPut.DebLog("检测是否有未发送的照片");
                        string sql = "select * from Record where Flag='0'";
                        DataTable UploadTable = DB.QueryDatabase(sql).Tables[0];
                        string path = "";
                        DebOutPut.DebLog("未传共  " + UploadTable.Rows.Count + "  个");
                        for (int i = 0; i < UploadTable.Rows.Count; i++)
                        {
                            if (tcpclient != null && tcpclient.clientSocket != null && tcpclient.clientSocket.Connected)
                            {
                                string collectTime = UploadTable.Rows[i]["CollectTime"].ToString();
                                DateTime dt = Convert.ToDateTime(collectTime);
                                path = Param.BasePath + "\\GrabImg\\" + dt.ToString("yyyyMMddHHmmss", System.Globalization.DateTimeFormatInfo.InvariantInfo) + ".bmp";
                                string imageName = dt.ToString("yyyyMMddHHmmss", System.Globalization.DateTimeFormatInfo.InvariantInfo) + ".bmp";
                                if (!File.Exists(path))
                                {
                                    DebOutPut.DebLog("文件不存在：" + path);
                                    continue;
                                }
                                DebOutPut.DebLog("当前发送第  " + (i + 1) + "  个，路径为:  " + path);
                                bool isSuccess = tcpclient.SendPicMsg(dt.ToString(Param.dataType, System.Globalization.DateTimeFormatInfo.InvariantInfo), path);
                                if (isSuccess)
                                {
                                    DateTime startTime = DateTime.Now;
                                    bool isUpladResult = true;//本张图像服务器回应结果
                                    while (!isUpladCom)
                                    {
                                        //这条采集数据发送5分钟之后仍然没有收到回应，就停止等待，终止本次发送
                                        if (startTime.AddMinutes(5) < DateTime.Now)
                                        {
                                            isUpladResult = false;//代表本张图像上传失败
                                            break;
                                        }
                                        Thread.Sleep(1000);
                                    }
                                    if (!isUpladResult)
                                    {
                                        DebOutPut.DebLog("图像 " + imageName + " 未收到回应，本次发送将被终止，图像路径为:  " + path);
                                        DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "图像 " + imageName + " 未收到回应，本次发送将被终止，图像路径为:  " + path);
                                        break;
                                    }
                                    else if (isUpladResult)
                                    {
                                        DebOutPut.DebLog("图像 " + imageName + " 已收到回应，图像路径为:  " + path);
                                        isUpladCom = false;
                                    }
                                }
                                else
                                {
                                    DebOutPut.DebLog("发现无法上传图像，路径为:  " + path);
                                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "发现无法上传图像，路径为:  " + path);
                                    collectTime = "update Record Set Flag = '2' where CollectTime='" + collectTime + "'";
                                    int x = DB.updateDatabase(collectTime);
                                    if (x != -1)
                                    {
                                        DebOutPut.DebLog("发现无法上传图像，该图像已被标记，路径为:  " + path);
                                        DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "发现无法上传图像，该图像已被标记，路径为:  " + path);
                                    }

                                }
                            }
                        }
                        UploadTable.Dispose();
                    }
                    else
                    {
                        DebOutPut.DebLog("数据发送失败,发送数据条件不满足！");
                        DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "数据发送失败,发送数据条件不满足！");
                    }
                }
                catch (Exception ex)
                {
                    DebOutPut.DebLog(ex.ToString());
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, ex.ToString());
                }
            }
        }

        private void doLiucheng()
        {
            initDev();
        }
        /*
         * 初始化设备：将所有轴回归原点
         */
        private void initDev()
        {
            try
            {
                DebOutPut.DebLog("设备初始化");
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, "设备初始化");
                //1.设备初始化，全部归原点。
                Cmd.CommunicateDp(0x10, 0);//轴一找原点
                DebOutPut.DebLog("轴一找原点");
                Cmd.CommunicateDp(0x20, 0);//轴二找原点
                DebOutPut.DebLog("轴二找原点");
                Cmd.CommunicateDp(0x30, 0);//轴三找原点
                DebOutPut.DebLog("轴三找原点");
                Cmd.CommunicateDp(0x40, 0);//轴四找原点
                DebOutPut.DebLog("轴四找原点");
                Cmd.CommunicateDp(0x93, 0);//关闭风机和补光
                DebOutPut.DebLog("关闭风机和补光");
                bStep = 0;//
                list.Clear();
                list.Add(0);
                list.Add(5);
                list.Add(7);
                list.Add(9);
                startTimer1Time = DateTime.Now;
                timer1.Start();
                /*
                 * 开始计时此初始化时间
                 */
                startTime = DateTime.Now;
                timer2.Start();
                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "Timer2_Start");
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }
        /*
        * 推片准备：轴3 到接片位置 轴4到待推片位置
        */
        private void inituipian()
        {
            try
            {

                //2. 推片准备：轴3 到x9位置 轴4到x11位置/如果是70mm长的轴四电机则轴四不动
                Cmd.CommunicateDp(0x33, 2);
                if (Param.recoveryDevice == "0")
                    Cmd.CommunicateDp(0x43, 2);
                else if (Param.recoveryDevice == "1")
                    Cmd.CommunicateDp(0x43, 1);
                bStep = 1;
                list.Clear();
                list.Add(8);
                if (Param.recoveryDevice == "0")
                    list.Add(10);
                else if (Param.recoveryDevice == "1")
                    list.Add(9);
                //Timer2Stop();
                startTimer1Time = DateTime.Now;
                timer1.Start();

                /*
                 * 开始计时此准备时间
                 */
                startTime = DateTime.Now;
                timer2.Start();
                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "Timer2_Start");
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }
        /*
         *   //3. 轴2执行推片
         */
        private void tuipian()
        {
            try
            {

                //3. 轴2执行推片执行推片后复位
                Cmd.CommunicateDp(0x23, 2);//轴二推片
                //Timer2Stop();
                bStep = 2;
                list.Clear();
                list.Add(6);
                list.Add(13);
                startTimer1Time = DateTime.Now;
                timer1.Start();
                /*
                 * 开始计时此推片时间
                 */
                startTime = DateTime.Now;
                timer2.Start();
                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "Timer2_Start");
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }
        /*推片后复位*/
        private void tuipianreset()
        {
            try
            {

                Cmd.CommunicateDp(0x20, 0);//轴二复位
                DebOutPut.DebLog("玻片矫正");
                Cmd.CommunicateDp(0x31, int.Parse(Param.slideCorrection));
                if (!isLongRangeDebug)
                {
                    //Timer2Stop();
                    bStep = 3;
                    list.Clear();
                    list.Add(5);
                    startTimer1Time = DateTime.Now;
                    timer1.Start();
                }
                /*
                 * 开始计时此复位时间
                 */
                startTime = DateTime.Now;
                timer2.Start();
                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "Timer2_Start");
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }

        }
        private void tuifanshilin()
        {
            try
            {

                //4.推片到粘附液位置
                Cmd.CommunicateDp(0x13, 2);
                //Timer2Stop();
                bStep = 4;
                list.Clear();
                list.Add(1);
                startTimer1Time = DateTime.Now;
                timer1.Start();

                /*
                 * 开始计时到粘附液位置时间
                 */
                startTime = DateTime.Now;
                timer2.Start();
                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "Timer2_Start");
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }



        }

        /// <summary>
        /// 滴加粘附液
        /// </summary>
        private void jiafanshilin()
        {
            try
            {
                bStep = 5;
                countdown = 20;
                if (Param.DripDevice == "0")
                    Cmd.CommunicateDp(0x51, Convert.ToInt16(Param.fanshilin));
                else if (Param.DripDevice == "1")
                    PushingFluidMove(true, Param.fanshilin);
                // Timer2Stop();
                startTime = DateTime.Now;
                timer2.Start();
                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "Timer2_Start");
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }
        private void tuipianFengshan()
        {
            try
            {

                Cmd.CommunicateDp(0x13, 3);

                bStep = 6;
                list.Clear();
                list.Add(2);
                startTimer1Time = DateTime.Now;
                timer1.Start();

                /*
                 * 开始计时到风机位置时间
                 */
                startTime = DateTime.Now;
                timer2.Start();
                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "Timer2_Start");

            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }
        private void openFengji()
        {
            try
            {
                //7.开风机
                bStep = 7;
                countdown = Convert.ToInt16(Param.FanMinutes) * 60;
                //恒定
                if (Param.FanMode == "0")
                {
                    DebOutPut.DebLog("采集模式：恒定");
                    DebOutPut.DebLog("采集强度：" + Param.FanStrength + " 转");
                    Cmd.CommunicateDp(0x91, Convert.ToInt16(Param.FanStrength));
                }
                //双值
                else if (Param.FanMode == "1")
                {
                    DebOutPut.DebLog("采集模式：双值");
                    DebOutPut.DebLog("采集最大强度：" + Param.FanStrengthMax + " 转");
                    DebOutPut.DebLog("采集最小强度：" + Param.FanStrengthMin + " 转");
                    Cmd.CommunicateDp(0x91, Convert.ToInt16(Param.FanStrengthMax));
                }
                //Timer2Stop();
                startTime = DateTime.Now;
                timer2.Start();
                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "Timer2_Start");
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }

        /// <summary>
        /// 关闭风机
        /// </summary>
        private void closeFengji()
        {
            try
            {
                //8.关风机
                Cmd.CommunicateDp(0x93, 0);//初始化
                if (!isLongRangeDebug)
                {
                    bStep = 8;
                    list.Clear();
                    list.Add(2);
                    startTimer1Time = DateTime.Now;
                    timer1.Start();
                }
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }

        }


        private void tuiPeiyangye()
        {
            try
            {
                //9.推片值培养液位置

                Cmd.CommunicateDp(0x13, 4);
                bStep = 9;
                list.Clear();
                list.Add(3);
                startTimer1Time = DateTime.Now;
                timer1.Start();
                /*
                 * 开始计时到培养液位置时间
                 */
                startTime = DateTime.Now;
                timer2.Start();
                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "Timer2_Start");
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }

        }
        private void peiyang()
        {
            try
            {
                bStep = 10;
                countdown = Convert.ToInt16(Param.peiyangtime) * 60 + 20;
                //10.滴加培养液
                Cmd.CommunicateDp(0x61, Convert.ToInt16(Param.peiyangye));
                //Timer2Stop();
                startTime = DateTime.Now;
                timer2.Start();
                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "Timer2_Start");
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }

        }
        private void tuipaizhao()
        {
            try
            {

                //推片到拍照位置
                Cmd.CommunicateDp(0x13, 5);
                //打开补光灯
                Cmd.CommunicateDp(0x92, 800);

                bStep = 11;
                list.Clear();
                list.Add(4);
                startTimer1Time = DateTime.Now;
                timer1.Start();
                /*
                 * 开始计时此推片到拍照位置时间
                 */
                startTime = DateTime.Now;
                timer2.Start();
                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "Timer2_Start");
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }

        private void huishoupian()
        {
            try
            {
                Thread.Sleep(5000);
                //2. 推片准备：轴3 到原点位置 轴4到原点位置/推完片位置
                Cmd.CommunicateDp(0x33, 1);
                DebOutPut.DebLog("执行0x33完毕");
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, "执行0x33完毕");
                Thread.Sleep(15000);
                if (Param.recoveryDevice == "0")
                    Cmd.CommunicateDp(0x43, 1);
                else if (Param.recoveryDevice == "1")
                    Cmd.CommunicateDp(0x43, 2);
                DebOutPut.DebLog("执行0x43完毕");
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, "执行0x43完毕");
                Thread.Sleep(20000);
                //履带电机运动
                DebOutPut.DebLog("履带电机运动");
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, "履带电机运动");
                MoveTrack();
                DebOutPut.DebLog("履带电机运动完毕");
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, "履带电机运动完毕");
                bStep = 12;
                DebOutPut.DebLog("bStep赋值：" + bStep);
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, "bStep赋值：" + bStep);
                list.Clear();
                list.Add(7);
                if (Param.recoveryDevice == "0")
                    list.Add(9);
                else if (Param.recoveryDevice == "1")
                    list.Add(10);
                startTimer1Time = DateTime.Now;
                timer1.Start();
                /*
                * 开始计时此回收片时间
                */
                startTime = DateTime.Now;
                timer2.Start();
                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "Timer2_Start");
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }

        private void finish()
        {
            try
            {

                //12.设备初始化，全部归原点。
                Cmd.CommunicateDp(0x10, 0);//轴一找原点
                Cmd.CommunicateDp(0x20, 0);//轴二找原点
                Cmd.CommunicateDp(0x30, 0);//轴三找原点
                Cmd.CommunicateDp(0x40, 0);//轴四找原点
                Cmd.CommunicateDp(0x93, 0);//关闭风机和补光

                if (!isLongRangeDebug)
                {
                    //Timer2Stop();
                    bStep = 13;
                    list.Clear();
                    list.Add(0);
                    list.Add(5);
                    list.Add(7);
                    list.Add(9);
                    startTimer1Time = DateTime.Now;
                    timer1.Start();
                }

                /*
                 * 开始计时此回归原点时间
                 */
                startTime = DateTime.Now;
                timer2.Start();
                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "Timer2_Start");
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }

        /// <summary>
        /// 应用
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button53_Click(object sender, EventArgs e)
        {
            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "点击事件_参数设置应用");
            string UploadIP = Param.Read_ConfigParam(configfileName, "Config", "UploadIP");
            string UploadPort = Param.Read_ConfigParam(configfileName, "Config", "UploadPort");
            string devId = Param.Read_ConfigParam(configfileName, "Config", "DeviceID");
            string dataType = Param.Read_ConfigParam(configfileName, "Config", "dataType");
            string currDataType = "";
            if (this.CbdataType.Text == "yyyy-MM-dd HH:mm:ss")
                currDataType = "0";
            else if (this.CbdataType.Text == "yyyy/MM/dd HH:mm:ss")
                currDataType = "1";
            string collectHour = Param.Read_ConfigParam(configfileName, "Config", "CollectHour");
            string collectMinute = Param.Read_ConfigParam(configfileName, "Config", "CollectMinute");
            if (this.txt_IP.Text.Trim() != UploadIP || this.txt_Port.Text.Trim() != UploadPort || this.TxtDevId.Text != devId || dataType != currDataType || this.txt_Hour.Text.Trim() != collectHour || this.txt_Mins.Text.Trim() != collectMinute)
            {
                //时间格式发生变化
                if (dataType != currDataType)
                {
                    ProgressForm progressForm = null;
                    if (currDataType == "0")
                    {
                        string sql = "select * from Record";
                        DataTable dataTable = DB.QueryDatabase(sql).Tables[0];
                        if (dataTable.Rows.Count > 0)
                        {
                            progressForm = new ProgressForm(0, dataTable.Rows.Count);
                            progressForm.Show();
                        }
                        for (int i = 0; i < dataTable.Rows.Count; i++)
                        {
                            progressForm.AddProgress();
                            string collectTime = dataTable.Rows[i]["CollectTime"].ToString();
                            string newCollectTime = collectTime.Replace("/", "-");
                            sql = "update Record Set CollectTime = '" + newCollectTime + "' where CollectTime='" + collectTime + "'";
                            int x = DB.updateDatabase(sql);
                            if (x == -1)
                            {
                                DebOutPut.DebLog("检测到您更改了日期格式，日期格式更改之后，系统将自动更新数据库必要信息，此信息表示数据库信息更新失败！");
                                //DebOutPut.WriteLog(LogType.Normal,LogDetailedType.Ordinary,
                                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "检测到您更改了日期格式，日期格式更改之后，系统将自动更新数据库必要信息，此信息表示数据库信息更新失败！");
                            }
                        }
                    }
                    else if (currDataType == "1")
                    {
                        string sql = "select * from Record";
                        DataTable dataTable = DB.QueryDatabase(sql).Tables[0];
                        if (dataTable.Rows.Count > 0)
                        {
                            progressForm = new ProgressForm(0, dataTable.Rows.Count);
                            progressForm.Show();
                        }
                        for (int i = 0; i < dataTable.Rows.Count; i++)
                        {
                            progressForm.AddProgress();
                            string collectTime = dataTable.Rows[i]["CollectTime"].ToString();
                            string newCollectTime = collectTime.Replace("-", "/");
                            sql = "update Record Set CollectTime = '" + newCollectTime + "' where CollectTime='" + collectTime + "'";
                            int x = DB.updateDatabase(sql);
                            if (x == -1)
                            {
                                DebOutPut.DebLog("检测到您更改了日期格式，日期格式更改之后，系统将自动更新数据库必要信息，此信息表示数据库信息更新失败！");
                                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "检测到您更改了日期格式，日期格式更改之后，系统将自动更新数据库必要信息，此信息表示数据库信息更新失败！");
                            }
                        }
                    }
                    if (progressForm != null)
                    {
                        progressForm.Close();
                        progressForm = null;
                    }
                }
                SetParamTab0(configfileName);
                DialogResult dialogResult = MessageBox.Show("检测到您更改了系统关键性配置，将在系统重启之后生效。点击“确定”将立即重启本程序，点击“取消”请稍后手动重启！", "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                if (dialogResult == DialogResult.OK)
                {
                    Tools.RestStart();
                }
            }
            ConEnCtrlTab0(false);
            SetParamTab0(configfileName);
            Param.Init_Param(configfileName);
        }




        private void SetParamTab0(string configfileName)
        {
            Param.Set_ConfigParm(configfileName, "Config", "UploadIP", txt_IP.Text);
            Param.Set_ConfigParm(configfileName, "Config", "UploadPort", txt_Port.Text);
            Param.Set_ConfigParm(configfileName, "Config", "CollectHour", txt_Hour.Text);
            Param.Set_ConfigParm(configfileName, "Config", "CollectMinute", txt_Mins.Text);
            Param.Set_ConfigParm(configfileName, "Config", "DeviceID", this.TxtDevId.Text);
            string dataType = "";
            if (this.CbdataType.Text == "yyyy-MM-dd HH:mm:ss")
                dataType = "0";
            else if (this.CbdataType.Text == "yyyy/MM/dd HH:mm:ss")
                dataType = "1";
            else
                dataType = "0";
            Param.Set_ConfigParm(configfileName, "Config", "dataType", dataType);
        }

        /// <summary>
        /// 修改
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "点击事件_参数设置修改");
            ConEnCtrlTab0(true);
        }

        private void ConEnCtrlTab0(bool isEn)
        {
            this.TxtDevId.Enabled = isEn;
            this.CbdataType.Enabled = isEn;
            this.txt_IP.Enabled = isEn;
            this.txt_Port.Enabled = isEn;
            this.txt_Hour.Enabled = isEn;
            this.txt_Mins.Enabled = isEn;
        }

        private void remotePaizhao()
        {
            //if()
        }
        private void RefeshWindowLabel()
        {
            try
            {
                txt_IP.Text = Param.UploadIP;
                txt_Port.Text = Param.UploadPort;
                txt_Hour.Text = Param.CollectHour;
                txt_Mins.Text = Param.CollectMinute;
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }

        /// <summary>
        /// 锁
        /// </summary>
        static readonly object SequenceLockdealMsg = new object();

        /// <summary>
        /// 处理远程命令
        /// </summary>
        /// <param name="jsonText">收到的Json串</param>
        public void dealMsg(string jsonText)
        {
            lock (SequenceLockdealMsg)
            {
                int func = -1;
                string collectTime = "";
                try
                {
                    if (jsonText != "")
                    {
                        JObject jo = (JObject)JsonConvert.DeserializeObject(jsonText);
                        if (jo.Property("devId") == null || jo.Property("devId").ToString() == "" || Param.DeviceID != jo["devId"].ToString() || jo.Property("func") == null || jo.Property("func").ToString() == "")
                        {
                            DebOutPut.DebLog("Socket事件_接收到的数据不合法！数据：" + jsonText);
                            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "Socket事件_接收到的数据不合法！数据：" + jsonText);
                            return;
                        }
                        func = Convert.ToInt16(jo["func"].ToString());
                        if (func == 100)
                            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.KeepAliveLog, "Socket事件_接收:" + jsonText);
                        else
                            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "Socket事件_接收:" + jsonText);
                        DebOutPut.DebLog("Socket事件_接收:" + jsonText);
                        switch (func)
                        {
                            case 100://连接保持帧头回应
                                     //tcpclient.sendKeepLive();
                                break;
                            case 101: //发送采集信息 上位机→服务器
                                DebOutPut.DebLog("发送采集信息收到回应，指令:101");
                                collectTime = Convert.ToString(jo["collectTime"].ToString());
                                collectTime = "update Record Set Flag = '1' where CollectTime='" + collectTime + "'";
                                int x = DB.updateDatabase(collectTime);
                                if (x != -1)
                                    isUpladCom = true;
                                else
                                {
                                    isUpladCom = false;
                                    DebOutPut.DebLog("收到采集信息上传回复，但数据库标志位更改失败！");
                                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "收到采集信息上传回复，但数据库标志位更改失败！");
                                }
                                break;
                            case 102: //发送位置信息 上位机→服务器
                                      //DebOutPut.DebLog( "发送位置信息收到回应，指令:102");
                                break;
                            case 130: //发送实时图像信息  上位机→服务器（略）
                                      //Debug.WriteLine("实时图像上传成功！");
                                break;
                            case 134: //设置载玻片数量和当前工作模式 上位机→服务器
                                      //DebOutPut.DebLog( "发送载玻片数量和当前工作模式收到回应，指令:134");
                                break;
                            case 110: //获取设备参数 服务器→上位机
                                int isbug = -1;
                                if (cb_switch.Checked == false)
                                    isbug = 0;
                                else if (cb_switch.Checked == true)
                                    isbug = 1;
                                tcpclient.sendDevParam(Convert.ToInt32(Param.CollectHour), Convert.ToInt32(Param.CollectMinute), Convert.ToInt32(Param.FanMinutes), Convert.ToInt32(Param.FanStrength), Convert.ToInt32(Param.peiyangye), Convert.ToInt32(Param.fanshilin), Convert.ToInt32(Param.peiyangtime), Convert.ToInt32(Param.MinSteps), Convert.ToInt32(Param.MaxSteps), Convert.ToInt32(Param.clearCount), Convert.ToInt32(Param.leftMaxSteps), Convert.ToInt32(Param.rightMaxSteps), Convert.ToInt32(Param.liftRightClearCount), Convert.ToInt32(Param.moveInterval), Convert.ToInt32(Param.FanStrengthMax), Convert.ToInt32(Param.FanStrengthMin), Convert.ToInt32(Param.tranStepsMin), Convert.ToInt32(Param.tranStepsMax), Convert.ToInt32(Param.tranClearCount), Convert.ToInt32(Param.XCorrecting), Convert.ToInt32(Param.YCorrecting), Convert.ToInt32(Param.YJustRange), Convert.ToInt32(Param.YNegaRange), Convert.ToInt32(Param.YInterval), Convert.ToInt32(Param.YJustCom), Convert.ToInt32(Param.YNageCom), Convert.ToInt32(Param.YFirst), Convert.ToInt32(Param.YCheck), isbug);
                                break;
                            case 111: //获取设备当前状态和当前动作 服务器→上位机
                                string position = "";
                                switch (locaiton)
                                {
                                    case 1:
                                        position = "原点";
                                        break;
                                    case 3:
                                        position = "推片";
                                        break;
                                    case 5:
                                        position = "粘附液";
                                        break;
                                    case 7:
                                        position = "收集";
                                        break;
                                    case 9:
                                        position = "培养液";
                                        break;
                                    case 11:
                                        position = "拍照";
                                        break;
                                    case 13:
                                        position = "回收";
                                        break;
                                    case 14:
                                        position = "复位";
                                        break;
                                    default:
                                        break;
                                }
                                if (position == "")
                                    position = "等待";
                                tcpclient.senddevStatus(111, "", statusInfo, position);
                                DebOutPut.DebLog("同步载破片数量和工作模式");
                                int remain1 = int.Parse(this.TxtRemain.Text.Trim());//载玻片数量
                                if (remain1 < 0)
                                    remain1 = 0;
                                int currRunMode1 = this.TxtRunMode.SelectedIndex;//当前工作模式
                                tcpclient.SendSlideGlassCount(134, "", remain1, currRunMode1, (float)wd);
                                break;
                            case 135: //获取工作时段 服务器→上位机
                                string temp;
                                String time1 = "[{ \"time1\":\"" + Param.work1 + "\"},";
                                String time2 = "{ \"time2\":\"" + Param.work2 + "\"},";
                                String time3 = "{ \"time3\":\"" + Param.work3 + "\"},";
                                String time4 = "{ \"time4\":\"" + Param.work4 + "\"},";
                                String time5 = "{ \"time5\":\"" + Param.work5 + "\"}]";
                                temp = time1 + time2 + time3 + time4 + time5;
                                tcpclient.sendtimeControl(135, "", temp);
                                break;
                            case 136://获取当前工作模式 服务器→上位机
                                tcpclient.sendWorkMode(Param.RunFlag);
                                break;
                            case 123: //设置设备参数 服务器→上位机
                                string mess = jo["message"].ToString();
                                jo = (JObject)JsonConvert.DeserializeObject(mess);
                                if (jo.Property("collectHour") != null && jo.Property("collectHour").ToString() != "")
                                {
                                    string hour = jo["collectHour"].ToString();
                                    if (hour != null && hour != "")
                                        Param.Set_ConfigParm(configfileName, "Config", "CollectHour", hour);
                                }
                                if (jo.Property("collectTime") != null && jo.Property("collectTime").ToString() != "")
                                {
                                    string minute = jo["collectTime"].ToString();
                                    if (minute != null && minute != "")
                                        Param.Set_ConfigParm(configfileName, "Config", "CollectMinute", minute);
                                }
                                if (jo.Property("sampleMinutes") != null && jo.Property("sampleMinutes").ToString() != "")
                                {
                                    string sampleMinutes = jo["sampleMinutes"].ToString();
                                    if (sampleMinutes != null && sampleMinutes != "")
                                        Param.Set_ConfigParm(configfileName, "Config", "FanMinutes", sampleMinutes);
                                }
                                if (jo.Property("sampleStrenth") != null && jo.Property("sampleStrenth").ToString() != "")
                                {
                                    string sampleStrenth = jo["sampleStrenth"].ToString();
                                    if (sampleStrenth != null && sampleStrenth != "")
                                        Param.Set_ConfigParm(configfileName, "Config", "FanStrength", sampleStrenth);
                                }
                                if (jo.Property("cultureCount") != null && jo.Property("cultureCount").ToString() != "")
                                {
                                    string cultureCount = jo["cultureCount"].ToString();
                                    if (cultureCount != null && cultureCount != "")
                                        Param.Set_ConfigParm(configfileName, "Config", "peiyangye", cultureCount);
                                }
                                if (jo.Property("VaselineCount") != null && jo.Property("VaselineCount").ToString() != "")
                                {
                                    string VaselineCount = jo["VaselineCount"].ToString();
                                    if (VaselineCount != null && VaselineCount != "")
                                        Param.Set_ConfigParm(configfileName, "Config", "fanshilin", VaselineCount);
                                }
                                if (jo.Property("cultureTime") != null && jo.Property("cultureTime").ToString() != "")
                                {
                                    string cultureTime = jo["cultureTime"].ToString();
                                    if (cultureTime != null && cultureTime != "")
                                        Param.Set_ConfigParm(configfileName, "Config", "peiyangtime", cultureTime);
                                }
                                if (jo.Property("minSteps") != null && jo.Property("minSteps").ToString() != "")
                                {
                                    string minSteps = jo["minSteps"].ToString();
                                    if (minSteps != null && minSteps != "")
                                        Param.Set_ConfigParm(configfileName, "Config", "MinSteps", minSteps);
                                }
                                if (jo.Property("maxSteps") != null && jo.Property("maxSteps").ToString() != "")
                                {
                                    string maxSteps = jo["maxSteps"].ToString();
                                    if (maxSteps != null && maxSteps != "")
                                        Param.Set_ConfigParm(configfileName, "Config", "MaxSteps", maxSteps);
                                }
                                if (jo.Property("clearCount") != null && jo.Property("clearCount").ToString() != "")
                                {
                                    string clearCount = jo["clearCount"].ToString();
                                    if (clearCount != null && clearCount != "")
                                        Param.Set_ConfigParm(configfileName, "Config", "ClearCount", clearCount);
                                }
                                if (jo.Property("leftMaxSteps") != null && jo.Property("leftMaxSteps").ToString() != "")
                                {
                                    string leftMaxSteps = jo["leftMaxSteps"].ToString();
                                    if (leftMaxSteps != null && leftMaxSteps != "")
                                        Param.Set_ConfigParm(configfileName, "Config", "LeftMaxSteps", leftMaxSteps);
                                }
                                if (jo.Property("rightMaxSteps") != null && jo.Property("rightMaxSteps").ToString() != "")
                                {
                                    string rightMaxSteps = jo["rightMaxSteps"].ToString();
                                    if (rightMaxSteps != null && rightMaxSteps != "")
                                        Param.Set_ConfigParm(configfileName, "Config", "RightMaxSteps", rightMaxSteps);
                                }
                                if (jo.Property("liftRightClearCount") != null && jo.Property("liftRightClearCount").ToString() != "")
                                {
                                    string liftRightClearCount = jo["liftRightClearCount"].ToString();
                                    if (liftRightClearCount != null && liftRightClearCount != "")
                                        Param.Set_ConfigParm(configfileName, "Config", "LiftRightClearCount", liftRightClearCount);
                                }
                                if (jo.Property("liftRightMoveInterval") != null && jo.Property("liftRightMoveInterval").ToString() != "")
                                {
                                    string liftRightMoveInterval = jo["liftRightMoveInterval"].ToString();
                                    if (liftRightMoveInterval != null && liftRightMoveInterval != "")
                                        Param.Set_ConfigParm(configfileName, "Config", "LiftRightMoveInterval", liftRightMoveInterval);
                                }
                                if (jo.Property("fanStrengthMax") != null && jo.Property("fanStrengthMax").ToString() != "")
                                {
                                    string fanStrengthMax = jo["fanStrengthMax"].ToString();
                                    if (fanStrengthMax != null && fanStrengthMax != "")
                                        Param.Set_ConfigParm(configfileName, "Config", "FanStrengthMax", fanStrengthMax);
                                }
                                if (jo.Property("fanStrengthMin") != null && jo.Property("fanStrengthMin").ToString() != "")
                                {
                                    string fanStrengthMin = jo["fanStrengthMin"].ToString();
                                    if (fanStrengthMin != null && fanStrengthMin != "")
                                        Param.Set_ConfigParm(configfileName, "Config", "FanStrengthMin", fanStrengthMin);
                                }
                                if (jo.Property("tranStepsMin") != null && jo.Property("tranStepsMin").ToString() != "")
                                {
                                    string tranStepsMin = jo["tranStepsMin"].ToString();
                                    if (tranStepsMin != null && tranStepsMin != "")
                                        Param.Set_ConfigParm(configfileName, "Config", "tranStepsMin", tranStepsMin);
                                }
                                if (jo.Property("tranStepsMax") != null && jo.Property("tranStepsMax").ToString() != "")
                                {
                                    string tranStepsMax = jo["tranStepsMax"].ToString();
                                    if (tranStepsMax != null && tranStepsMax != "")
                                        Param.Set_ConfigParm(configfileName, "Config", "tranStepsMax", tranStepsMax);
                                }
                                if (jo.Property("tranClearCount") != null && jo.Property("tranClearCount").ToString() != "")
                                {
                                    string tranClearCount = jo["tranClearCount"].ToString();
                                    if (tranClearCount != null && tranClearCount != "")
                                        Param.Set_ConfigParm(configfileName, "Config", "tranClearCount", tranClearCount);
                                }
                                if (jo.Property("xCorrecting") != null && jo.Property("xCorrecting").ToString() != "")
                                {
                                    string xCorrecting = jo["xCorrecting"].ToString();
                                    if (xCorrecting != null && xCorrecting != "")
                                        Param.Set_ConfigParm(configfileName, "Config", "XCorrecting", xCorrecting);
                                }
                                if (jo.Property("yCorrecting") != null && jo.Property("yCorrecting").ToString() != "")
                                {
                                    string yCorrecting = jo["yCorrecting"].ToString();
                                    if (yCorrecting != null && yCorrecting != "")
                                        Param.Set_ConfigParm(configfileName, "Config", "YCorrecting", yCorrecting);
                                }
                                if (jo.Property("yJustRange") != null && jo.Property("yJustRange").ToString() != "")
                                {
                                    string yJustRange = jo["yJustRange"].ToString();
                                    if (yJustRange != null && yJustRange != "")
                                        Param.Set_ConfigParm(configfileName, "Config", "YJustRange", yJustRange);
                                }
                                if (jo.Property("yNegaRange") != null && jo.Property("yNegaRange").ToString() != "")
                                {
                                    string yNegaRange = jo["yNegaRange"].ToString();
                                    if (yNegaRange != null && yNegaRange != "")
                                        Param.Set_ConfigParm(configfileName, "Config", "YNegaRange", yNegaRange);
                                }
                                if (jo.Property("yInterval") != null && jo.Property("yInterval").ToString() != "")
                                {
                                    string yInterval = jo["yInterval"].ToString();
                                    if (yInterval != null && yInterval != "")
                                        Param.Set_ConfigParm(configfileName, "Config", "YInterval", yInterval);
                                }
                                if (jo.Property("yJustCom") != null && jo.Property("yJustCom").ToString() != "")
                                {
                                    string yJustCom = jo["yJustCom"].ToString();
                                    if (yJustCom != null && yJustCom != "")
                                        Param.Set_ConfigParm(configfileName, "Config", "YJustCom", yJustCom);
                                }
                                if (jo.Property("yNageCom") != null && jo.Property("yNageCom").ToString() != "")
                                {
                                    string yNageCom = jo["yNageCom"].ToString();
                                    if (yNageCom != null && yNageCom != "")
                                        Param.Set_ConfigParm(configfileName, "Config", "YNageCom", yNageCom);
                                }
                                if (jo.Property("yFirst") != null && jo.Property("yFirst").ToString() != "")
                                {
                                    string yFirst = jo["yFirst"].ToString();
                                    if (yFirst != null && yFirst != "")
                                        Param.Set_ConfigParm(configfileName, "Config", "YFirst", yFirst);
                                }
                                if (jo.Property("yCheck") != null && jo.Property("yCheck").ToString() != "")
                                {
                                    string yCheck = jo["yCheck"].ToString();
                                    if (yCheck != null && yCheck != "")
                                        Param.Set_ConfigParm(configfileName, "Config", "YCheck", yCheck);
                                }
                                Param.Init_Param(configfileName);
                                RefeshWindowLabel();
                                tcpclient.Replay(123, "success", "");
                                break;
                            case 124: //工作模式切换：自动、定时、时段 服务器→上位机
                                string msg = jo["message"].ToString();
                                jo = (JObject)JsonConvert.DeserializeObject(msg);
                                string mode = jo["workmode"].ToString();
                                tcpclient.Replay(124, "success", "");
                                Param.Set_ConfigParm(configfileName, "Config", "RunFlag", mode);
                                DevStopWork();
                                DevStartWork();

                                DebOutPut.DebLog("同步载破片数量和工作模式");
                                int remain = int.Parse(this.TxtRemain.Text.Trim());//载玻片数量
                                if (remain < 0)
                                    remain = 0;
                                int currRunMode = this.TxtRunMode.SelectedIndex;//当前工作模式
                                tcpclient.SendSlideGlassCount(134, "", remain, currRunMode, (float)wd);
                                break;
                            case 129: //远程重启设备  服务器→上位机
                                tcpclient.Replay(129, "success", "");
                                Tools.RestStart();
                                break;
                            case 131: //远程开关设备  服务器→上位机
                                string mess1 = jo["message"].ToString();
                                tcpclient.Replay(131, "success", "");
                                if (mess1.ToUpper() == "ON")
                                {
                                    if (!devIsStart)
                                    {
                                        statusInfo = "正常";
                                        locaiton = 13;
                                        tcpclient.SendCurrAction(142, "", "回收");
                                        hideLocations();
                                        DevStartWork();
                                    }
                                }
                                else if (mess1.ToUpper() == "OFF")
                                {
                                    if (devIsStart)
                                    {
                                        statusInfo = "关机";
                                        hideLocations();
                                        DevStopWork();
                                    }
                                }
                                break;
                            case 133: //设置工作时段 服务器→上位机
                                string msgg = jo["message"].ToString();
                                int a = msgg.IndexOf(':');
                                string str1 = msgg.Substring(0, a + 1);
                                int b = msgg.IndexOf('[');
                                int c = msgg.IndexOf(']');
                                string str2 = msgg.Substring(b);
                                int d = str2.IndexOf(']');
                                string str3 = str2.Substring(0, d + 1);
                                string str4 = str2.Substring(str2.Length - 1);
                                string strz = str1 + str3 + str4;//去掉了转义字符之后的
                                TimeRoot root1 = new TimeRoot();
                                root1 = JsonConvert.DeserializeObject<TimeRoot>(strz);
                                while (root1.timecontrol.Count < 5)
                                    root1.timecontrol.Add("00:00-00:00");
                                writeWorktime(root1.timecontrol);
                                tcpclient.Replay(133, "success", "");
                                break;
                            /*
                             * 远程调试模式下
                             */
                            case 143: //设备状态切换：正常、调试 服务器→上位机
                                string msg1 = jo["message"].ToString();
                                tcpclient.Replay(143, "success", "");
                                if (msg1 == "1")
                                {
                                    NormalMode();
                                    this.cb_switch.Checked = false;
                                    isLongRangeDebug = false;
                                }
                                else if (msg1 == "0")
                                {
                                    Thread thread1 = new Thread(DevState);
                                    thread1.IsBackground = true;
                                    thread1.Start();
                                }
                                break;
                            case 127: //调整载物台 服务器→上位机(略)
                                break;
                            case 128: //设备复位  服务器→上位机
                                if (isDevRes == false)
                                {
                                    tcpclient.Replay(128, "success", "");
                                    isDevRes = true;
                                    Thread thread = new Thread(DevRes);
                                    thread.IsBackground = true;
                                    thread.Start();
                                }
                                break;
                            case 137: //推片 服务器→上位机

                                if (cb_switch.Checked)
                                {
                                    if (label21.Text == "存在")
                                        tcpclient.Replay(137, "fail", "载玻片已存在");
                                    else
                                    {
                                        tcpclient.Replay(137, "success", "");
                                        bstop = false;
                                        setLocation(2);
                                        inituipian();
                                    }
                                }
                                else
                                    tcpclient.Replay(137, "fail", "只有调试模式下才可使用此功能");
                                break;
                            case 138: //滴加粘附液 服务器→上位机
                                if (cb_switch.Checked)
                                {
                                    tcpclient.Replay(138, "success", "");
                                    setLocation(4);
                                    tuifanshilin();
                                }
                                else
                                    tcpclient.Replay(138, "fail", "只有调试模式下才可使用此功能");
                                break;
                            case 139: //收集 服务器→上位机
                                if (cb_switch.Checked)
                                {
                                    tcpclient.Replay(139, "success", "");
                                    setLocation(6);
                                    tuipianFengshan();
                                }
                                else
                                    tcpclient.Replay(139, "fail", "只有调试模式下才可使用此功能");
                                break;
                            case 140: //滴加培养液 服务器→上位机
                                if (cb_switch.Checked)
                                {
                                    tcpclient.Replay(140, "success", "");
                                    setLocation(8);
                                    tuiPeiyangye();
                                }
                                else
                                    tcpclient.Replay(140, "fail", "只有调试模式下才可使用此功能");
                                break;
                            case 126: //拍照  服务器→上位机
                                if (cb_switch.Checked)
                                {
                                    tcpclient.Replay(126, "success", "开始拍照");
                                    setLocation(10);
                                    tuipaizhao();
                                }
                                else
                                    tcpclient.Replay(126, "fail", "只有调试模式下才可使用此功能");
                                break;
                            case 141: //回收载玻片 服务器→上位机
                                if (cb_switch.Checked)
                                {
                                    tcpclient.Replay(141, "success", "");
                                    Thread thread = new Thread(ResCard);
                                    thread.IsBackground = true;
                                    thread.Start();
                                }
                                else
                                    tcpclient.Replay(141, "fail", "只有调试模式下才可使用此功能");
                                break;
                            case 142://当前动作 服务器→上位机
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (func == -1)
                    {
                        DebOutPut.DebLog("Socket事件_接收到的数据不合法！接收数据：" + jsonText + "\r\n错误信息：" + ex.ToString());
                        DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, "Socket事件_接收到的数据不合法！接收数据：" + jsonText + "\r\n错误信息：" + ex.ToString());
                    }
                    else
                    {
                        DebOutPut.DebLog("功能码：" + func + " Err！\r\n接收数据：" + jsonText + "\r\n错误信息：" + ex.ToString());
                        DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, "功能码：" + func + " Err！\r\n接收数据：" + jsonText + "\r\n错误信息：" + ex.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// 回收片
        /// </summary>
        private void ResCard()
        {
            setLocation(12);
            while (!isReady() && !bstop)
            {
                DebOutPut.DebLog("检测状态不到位");
                Thread.Sleep(2000);
            }
            if (bstop)
                return;
            locaiton = 13;
            ResetStatu();
            Thread.Sleep(110 * 1000);
            setLocation(0);
            tcpclient.SendCurrAction(142, "", "原点");
            locaiton = 1;

        }


        /// <summary>
        /// 设备状态切换
        /// </summary>
        private void DevState()
        {
            locaiton = 14;
            while (!isReady() && !bstop)
            {
                DebOutPut.DebLog("检测状态不到位");
                Thread.Sleep(2000);
            }
            if (bstop)
                return;
            DebugMode();
            this.cb_switch.Checked = true;
            isLongRangeDebug = true;
            SerialInit();
            Cmd.InitComm(serialPort1);
            ResetStatu();
            Thread.Sleep(110 * 1000);
            tcpclient.SendCurrAction(142, "", "原点");
            locaiton = 1;
        }

        /// <summary>
        /// 调试模式
        /// </summary>
        private void DebugMode()
        {
            try
            {
                //停止本机工作,进入到调试模式
                hideLocations();
                groupBox10.Enabled = true;
                cb_switch.Enabled = true;
                label26.Text = "当前工作模式:" + "调试模式";
                stopDevRun();
                bstop = true;
                Timer1Stop();
                Timer2Stop();
                Timer3Stop();
                Timer4Stop();
            }
            catch (Exception ex)
            {
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }

        /// <summary>
        /// 正常模式
        /// </summary>
        private void NormalMode()
        {
            try
            {
                cb_switch.Enabled = false;
                groupBox10.Enabled = false;
                runmode = 0;
                bstop = false;
                Param.Init_Param(configfileName); //恢复正常参数
                if (SerialInit())
                {
                    Cmd.InitComm(serialPort1);
                    //InitDev_FuWei(); //判断设备是否就绪，可以开始新的流程了。
                    if (autoFlag) //开机自动运行
                        label26.Text = "当前工作模式:" + "自动运行";
                    else if (!autoFlag && Param.RunFlag == "2")
                        label26.Text = "当前工作模式:" + "时段运行";
                    else if (!autoFlag && Param.RunFlag == "1")
                        label26.Text = "当前工作模式:" + "定时运行";
                    InitDev(); //判断设备是否就绪，可以开始新的流程了。
                    if (!timer3.Enabled)
                    {
                        if (autoFlag) //开机自动运行
                            timer4.Start();
                        else if (!autoFlag)
                        {
                            DebOutPut.DebLog("恢复正常模式_设备初始化");
                            DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, "恢复正常模式_设备初始化");
                            Cmd.CommunicateDp(0x10, 0);
                            DebOutPut.DebLog("恢复正常模式_轴一找原点");
                            Cmd.CommunicateDp(0x20, 0);
                            DebOutPut.DebLog("恢复正常模式_轴二找原点");
                            Cmd.CommunicateDp(0x30, 0);
                            DebOutPut.DebLog("恢复正常模式_轴三找原点");
                            Cmd.CommunicateDp(0x40, 0);
                            DebOutPut.DebLog("恢复正常模式_轴四找原点");
                            Cmd.CommunicateDp(0x93, 0);
                            DebOutPut.DebLog("恢复正常模式_关闭风机和补光");
                            list.Clear();
                            list.Add(0);
                            list.Add(5);
                            list.Add(7);
                            list.Add(9);
                            while (!isReady())
                                Thread.Sleep(50);
                            setLocation(0);
                            timer5.Start();
                        }
                    }
                }
                cb_switch.Enabled = true;
            }
            catch (Exception ex)
            {
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }


        bool isDevRes = false;
        /// <summary>
        /// 设备复位线程
        /// </summary>
        private void DevRes()
        {
            while (!isReady() && !bstop)
            {
                DebOutPut.DebLog("检测状态不到位");
                Thread.Sleep(2000);
            }
            if (bstop)
                return;
            locaiton = 14;//表示复位位置
            //进入调试模式
            DebugMode();
            this.cb_switch.Checked = true;
            isLongRangeDebug = true;
            //执行复位
            ResetStatu();
            //切回正常模式
            NormalMode();
            if (autoFlag) //开机自动运行
                label26.Text = "当前工作模式:" + "自动运行";
            else if (!autoFlag && Param.RunFlag == "2")
                label26.Text = "当前工作模式:" + "时段运行";
            else if (!autoFlag)//定时运行
                label26.Text = "当前工作模式:" + "定时运行";
            this.cb_switch.Checked = false;
            isLongRangeDebug = false;
            //关机
            statusInfo = "关机";
            hideLocations();
            DevStopWork();
            Thread.Sleep(100 * 1000);
            tcpclient.SendCurrAction(142, "", "原点");
            locaiton = 1;
            isDevRes = false;
        }


        /// <summary>
        /// 复位状态--调试模式下
        /// </summary>
        private void ResetStatu()
        {
            try
            {
                //执行复位
                runmode = 0;
                bstop = false;
                Param.Init_Param(configfileName);
                if (SerialInit())
                {
                    Cmd.InitComm(serialPort1);
                    //设备初始化
                    InitDev_FuWei();
                    //回归原点
                    if (bStep == 13 || bStep == 12 || bStep == 0)
                    {
                        if (berror == true)
                        {
                            return;
                        }
                        bStep = 0;//
                                  //1.设备初始化，全部归原点。
                        Cmd.CommunicateDp(0x10, 0);//轴一找原点
                        Cmd.CommunicateDp(0x20, 0);//轴二找原点
                        Cmd.CommunicateDp(0x30, 0);//轴三找原点
                        Cmd.CommunicateDp(0x40, 0);//轴四找原点
                        Cmd.CommunicateDp(0x93, 0);//关闭风机和补光
                        list.Clear();
                        list.Add(0);
                        list.Add(5);
                        list.Add(7);
                        list.Add(9);
                    }
                }
            }
            catch (Exception ex)
            {
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }



        /// <summary>
        /// 微调载物台--调试模式下
        /// </summary>
        private void FineTuningLoadingStage(string type)
        {
            try
            {
                DebugMode();
                if (type == "up")
                {
                    MoveUporMove(false, "1");
                }
                else if (type == "down")
                {
                    MoveUporMove(true, "1");
                }
            }
            catch (Exception ex)
            {
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }

        }



        /// <summary>
        /// 设备停止工作
        /// </summary>
        private void DevStopWork()
        {
            try
            {
                stopDevRun();
                bstop = true;
                Timer1Stop();
                Timer2Stop();
                Timer3Stop();
                Timer4Stop();
                devIsStart = false;
            }
            catch (Exception ex)
            {
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }

        /// <summary>
        /// 设备开始工作
        /// </summary>
        private void DevStartWork()
        {
            try
            {
                //程序开始工作
                //hideLocations();
                runmode = 0;
                bstop = false;
                Param.Init_Param(configfileName); //恢复正常参数
                                                  //RunFlag 0:自动运行 1:定时运行  2:分时运行 
                if (Param.RunFlag == "0")
                {
                    autoFlag = true;
                }
                else if (Param.RunFlag == "1" || Param.RunFlag == "2")
                {
                    autoFlag = false;
                }
                if (SerialInit())
                {
                    Cmd.InitComm(serialPort1);
                    if (autoFlag) //开机自动运行
                        label26.Text = "当前工作模式:" + "自动运行";
                    else if (!autoFlag && Param.RunFlag == "2")
                        label26.Text = "当前工作模式:" + "时段运行";
                    else if (!autoFlag && Param.RunFlag == "1")
                        label26.Text = "当前工作模式:" + "定时运行";
                    InitDev(); //判断设备是否就绪，可以开始新的流程了。
                    if (!timer3.Enabled)
                    {
                        if (autoFlag) //自动运行
                            timer4.Start();
                        else if (!autoFlag) //非自动运行
                        {
                            DebOutPut.DebLog("工作模式切换_设备初始化");
                            DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, "工作模式切换_设备初始化");
                            Cmd.CommunicateDp(0x10, 0);
                            DebOutPut.DebLog("工作模式切换_轴一找原点");
                            Cmd.CommunicateDp(0x20, 0);
                            DebOutPut.DebLog("工作模式切换_轴二找原点");
                            Cmd.CommunicateDp(0x30, 0);
                            DebOutPut.DebLog("工作模式切换_轴三找原点");
                            Cmd.CommunicateDp(0x40, 0);
                            DebOutPut.DebLog("工作模式切换_轴四找原点");
                            Cmd.CommunicateDp(0x93, 0);
                            DebOutPut.DebLog("工作模式切换_关闭风机和补光");
                            list.Clear();
                            list.Add(0);
                            list.Add(5);
                            list.Add(7);
                            list.Add(9);
                            while (!isReady())
                                Thread.Sleep(50);
                            setLocation(0);
                            timer5.Start();
                        }
                    }
                }
                devIsStart = true;
            }
            catch (Exception ex)
            {
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }


        private void writeWorktime(List<String> list)
        {
            try
            {
                String str;
                Param.Set_ConfigParm(configfileName, "Config", "work1", "");
                Param.Set_ConfigParm(configfileName, "Config", "work2", "");
                Param.Set_ConfigParm(configfileName, "Config", "work3", "");
                Param.Set_ConfigParm(configfileName, "Config", "work4", "");
                Param.Set_ConfigParm(configfileName, "Config", "work5", "");
                for (int i = 0; i < list.Count; i++)
                {
                    str = String.Format("work{0:G}", i + 1);
                    Param.Set_ConfigParm(configfileName, "Config", str, list[i]);
                }
                Param.Init_Param(configfileName);
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }

        private void hideLocations()
        {
            foreach (PictureBox pic in ctrls)
            {
                pic.Visible = false;
            }
        }
        private void setLocation(int index)
        {
            try
            {
                hideLocations();
                ctrls[index].Visible = true;
                Application.DoEvents();
                locaiton = index + 1;

                string message = "";
                switch (index)
                {
                    case 0: message = "到达原点位置"; break;
                    case 2: message = "到达推片位置，推片就绪"; break;
                    case 4: message = "滴加粘附液"; break;
                    case 6: message = "收集孢子"; break;
                    case 8: message = "滴加培养液"; break;
                    case 10: message = "推片到拍照位置"; break;
                    case 12: message = "回收玻片"; break;
                    default: break;
                }
                if (!string.IsNullOrEmpty(message))
                {
                    Thread thread = new Thread(new ParameterizedThreadStart(Speaking));
                    thread.IsBackground = true;
                    thread.Start(message);
                }
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }

        }


        #region 语音播报
        /// <summary>
        /// 语音播报
        /// </summary>
        /// <param name="saying"></param>
        private void Speaking(object saying)
        {
            string say = saying + "";
            Task task = new Task(() =>
            {
                SpeechSynthesizer speech = new SpeechSynthesizer();
                speech.Volume = 100; //音量
                speech.Rate = 0;//语速-10至10
                CultureInfo keyboardCulture = System.Windows.Forms.InputLanguage.CurrentInputLanguage.Culture;
                InstalledVoice neededVoice = speech.GetInstalledVoices(keyboardCulture).FirstOrDefault();
                if (neededVoice == null)
                {
                    say = "";
                }
                else
                {
                    speech.SelectVoice(neededVoice.VoiceInfo.Name);
                }
                try
                {
                    if (!string.IsNullOrEmpty(say))
                    {
                        speech.Speak(say);
                    }
                }
                catch (Exception ex)
                {
                    DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, "语音播报异常" + ex.Message);
                    return;
                }
            });
            task.Start();
        }
        #endregion

        /*
         * 此函数为设备启动后的初始化函数：
         * 判断设备当前所处状态：主要判断载物台的位置及是否有载玻片：如果有载玻片及不在远点，现将载玻片回收后回归原地
         */
        public void InitDev()
        {
            try
            {
                int count = 0;
                byte[] ret = Cmd.CommunicateDp(0xA0, 0);
                if (ret == null || ret[0] != 0xFF)
                {
                    statusInfo = "初始化读取状态失败,请处理！";
                    label18.Text = "初始化读取状态失败,请处理！";
                    label18.ForeColor = System.Drawing.Color.Red;
                    DebOutPut.DebLog("初始化读取状态失败！");
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "初始化读取状态失败！");
                    return;
                }
                int dirs = (ret[7] << 8) | ret[6];
                DebOutPut.DebLog("初始化：dirs值：" + dirs);
                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "初始化：dirs值：" + dirs);
                //有载玻片：
                if (((dirs >> 13) & 0x01) == 1)
                {
                    label21.Text = "存在";
                    label21.ForeColor = System.Drawing.Color.Black;
                    Cmd.CommunicateDp(0x13, 4);
                    list.Clear();
                    list.Add(3);
                    DebOutPut.DebLog("第一次输出：设备运行停止位标志：" + bstop);
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "第一次输出：设备运行停止位标志：" + bstop);
                    while (!isReady() && !bstop)
                    {
                        if (serialPort1 == null || !serialPort1.IsOpen)
                        {
                            DebOutPut.DebLog("serialPort1未打开！");
                            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "serialPort1未打开！");
                            return;
                        }
                        DebOutPut.DebLog("设备初始化，有载玻片存在，先去培养液位置");
                        DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "设备初始化，有载玻片存在，先去培养液位置");
                        Thread.Sleep(1000);
                    }
                    DebOutPut.DebLog("第二次输出：设备运行停止位标志：" + bstop);
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "第二次输出：设备运行停止位标志：" + bstop);
                    if (bstop)
                        return;
                    DebOutPut.DebLog("轴一到培养液位置");
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "轴一到培养液位置");
                    Thread.Sleep(5000);
                    Cmd.CommunicateDp(0x33, 2);
                    if (Param.recoveryDevice == "0")
                        Cmd.CommunicateDp(0x43, 2);
                    else if (Param.recoveryDevice == "1")
                        Cmd.CommunicateDp(0x43, 1);
                    Cmd.CommunicateDp(0x13, 5);
                    nFinishStep = 0;
                    list.Clear();
                    list.Add(4);
                    DebOutPut.DebLog("timer3_Start");
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "timer3_Start");
                    startTime = DateTime.Now;
                    //回收载玻片
                    timer3.Start();
                    count++;
                }
                else
                {
                    bStep = 0;
                    label21.Text = "不存在";
                    label21.ForeColor = System.Drawing.Color.Red;
                }
                //无培养液
                if (((dirs >> 12) & 0x01) == 0)
                {
                    label20.Text = "培养液缺液";
                    label20.ForeColor = System.Drawing.Color.Red;
                }
                else
                {

                    label20.Text = "正常";
                    label20.ForeColor = System.Drawing.Color.Black;
                    count++;
                }
                if (Param.DripDevice == "0")
                {
                    //无粘附液
                    if (((dirs >> 11) & 0x01) == 0)
                    {
                        label19.Text = "粘附液缺液";
                        label19.ForeColor = System.Drawing.Color.Red;
                    }
                    else
                    {
                        label19.Text = "正常";
                        label19.ForeColor = System.Drawing.Color.Black;
                        count++;
                    }
                }
                else if (Param.DripDevice == "1")
                {
                    string stuta = PushingFluidRead();
                    //无粘附液
                    if (stuta == "01")
                    {
                        label19.Text = "粘附液缺液";
                        label19.ForeColor = System.Drawing.Color.Red;
                    }
                    else if (stuta == "00")
                    {
                        label19.Text = "正常";
                        label19.ForeColor = System.Drawing.Color.Black;
                        count++;
                    }
                }
                //雨
                if (((dirs >> 14) & 0x01) == 0)
                    lb_yk.Text = "无雨";
                else
                    lb_yk.Text = "有雨";
                if (count != 3)
                {
                    string str = "";
                    if (label21.Text.Trim() == "不存在")
                        str += "载玻片" + label21.Text.Trim();
                    if (label20.Text.Trim() == "培养液缺液")
                    {
                        if (str != "")
                            str += ";" + label20.Text.Trim();
                        else if (str == "")
                            str += label20.Text.Trim();
                    }
                    if (label19.Text.Trim() == "粘附液缺液")
                    {
                        if (str != "")
                            str += ";" + label19.Text.Trim();
                        else if (str == "")
                            str += label19.Text.Trim();
                    }
                    if (str == "")
                        statusInfo = "正常";
                    else
                        statusInfo = str;
                }
                else
                    statusInfo = "正常";
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }

        /// <summary>
        /// 推液电机移动
        /// </summary>
        /// <param name="step">移动步数</param>
        /// <param name="isPosNeg">true正     false反</param>
        private void PushingFluidMove(bool isPosNeg, string step)
        {
            if (serialPort4 == null || !serialPort4.IsOpen)
            {
                DebOutPut.DebLog("副控串口未打开!");
                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "副控串口未打开!");
                return;
            }
            DebOutPut.DebLog("推液电机正在移动至指定位置");
            int index = 0;
            serialPort4.DiscardInBuffer();
            string frameHead = "5AA5";
            string func = "34";
            string content = (isPosNeg == true) ? "01" : "00";
            string step1 = Tools.TenToSixteen(step).PadLeft(4, '0');
            string frameTail = "F0";
            string data = frameHead + func + content + step1 + frameTail;
            byte[] bytes = Tools.HexStrTobyte(data);

            for (int i = 0; i < 3; i++)
            {
                serialPort4.Write(bytes, 0, bytes.Length);
                DebOutPut.DebLog("发送:" + Cmd.byteToHexStr(bytes));
                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.ComLog, "发送:" + Cmd.byteToHexStr(bytes));
                Thread.Sleep(350);
                if (serialPort4.BytesToRead <= 0)
                    index++;
                else
                    break;
            }
            if (index == 3)
            {
                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.ComLog, "发送:未收到终端回应！");
                DebOutPut.DebLog("未收到终端回应！请重试!");
                return;
            }
            byte[] rec = new byte[5];
            serialPort4.Read(rec, 0, 5);
            string recStr = Cmd.byteToHexStr(rec);
            string recContent = recStr.Substring(6, 2);
            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.ComLog, "接收到:" + recStr + "     返回内容:" + recContent);
            DebOutPut.DebLog("接收到:" + recStr + "     返回内容:" + recContent);
        }

        /// <summary>
        /// 推液电机位置读取
        /// </summary>
        private string PushingFluidRead()
        {
            if (serialPort4 == null || !serialPort4.IsOpen)
            {
                DebOutPut.DebLog("副控串口未打开!");
                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "副控串口未打开!");
                return "";
            }
            int index = 0;
            serialPort4.DiscardInBuffer();
            string frameHead = "5AA5";
            string func = "35";
            string content = "00";
            string frameTail = "F0";
            string data = frameHead + func + content + frameTail;
            byte[] bytes = Tools.HexStrTobyte(data);

            for (int i = 0; i < 3; i++)
            {
                serialPort4.Write(bytes, 0, bytes.Length);
                DebOutPut.DebLog("发送:" + Cmd.byteToHexStr(bytes));
                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.ComLog, "发送:" + Cmd.byteToHexStr(bytes));
                Thread.Sleep(350);
                if (serialPort4.BytesToRead <= 0)
                    index++;
                else
                    break;
            }
            if (index == 3)
            {
                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.ComLog, "发送:未收到终端回应！");
                DebOutPut.DebLog("未收到终端回应！请重试!");
                return "";
            }
            byte[] rec = new byte[5];
            serialPort4.Read(rec, 0, 5);
            string recStr = Cmd.byteToHexStr(rec);
            string recContent = recStr.Substring(6, 2);
            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.ComLog, "接收到:" + recStr + "     返回内容:" + recContent);
            DebOutPut.DebLog("接收到:" + recStr + "     返回内容:" + recContent);
            return recContent;
        }



        public void InitDev_FuWei()
        {
            try
            {
                byte[] ret = Cmd.CommunicateDp(0xA0, 0);
                if (ret == null || ret[0] != 0xFF)
                {
                    return;
                }
                int dirs = (ret[7] << 8) | ret[6];
                //有载玻片：
                if (((dirs >> 13) & 0x01) == 1)
                {
                    label21.Text = "存在";
                    label21.ForeColor = System.Drawing.Color.Black;
                    Cmd.CommunicateDp(0x33, 2);
                    if (Param.recoveryDevice == "0")
                        Cmd.CommunicateDp(0x43, 2);
                    else if (Param.recoveryDevice == "1")
                        Cmd.CommunicateDp(0x43, 1);
                    Cmd.CommunicateDp(0x13, 5);
                    nFinishStep = 0;
                    list.Clear();
                    list.Add(4);
                    startTime = DateTime.Now;
                    int i = 0;
                    while (!isReady() && !bstop)
                    {
                        if (serialPort1 == null || !serialPort1.IsOpen)
                        {
                            return;
                        }
                        i++;
                        DebOutPut.DebLog("未就位");
                        //if (i > 100)
                        //{
                        //    return;
                        //}
                        Thread.Sleep(1000);
                    }
                    if (bstop)
                        return;
                    Timer3Stop();//查询状态成功，准备就绪
                    switch (nFinishStep)
                    {
                        case 0:
                            DebOutPut.DebLog("回收片");
                            bStep = 12;
                            //2. 推片准备：轴3 到接片位置 轴4到原点位置
                            Cmd.CommunicateDp(0x33, 1);
                            //Thread.Sleep(10000);
                            Thread.Sleep(10000);
                            if (Param.recoveryDevice == "0")
                                Cmd.CommunicateDp(0x43, 1);
                            else if (Param.recoveryDevice == "1")
                                Cmd.CommunicateDp(0x43, 2);
                            //Thread.Sleep(10000);
                            Thread.Sleep(10000);
                            list.Clear();
                            list.Add(7);
                            if (Param.recoveryDevice == "0")
                                list.Add(9);
                            else if (Param.recoveryDevice == "1")
                                list.Add(10);
                            label21.Text = "载玻片不存在";
                            label21.ForeColor = System.Drawing.Color.Red;
                            break;
                        default:
                            break;
                    }

                }
                else
                {
                    bStep = 0;
                    label21.Text = "不存在";
                    label21.ForeColor = System.Drawing.Color.Red;
                }
                //无培养液
                if (((dirs >> 12) & 0x01) == 0)
                {
                    label20.Text = "培养液缺液";
                    label20.ForeColor = System.Drawing.Color.Red;
                }
                else
                {

                    label20.Text = "正常";
                    label20.ForeColor = System.Drawing.Color.Black;
                }
                if (Param.DripDevice == "0")
                {
                    //无粘附液
                    if (((dirs >> 11) & 0x01) == 0)
                    {
                        label19.Text = "粘附液缺液";
                        label19.ForeColor = System.Drawing.Color.Red;
                    }
                    else
                    {
                        label19.Text = "正常";
                        label19.ForeColor = System.Drawing.Color.Black;
                    }
                }
                else if (Param.DripDevice == "1")
                {
                    string stuta = PushingFluidRead();
                    //无粘附液
                    if (stuta == "01")
                    {
                        label19.Text = "粘附液缺液";
                        label19.ForeColor = System.Drawing.Color.Red;
                    }
                    else if (stuta == "00")
                    {
                        label19.Text = "正常";
                        label19.ForeColor = System.Drawing.Color.Black;
                    }
                }
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }



        public bool SerialInit()
        {
            List<string> list = new List<string>();
            list.Add(Param.SerialPortName);
            list.Add(Param.SerialPortCamera);
            list.Add(Param.SerialPortGpsName);
            list.Add(Param.SerialPortHjName);
            if (list.Distinct().Count<string>() != list.Count)//排查数组中是否有重复元素
            {
                statusInfo = "串口配置错误,请处理！";
                label18.Text = "串口配置错误,请处理！";
                DebOutPut.DebLog("串口配置错误");
                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "串口配置错误");
                label18.ForeColor = System.Drawing.Color.Red;
                return false;
            }
            String[] Portname = SerialPort.GetPortNames();
            int serialCount = 0;
            bool SerialInitOk = false;
            do
            {
                try
                {
                    //主机通讯串口
                    if (Portname.Contains(Param.SerialPortName))
                    {
                        if (serialPort1.IsOpen)
                        {
                            serialPort1.Close();
                        }
                        serialPort1.PortName = Param.SerialPortName;
                        serialPort1.BaudRate = Convert.ToInt32(115200);
                        serialPort1.ReceivedBytesThreshold = 1;
                        serialPort1.Open();
                        serialCount++;
                        SerialInitOk = true;
                    }
                    else
                    {
                        statusInfo = "主控串口打开故障";
                        label18.Text = "主控串口打开故障,请处理！";
                        DebOutPut.DebLog("主控串口打开故障");
                        DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "主控串口打开故障");
                        label18.ForeColor = System.Drawing.Color.Red;
                        return false;
                    }
                    if (Portname.Contains(Param.SerialPortCamera))
                    {
                        if (serialPort4.IsOpen)
                        {
                            serialPort4.Close();
                        }
                        serialPort4.PortName = Param.SerialPortCamera;
                        serialPort4.BaudRate = Convert.ToInt32(115200);
                        serialPort4.ReceivedBytesThreshold = 1;
                        serialPort4.Open();
                    }
                    else
                    {
                        DebOutPut.DebLog("副控串口打开故障");
                        DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "副控串口打开故障");
                        if (int.Parse(Param.YJustRange) != 0 || int.Parse(Param.YNegaRange) != 0 || int.Parse(Param.YCorrecting) != 0 || Param.DripDevice == "1")
                        {
                            statusInfo = "副控串口打开故障";
                            label18.Text = "副控串口打开故障,请处理！";
                            label18.ForeColor = System.Drawing.Color.Red;
                            return false;
                        }
                    }
                    if (Portname.Contains(Param.SerialPortGpsName))
                    {
                        if (serialPort2.IsOpen)
                        {
                            serialPort2.Close();
                        }
                        if (Param.SerialPortGpsName != null && Param.SerialPortGpsName != "")
                        {
                            serialPort2.PortName = Param.SerialPortGpsName;
                            serialPort2.BaudRate = Convert.ToInt32(9600);
                            serialPort2.ReceivedBytesThreshold = 100;
                            serialPort2.Open();
                            serialPort2.WriteLine("fsdfsadf");
                        }
                    }
                    else
                    {
                        DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "GPS通讯串口打开故障");
                    }

                    if (Portname.Contains(Param.SerialPortHjName))
                    {
                        if (serialPort3.IsOpen)
                        {
                            serialPort3.Close();
                        }
                        if (Param.SerialPortHjName != null && Param.SerialPortHjName != "")
                        {
                            serialPort3.PortName = Param.SerialPortHjName;
                            serialPort3.BaudRate = Convert.ToInt32(9600);
                            serialPort3.ReceivedBytesThreshold = 100;
                            serialPort3.Open();
                        }
                    }
                    else
                    {
                        DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "环境通讯串口打开故障");
                    }
                }
                catch (Exception ex)
                {
                    serialCount++;
                    if (serialCount == 4)
                    {
                        statusInfo = "串口初始化失败";
                        label18.Text = "串口初始化失败,请处理！";
                        label18.ForeColor = System.Drawing.Color.Red;
                    }
                    DebOutPut.DebLog("串口初始化失败(第" + serialCount.ToString() + "次)——" + ex.ToString());
                    DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, "串口初始化失败(第" + serialCount.ToString() + "次)——" + ex.Message);
                    SerialInitOk = false;

                }
                Thread.Sleep(100);
            } while (!SerialInitOk && !(serialCount == 4));

            if (SerialInitOk && _reconnection)
            {
                //采集开始

            }
            return SerialInitOk;
        }

        /// <summary>
        /// 启动相机
        /// </summary>
        private void Start_GrabImage()
        {
            try
            {
                SeacehDev();//搜索设备
                OpenDev();//打开设备
                StartCollection();//开始采集
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }

        /// <summary>
        /// 搜索设备
        /// </summary>
        private void SeacehDev()
        {
            try
            {
                int nRet;
                // ch:创建设备列表 en:Create Device List
                cbDeviceList.Items.Clear();
                nRet = MyCamera.MV_CC_EnumDevices_NET(MyCamera.MV_GIGE_DEVICE | MyCamera.MV_USB_DEVICE, ref m_pDeviceList);
                if (0 != nRet)
                {
                    DebOutPut.DebLog("未检索到相机，nRet：" + nRet);
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "未检索到相机，nRet：" + nRet);
                    cameraErrStr = "未检索到相机，nRet：" + nRet;
                    return;
                }

                // ch:在窗体列表中显示设备名 | en:Display device name in the form list
                for (int i = 0; i < m_pDeviceList.nDeviceNum; i++)
                {
                    MyCamera.MV_CC_DEVICE_INFO device = (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(m_pDeviceList.pDeviceInfo[i], typeof(MyCamera.MV_CC_DEVICE_INFO));
                    if (device.nTLayerType == MyCamera.MV_GIGE_DEVICE)
                    {
                        IntPtr buffer = Marshal.UnsafeAddrOfPinnedArrayElement(device.SpecialInfo.stGigEInfo, 0);
                        MyCamera.MV_GIGE_DEVICE_INFO gigeInfo = (MyCamera.MV_GIGE_DEVICE_INFO)Marshal.PtrToStructure(buffer, typeof(MyCamera.MV_GIGE_DEVICE_INFO));
                        if (gigeInfo.chUserDefinedName != "")
                        {
                            cbDeviceList.Items.Add("GigE: " + gigeInfo.chUserDefinedName + " (" + gigeInfo.chSerialNumber + ")");
                        }
                        else
                        {
                            cbDeviceList.Items.Add("GigE: " + gigeInfo.chManufacturerName + " " + gigeInfo.chModelName + " (" + gigeInfo.chSerialNumber + ")");
                        }
                    }
                    else if (device.nTLayerType == MyCamera.MV_USB_DEVICE)
                    {
                        IntPtr buffer = Marshal.UnsafeAddrOfPinnedArrayElement(device.SpecialInfo.stUsb3VInfo, 0);
                        MyCamera.MV_USB3_DEVICE_INFO usbInfo = (MyCamera.MV_USB3_DEVICE_INFO)Marshal.PtrToStructure(buffer, typeof(MyCamera.MV_USB3_DEVICE_INFO));
                        if (usbInfo.chUserDefinedName != "")
                        {
                            cbDeviceList.Items.Add("USB: " + usbInfo.chUserDefinedName + " (" + usbInfo.chSerialNumber + ")");
                        }
                        else
                        {
                            cbDeviceList.Items.Add("USB: " + usbInfo.chManufacturerName + " " + usbInfo.chModelName + " (" + usbInfo.chSerialNumber + ")");
                        }
                    }
                }

                // ch:选择第一项 | en:Select the first item
                if (m_pDeviceList.nDeviceNum != 0)
                {
                    cbDeviceList.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }

        /// <summary>
        /// 打开设备
        /// </summary>
        private void OpenDev()
        {
            try
            {
                if (m_pDeviceList.nDeviceNum == 0 || cbDeviceList.SelectedIndex == -1)
                {
                    DebOutPut.DebLog("未检索到相机！");
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "未检索到相机！");
                    cameraErrStr = "未检索到相机";
                    return;
                }
                int nRet = -1;

                // ch:获取选择的设备信息 | en:Get selected device information
                MyCamera.MV_CC_DEVICE_INFO device =
                    (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(m_pDeviceList.pDeviceInfo[cbDeviceList.SelectedIndex],
                                                                  typeof(MyCamera.MV_CC_DEVICE_INFO));

                // ch:打开设备 | en:Open device
                if (null == m_pMyCamera)
                {
                    m_pMyCamera = new MyCamera();
                    if (null == m_pMyCamera)
                    {
                        DebOutPut.DebLog("创建相机对象失败!");
                        DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "创建相机对象失败!");
                        cameraErrStr = "创建相机对象失败";
                        return;
                    }
                }

                nRet = m_pMyCamera.MV_CC_CreateDevice_NET(ref device);
                if (MyCamera.MV_OK != nRet)
                {
                    DebOutPut.DebLog("创建设备对象失败，nRet：" + nRet);
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "创建设备对象失败，nRet：" + nRet);
                    cameraErrStr = "创建设备对象失败，nRet：" + nRet;
                    m_pMyCamera = null;
                    return;
                }

                nRet = m_pMyCamera.MV_CC_OpenDevice_NET();
                if (MyCamera.MV_OK != nRet)
                {
                    m_pMyCamera.MV_CC_DestroyDevice_NET();
                    DebOutPut.DebLog("相机启动失败，nRet：" + nRet);
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "相机启动失败，nRet：" + nRet);
                    cameraErrStr = "相机启动失败，nRet：" + nRet;
                    m_pMyCamera = null;
                    return;
                }

                // ch:探测网络最佳包大小(只对GigE相机有效) | en:Detection network optimal package size(It only works for the GigE camera)
                if (device.nTLayerType == MyCamera.MV_GIGE_DEVICE)
                {
                    int nPacketSize = m_pMyCamera.MV_CC_GetOptimalPacketSize_NET();
                    if (nPacketSize > 0)
                    {
                        nRet = m_pMyCamera.MV_CC_SetIntValue_NET("GevSCPSPacketSize", (uint)nPacketSize);
                        if (nRet != MyCamera.MV_OK)
                        {
                            DebOutPut.DebLog("警告：设置数据包大小失败，nRet：" + nRet);
                            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "警告：设置数据包大小失败，nRet：" + nRet);
                        }
                    }
                    else
                    {
                        DebOutPut.DebLog("警告：获取数据包大小失败，nRet：" + nRet);
                        DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "警告：获取数据包大小失败，nRet：" + nRet);
                    }
                }

                // ch:设置采集连续模式 | en:Set Continues Aquisition Mode
                m_pMyCamera.MV_CC_SetEnumValue_NET("AcquisitionMode", 1);// ch:工作在连续模式 | en:Acquisition On Continuous Mode
                                                                         // m_pMyCamera.MV_CC_SetEnumValue_NET("TriggerMode", 0);    // ch:连续模式 | en:Continuous
                                                                         //SetParam();//设置参数

            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }

        }

        /// <summary>
        /// 设置参数
        /// </summary>
        private void SetParam()
        {
            //try
            //{
            //    if (m_pMyCamera == null)
            //    {
            //        DebOutPut.WriteLog( LogType.Normal, "m_pMyCamera:null");
            //        return;
            //    }
            //    int nRet;
            //    m_pMyCamera.MV_CC_SetEnumValue_NET("ExposureAuto", 0);
            //    float.Parse(Param.cameraExposure);//曝光
            //    float.Parse(Param.cameraGain);//增益
            //    float.Parse(Param.cameraFrameRate);//帧率
            //    nRet = m_pMyCamera.MV_CC_SetFloatValue_NET("ExposureTime", float.Parse(Param.cameraExposure));
            //    if (nRet != MyCamera.MV_OK)
            //    {
            //        DebOutPut.WriteLog( LogType.Normal, "设置曝光时间失败!");
            //    }

            //    m_pMyCamera.MV_CC_SetEnumValue_NET("GainAuto", 0);
            //    nRet = m_pMyCamera.MV_CC_SetFloatValue_NET("Gain", float.Parse(Param.cameraGain));
            //    if (nRet != MyCamera.MV_OK)
            //    {
            //        DebOutPut.WriteLog( LogType.Normal, "设置增益失败!");
            //    }

            //    nRet = m_pMyCamera.MV_CC_SetFloatValue_NET("AcquisitionFrameRate", float.Parse(Param.cameraFrameRate));
            //    if (nRet != MyCamera.MV_OK)
            //    {
            //        DebOutPut.WriteLog( LogType.Normal, "设置帧速率失败!");
            //    }
            //}
            //catch (Exception ex)
            //{
            //    DebOutPut.DebLog( ex.ToString());
            //    DebOutPut.WriteLog( LogType.Error, ex.ToString());
            //}

        }

        /// <summary>
        /// 开始采集
        /// </summary>
        private void StartCollection()
        {
            try
            {
                if (m_pMyCamera == null)
                {
                    return;
                }
                int nRet;

                // ch:开始采集 | en:Start Grabbing
                nRet = m_pMyCamera.MV_CC_StartGrabbing_NET();
                if (MyCamera.MV_OK != nRet)
                {
                    CameraClose();
                    DebOutPut.DebLog("相机取像失败，nRet：" + nRet);
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "相机取像失败，nRet：" + nRet);
                    cameraErrStr = "相机取像失败，nRet：" + nRet;
                    return;
                }


                // ch:标志位置位true | en:Set position bit true
                //m_bGrabbing = true;
                // ch:显示 | en:Display
                nRet = m_pMyCamera.MV_CC_Display_NET(Cv_Main1.Handle);

                if (MyCamera.MV_OK != nRet)
                {
                    CameraClose();
                    DebOutPut.DebLog("相机取像显示失败，nRet：" + nRet);
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "相机取像显示失败，nRet：" + nRet);
                    cameraErrStr = "相机取像显示失败，nRet：" + nRet;
                    return;
                }
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }

        }
        private void CameraClose()
        {
            if (m_pMyCamera != null)
            {
                m_pMyCamera.MV_CC_CloseDevice_NET();
                m_pMyCamera.MV_CC_DestroyDevice_NET();
                m_pMyCamera = null;
            }
        }

        void DelegateOnExposureCallback()
        {
            BeginInvoke(evexposure_);
        }
        private void OnEventImage(int[] roundrobin)
        {
            try
            {
                lock (locker_)
                {
                    if (roundrobin[0] == 1)
                        Cv_Main1.Image = bmp1_;
                    else
                        Cv_Main1.Image = bmp2_;
                    roundrobin_ = roundrobin[0];
                }
                Cv_Main1.Invalidate();
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }

        }

        private DataTable MakeNamesTable(string tablename)
        {
            try
            {
                DataTable namesTable = new DataTable();

                DataColumn idColumn = new DataColumn();
                idColumn.DataType = System.Type.GetType("System.Int32");
                idColumn.ColumnName = "ID";
                namesTable.Columns.Add(idColumn);

                DataColumn Column1 = new DataColumn();
                Column1.DataType = System.Type.GetType("System.String");
                Column1.ColumnName = "CollectTime";
                namesTable.Columns.Add(Column1);

                DataColumn Column2 = new DataColumn();
                Column2.DataType = System.Type.GetType("System.String");
                Column2.ColumnName = "path";
                namesTable.Columns.Add(Column2);

                return namesTable;
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
                return null;
            }

        }

        /// <summary>
        /// 每页展示的数量
        /// </summary>
        int showCount = 2;
        private void UpdateList()
        {
            try
            {
                RecordDt.Tables.Clear();
                collectionDispData.Clear();
                DataTable TableTemp = MakeNamesTable("Record");
                RecordDt.Tables.Add(TableTemp);

                string sql = "select * from Record order by CollectTime desc";
                DataTable dt = DB.QueryDatabase(sql).Tables[0];
                //刷新页
                UpdataPage(dt);
                imageList1.Images.Clear();
                ImgNames.Clear();
                int a = 0;
                for (int i = currentPage * showCount - showCount; i < (((dt.Rows.Count - (currentPage * showCount - showCount)) > showCount) ? currentPage * showCount : dt.Rows.Count); i++)
                {
                    a++;
                    DataRow row = TableTemp.NewRow();
                    row["ID"] = dt.Rows[i]["ID"].ToString();
                    string time = dt.Rows[i]["CollectTime"].ToString();
                    row["CollectTime"] = time;
                    string picPath = Path.Combine(Param.BasePath, "GrabImg");
                    picPath = Path.Combine(picPath, Convert.ToDateTime(time).ToString("yyyyMMddHHmmss", System.Globalization.DateTimeFormatInfo.InvariantInfo) + ".bmp");
                    row["path"] = picPath;
                    TableTemp.Rows.Add(row);

                    int id = Convert.ToInt32(dt.Rows[i]["ID"]);
                    string path = picPath;
                    if (!File.Exists(path))
                    {
                        return;
                    }
                    collectionDispData.Add(new ImageItem(id, path, time));

                    Image image = Tools.FileToBitmap(path); //获取文件
                    imageList1.Images.Add(image);
                    ImgNames.Add(time);
                    Thread.Sleep(100);
                }
                this.listView1.BeginUpdate();
                listView1.Clear();
                for (int i = 0; i < ImgNames.Count; i++)
                {
                    ListViewItem lvi = new ListViewItem();
                    lvi.Tag = collectionDispData.ElementAt(i).path;
                    lvi.ImageIndex = i;
                    lvi.Text = ImgNames[i];
                    this.listView1.Items.Add(lvi);
                }
                this.listView1.EndUpdate();

                if (pageCount > currentPage)
                {
                    nextpage.Enabled = true;
                }
                else
                {
                    nextpage.Enabled = false;
                }
                if (currentPage <= 1)
                {
                    prepage.Enabled = false;
                }
                else
                {
                    prepage.Enabled = true;
                }
                if (currentPage == 1)
                {
                    button10.Enabled = false;
                }
                else
                {
                    button10.Enabled = true;
                }
                if (currentPage == pageCount)
                {
                    button11.Enabled = false;
                }
                else
                {
                    button11.Enabled = true;
                }
                if (this.BtnLookData.Enabled == false)
                {
                    this.BtnLookData.Enabled = true;
                }
                PageInfo.Text = currentPage.ToString() + "/" + pageCount.ToString();
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }
        /// <summary>
        /// 刷新页数
        /// </summary>
        private void UpdataPage(DataTable dt)
        {
            //总条数
            recordnum = dt.Rows.Count;
            if ((recordnum % perPageCount) == 0)
            {
                pageCount = recordnum / perPageCount;
            }
            else
            {
                pageCount = recordnum / perPageCount + 1;
            }
            PageInfo.Text = ((currentPage == 0) ? 1 : currentPage) + "/" + ((pageCount == 0) ? 1 : pageCount);
        }


        /// <summary>
        /// 锁
        /// </summary>
        static readonly object SequenceLockUpdate = new object();
        private void UpdateListBoxItem()
        {
            lock (SequenceLockUpdate)
            {
                try
                {
                    Thread myThread = new Thread(new ThreadStart(UpdateList));
                    myThread.IsBackground = true;
                    myThread.Start();
                }
                catch (Exception ex)
                {
                    DebOutPut.DebLog(ex.ToString());
                    DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
                }
            }
        }

        public void _Set_LableStatus_Value(Label lb, string value)
        {

        }

        private void button9_Click(object sender, EventArgs e)
        {
            if (button9.Text == "拍照")
            {
                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "点击事件_拍照");
                Thread thread = new Thread(HandleImage);
                thread.IsBackground = true;
                thread.Start();
            }
        }

        /// <summary>
        /// 获取图片
        /// </summary>
        private Image GetPic()
        {
            //判断当前是新版相机还是旧版相机
            try
            {
                if (m_pMyCamera == null)
                {
                    DebOutPut.DebLog("相机对象为空");
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "相机对象为空");
                    return null;
                }
                int nRet;
                UInt32 nPayloadSize = 0;
                MyCamera.MVCC_INTVALUE stParam = new MyCamera.MVCC_INTVALUE();
                nRet = m_pMyCamera.MV_CC_GetIntValue_NET("PayloadSize", ref stParam);
                if (MyCamera.MV_OK != nRet)
                {
                    DebOutPut.DebLog("获取PayloadSize失败，nRet：" + nRet);
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "获取PayloadSize失败，nRet：" + nRet);
                    return null;
                }
                nPayloadSize = stParam.nCurValue;
                if (nPayloadSize > m_nBufSizeForDriver)
                {
                    m_nBufSizeForDriver = nPayloadSize;
                    m_pBufForDriver = new byte[m_nBufSizeForDriver];

                    // ch:同时对保存图像的缓存做大小判断处理 | en:Determine the buffer size to save image
                    // ch:BMP图片大小：width * height * 3 + 2048(预留BMP头大小) | en:BMP image size: width * height * 3 + 2048 (Reserved for BMP header)
                    m_nBufSizeForSaveImage = m_nBufSizeForDriver * 3 + 2048;
                    m_pBufForSaveImage = new byte[m_nBufSizeForSaveImage];
                }

                IntPtr pData = Marshal.UnsafeAddrOfPinnedArrayElement(m_pBufForDriver, 0);
                MyCamera.MV_FRAME_OUT_INFO_EX stFrameInfo = new MyCamera.MV_FRAME_OUT_INFO_EX();

                // ch:超时获取一帧，超时时间为1秒 | en:Get one frame timeout, timeout is 1 sec
                nRet = m_pMyCamera.MV_CC_GetOneFrameTimeout_NET(pData, m_nBufSizeForDriver, ref stFrameInfo, 1000);
                if (MyCamera.MV_OK != nRet)
                {
                    DebOutPut.DebLog("无数据，nRet：" + nRet);
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "无数据，nRet：" + nRet);
                    return null;
                }

                IntPtr pImage = Marshal.UnsafeAddrOfPinnedArrayElement(m_pBufForSaveImage, 0);

                MyCamera.MV_SAVE_IMAGE_PARAM_EX stSaveParam = new MyCamera.MV_SAVE_IMAGE_PARAM_EX();
                stSaveParam.enImageType = MyCamera.MV_SAVE_IAMGE_TYPE.MV_Image_Jpeg;
                stSaveParam.enPixelType = stFrameInfo.enPixelType;
                stSaveParam.pData = pData;
                stSaveParam.nDataLen = stFrameInfo.nFrameLen;
                stSaveParam.nHeight = stFrameInfo.nHeight;
                stSaveParam.nWidth = stFrameInfo.nWidth;
                stSaveParam.pImageBuffer = pImage;
                stSaveParam.nBufferSize = m_nBufSizeForSaveImage;
                stSaveParam.nJpgQuality = 80;
                nRet = m_pMyCamera.MV_CC_SaveImageEx_NET(ref stSaveParam);
                if (MyCamera.MV_OK != nRet)
                {
                    DebOutPut.DebLog("保存失败，nRet：" + nRet);
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "保存失败，nRet：" + nRet);
                    return null;
                }
                MemoryStream ms = new MemoryStream(m_pBufForSaveImage);
                Image realTimeImage = Bitmap.FromStream(ms, true);
                return realTimeImage;
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, "保存失败!  " + ex.ToString());
                return null;
            }

        }
        /// <summary>
        /// 锁
        /// </summary>
        static readonly object SequenceLock1 = new object();
        //处理图片
        //取像——增加水印——保存——上传
        private void HandleImage()
        {
            lock (SequenceLock1)
            {
                button9.Text = "拍照中...";
                int GrabTimes = 0;
                Image realTimeImage = null;
                try
                {
                    while (GrabTimes != 4)
                    {
                        //判断相机
                        if (Param.cameraVersion == "1")
                        {
                            realTimeImage = OldGetPic();
                        }
                        else if (Param.cameraVersion == "2")
                        {
                            realTimeImage = GetPic();
                        }
                        if (realTimeImage == null)
                        {
                            DebOutPut.DebLog("拍照失败！");
                            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "拍照失败！");
                            button9.Text = "拍照";
                            return;
                        }
                        string sql = "", collectTime = "";
                        DateTime dt = DateTime.Now;//采集时间
                        Bitmap img = new Bitmap(realTimeImage);

                        //图片添加水印
                        if (img != null)
                        {
                            if (Param.version == "2")//带光控雨控
                            {
                                using (Graphics g = Graphics.FromImage(img))
                                {
                                    collectTime = dt.ToString(Param.dataType, System.Globalization.DateTimeFormatInfo.InvariantInfo);
                                    string devNum = "ID:" + Param.DeviceID;
                                    string time = "时间:" + collectTime;
                                    string wendu = (this.lb_wd.Text.Trim() == "温度" || this.lb_wd.Text.Trim() == "无数据") ? "温度:---" : this.lb_wd.Text.Trim();
                                    string shidu = (this.lb_sd.Text.Trim() == "湿度" || this.lb_sd.Text.Trim() == "无数据") ? "湿度:---" : this.lb_sd.Text.Trim();
                                    string guangzhao = (this.lb_gz.Text.Trim() == "光照" || this.lb_gz.Text.Trim() == "无数据") ? "光照:---" : this.lb_gz.Text.Trim();
                                    string yukong = (this.lb_yk.Text.Trim() == "雨控" || this.lb_yk.Text.Trim() == "无数据") ? "雨控:---" : this.lb_yk.Text.Trim();
                                    string hj = wendu + "   " + shidu + "   " + guangzhao + "   " + yukong;
                                    string context = devNum + "   " + time + "   " + hj;
                                    g.DrawString(context, new Font("仿宋_GB2312", 40, FontStyle.Bold), System.Drawing.Brushes.Yellow, new PointF(50, 10));
                                }
                            }
                            else if (Param.version == "1")//不带光控雨控
                            {
                                using (Graphics g = Graphics.FromImage(img))
                                {
                                    collectTime = dt.ToString(Param.dataType, System.Globalization.DateTimeFormatInfo.InvariantInfo);
                                    string devNum = "ID:" + Param.DeviceID;
                                    string time = "时间:" + collectTime;
                                    string context = devNum + "   " + time;
                                    g.DrawString(context, new Font("仿宋_GB2312", 40, FontStyle.Bold), System.Drawing.Brushes.Yellow, new PointF(50, 10));
                                    //g.DrawString(collectTime, new Font("宋体", 100), System.Drawing.Brushes.Yellow, new PointF(2500, 100));
                                    //g.DrawString(Param.DeviceID, new Font("宋体", 100), System.Drawing.Brushes.Yellow, new PointF(100, 100));
                                }
                            }
                            //保存图片
                            if (Param.SaveImage(img, dt.ToString("yyyyMMddHHmmss", System.Globalization.DateTimeFormatInfo.InvariantInfo) + ".bmp"))
                            {
                                img.Dispose();
                                sql = "insert into Record (Flag,CollectTime) values ('0','" + collectTime + "')";
                                if (DB.updateDatabase(sql) != 1)
                                {
                                    DebOutPut.DebLog("图像采集时间为：" + collectTime + "  插入数据库失败");
                                }
                                else
                                {
                                    DebOutPut.DebLog("图像采集时间为：" + collectTime + "  插入数据库成功");
                                }
                            }
                            Thread thread = new Thread(ManualImageSend);
                            thread.IsBackground = true;
                            thread.Start();
                            break;
                        }
                        else
                        {
                            GrabTimes++;
                            DebOutPut.DebLog("手动拍照失败——img为空,重试次数：" + GrabTimes.ToString());
                            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "手动拍照失败——img为空,重试次数：" + GrabTimes.ToString());
                            Thread.Sleep(2000);
                        }
                        Thread.Sleep(100);
                    }
                    button9.Text = "拍照";
                }
                catch (Exception ex)
                {
                    DebOutPut.DebLog(ex.ToString());
                    DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
                    button9.Text = "拍照";
                }
                realTimeImage = null;
            }
        }

        static readonly object SequenceLock = new object();
        /// <summary>
        /// 图像打水印、入库、发送
        /// </summary>
        private void ManualImageSend()
        {
            lock (SequenceLock)
            {
                if (Param.isContinuousUpload == "0")
                {
                    DebOutPut.DebLog("拍照上传");
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "拍照上传");
                    SendCollectionData();
                }
                ImageClear();
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            try
            {
                if (txt_upStep.Text != "")
                    MoveUporMove(true, txt_upStep.Text);
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }

        }

        /// <summary>
        /// 微调载物台（相机）
        /// </summary>
        /// <param name="bdir">true:顺时针,向下，命令0x31； false:逆时针，向上，命令0x32</param>
        /// <param name="step">本次移动步数</param>
        private void MoveUporMove(bool bdir, String step)
        {
            try
            {
                byte dir = (byte)(bdir ? 0x31 : 0x32);
                int nstep = Convert.ToInt32(step);
                Cmd.CommunicateDp(dir, nstep);

            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }

        }

        /// <summary>
        /// 多步微调载物台（相机）
        /// </summary>
        /// <param name="bdir">true:顺时针,向下，命令0x31； false:逆时针，向上，命令0x32</param>
        /// <param name="step">共移动步数</param>
        /// <param name="currStep">一次移动步数</param>
        private void MoveUporMove(bool bdir, string step, string currStep)
        {
            try
            {
                int nstep = Convert.ToInt32(step);
                int ncurrStep = Convert.ToInt32(currStep);
                byte dir = (byte)(bdir ? 0x31 : 0x32);
                for (int i = ncurrStep; i < nstep + ncurrStep; i += ncurrStep)
                {
                    Cmd.CommunicateDp(dir, ncurrStep);
                    Thread.Sleep(400);
                }

            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            try
            {
                if (txt_downStep.Text != "")
                    MoveUporMove(false, txt_downStep.Text);
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }

        }
        private bool getAutoDevStatus(List<byte> bits)
        {
            try
            {
                devstatus.clear();
                StringBuilder sb = new StringBuilder();
                string[] yes = new string[] { "轴1 原点位置", "轴1 粘附液位置", "轴1 吸孢子位置", "轴1： 培养液位置", "轴1：拍照位置", "轴2:原点位置", "轴2:已推片位置", "轴3：接片位置", "轴3：原点位置", "轴4：原点位置", "轴4：推完片位置" };
                string[] no = new string[] { "轴1 未到原点位置", "轴1 未到粘附液位置", "轴1 未到吸孢子位置", "轴1： 未到培养液位置", "轴1：未到拍照位置", "轴2:未到原点位置", "轴2:未到已推片位置", "轴3：未到接片位置", "轴3：未到原点位置", "轴4：未到原点位置", "轴4：未到推完片位置" };

                byte[] ret = Cmd.CommunicateDp(0xA0, 0);
                if (ret == null || ret[0] != 0xFF)
                {
                    return false;
                }
                // ret[7]=0x1A;
                //ret[6]=0xA1;
                int dirs = (ret[7] << 8) | ret[6];//可用于指示15个位当前状态

                foreach (byte i in bits)
                {

                    if (((dirs >> i) & 0x01) == 1)
                    {
                        //  sb.Append(yes[i]);
                        //i值介绍：
                        //0：x1   1：x2   2：x3    3：x4，依次类推
                        devstatus.bits[i] = 1;
                        //   ct[i].BackColor = Color.Green;
                    }
                    else
                    {
                        // sb.Append(no[i]);

                        devstatus.bits[i] = 0;
                        // ct[i].BackColor = Color.Yellow;
                    }
                }

                devstatus.status = sb.ToString();
                devstatus.isReady(bits);

                int count = 0;
                if (((dirs >> 13) & 0x01) == 1)
                {
                    label21.Text = "存在";
                    label21.ForeColor = System.Drawing.Color.Black;
                    count++;
                }
                else
                {
                    label21.Text = "不存在";
                    label21.ForeColor = System.Drawing.Color.Red;

                }
                //无培养液
                if (((dirs >> 12) & 0x01) == 0)
                {
                    label20.Text = "培养液缺液";
                    label20.ForeColor = System.Drawing.Color.Red;
                }
                else
                {

                    label20.Text = "正常";
                    label20.ForeColor = System.Drawing.Color.Black;
                    count++;
                }
                //无粘附液
                if (Param.DripDevice == "0")
                {
                    if (((dirs >> 11) & 0x01) == 0)
                    {
                        label19.Text = "粘附液缺液";
                        label19.ForeColor = System.Drawing.Color.Red;
                    }
                    else
                    {
                        label19.Text = "正常";
                        label19.ForeColor = System.Drawing.Color.Black;
                        count++;
                    }

                }
                else if (Param.DripDevice == "1")
                {
                    string stuta = PushingFluidRead();
                    if (stuta == "01")
                    {
                        label19.Text = "粘附液缺液";
                        label19.ForeColor = System.Drawing.Color.Red;

                    }
                    else if (stuta == "00")
                    {
                        label19.Text = "正常";
                        label19.ForeColor = System.Drawing.Color.Black;
                        count++;
                    }
                }
                //雨
                if (((dirs >> 14) & 0x01) == 0)
                {
                    lb_yk.Text = "无雨";
                }
                else
                {
                    lb_yk.Text = "有雨";
                }
                if (count != 3)
                {
                    string str = "";
                    if (label21.Text.Trim() == "不存在")
                    {
                        str += "载玻片" + label21.Text.Trim();
                    }
                    if (label20.Text.Trim() == "培养液缺液")
                    {
                        if (str != "")
                        {
                            str += ";" + label20.Text.Trim();
                        }
                        else if (str == "")
                        {
                            str += label20.Text.Trim();
                        }
                    }
                    if (label19.Text.Trim() == "粘附液缺液")
                    {
                        if (str != "")
                        {
                            str += ";" + label19.Text.Trim();
                        }
                        else if (str == "")
                        {
                            str += label19.Text.Trim();
                        }
                    }
                    if (str == "")
                        statusInfo = "正常";
                    else
                        statusInfo = str;
                }
                else
                {
                    statusInfo = "正常";
                }
                return true;
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
                return false;
            }

        }

        private bool isReady()
        {
            try
            {
                if (getAutoDevStatus(list))
                {
                    if (!devstatus.bReady)
                        return false;
                    else
                        return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
                return false;
            }

        }

        /*
         * 减少载玻片仓载玻片数量
         */
        private void SubtractPian()
        {
            try
            {

                int remain = int.Parse(Param.remain) - 1;//载玻片数量
                if (remain < 0)
                {
                    remain = 0;
                }
                Param.Set_ConfigParm(configfileName, "Config", "remain", remain.ToString());
                Param.remain = Param.Read_ConfigParam("Config.ini", "Config", "remain");
                this.TxtRemain.Text = ((int.Parse(Param.remain) < 0) ? 0 : int.Parse(Param.remain)).ToString();
                int currRunMode = this.TxtRunMode.SelectedIndex;//当前工作模式
                tcpclient.SendSlideGlassCount(134, "", int.Parse(Param.remain), currRunMode, (float)wd);
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }

        /// <summary>
        /// 设备运行
        /// 正常流程是：0 1 2 3 4 5 6 7 8 9 10 11 14 12 13
        /// </summary>
        private void timer1_Elapsed(object sender, EventArgs e)
        {
            try
            {
                if (Interlocked.Exchange(ref inTimer1, 1) == 0)
                {
                    if (serialPort1 == null || !serialPort1.IsOpen)
                    {
                        statusInfo = "主控串口异常";
                        label18.Text = "主控串口异常，请处理！";
                        label18.ForeColor = System.Drawing.Color.Red;
                        DebOutPut.DebLog("主控串口异常，设备停止运行！");
                        DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "主控串口异常，设备停止运行！");
                        Timer1Stop();
                        return;
                    }
                    if (bstop)
                    {
                        DebOutPut.DebLog("设备停止运行！");
                        DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "设备停止运行！");
                        Timer1Stop();
                        return;
                    }
                    string str = "";
                    for (int i = 0; i < list.Count; i++)
                    {
                        str += "X" + (int.Parse(list[i].ToString()) + 1).ToString() + "  ";
                    }
                    DebOutPut.DebLog("开始查询状态位，当前查询状态位：" + str);
                    if (!isReady())
                    {
                        string str1 = "";
                        for (int i = 0; i < list.Count; i++)
                        {
                            if (devstatus.bits[list[i]] == 0)
                            {
                                str1 += "X" + (list[i] + 1) + "  ";
                            }
                        }
                        DebOutPut.DebLog(str1 + "状态未激活！");
                        Interlocked.Exchange(ref inTimer1, 0);
                        return;
                    }
                    DebOutPut.DebLog("状态查询完成，所查询状态位正常！");
                    Timer2Stop();
                    Timer1Stop();//查询状态成功，准备就绪
                    DebOutPut.DebLog("当前开始执行：" + bStep.ToString() + " 运动");
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "流程事件_当前开始执行：" + bStep.ToString() + " 运动");
                    switch (bStep)
                    {
                        case 0: //已经到达原点位置，
                            DetectMemory();
                            setLocation(0);
                            tcpclient.SendCurrAction(142, "", "原点");
                            inituipian();
                            break;
                        case 1://已经到达推片位置，推片就绪
                            setLocation(2);
                            tcpclient.SendCurrAction(142, "", "推片");
                            tuipian();
                            break;
                        case 2://推片已经完成，可以复位了
                            tuipianreset();
                            SubtractPian();
                            break;
                        case 3://推片到粘附液位置
                            tuifanshilin();
                            break;
                        case 4://滴加粘附液
                            setLocation(4);
                            tcpclient.SendCurrAction(142, "", "粘附液");
                            jiafanshilin();
                            break;
                        case 5://推片到风机
                            tuipianFengshan();
                            break;
                        case 6:  //打开风机
                            setLocation(6);
                            tcpclient.SendCurrAction(142, "", "收集");
                            openFengji();
                            break;
                        case 7://关闭风机
                            closeFengji();
                            break;
                        case 8://推片到培养液位置
                            tuiPeiyangye();
                            break;
                        case 9://滴加培养液
                            setLocation(8);
                            tcpclient.SendCurrAction(142, "", "培养液");
                            peiyang();
                            break;
                        case 10://推片到拍照位置
                            setLocation(10);
                            tcpclient.SendCurrAction(142, "", "拍照");
                            tuipaizhao();
                            break;
                        case 11://自动聚焦拍照
                            //Timer2Stop();
                            Thread myThread = new Thread(new ThreadStart(getImageOnLine));
                            myThread.IsBackground = true;
                            myThread.Start();
                            break;
                        case 12://回归原点
                            setLocation(12);
                            tcpclient.SendCurrAction(142, "", "回收");
                            finish();
                            break;
                        case 13://开始新的流程
                            setLocation(0);
                            //Timer2Stop();
                            if (autoFlag)//自动运行
                            {
                                DebOutPut.DebLog("自动运行");
                                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "自动运行");
                                doLiucheng();
                            }
                            else//非自动运行
                            {
                                DebOutPut.DebLog("非自动运行，是否是正常工作模式（0正常、1调试）：runmode：" + runmode);
                                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "非自动运行，是否是正常工作模式（0正常、1调试）：runmode：" + runmode);
                                //非调试模式
                                if (runmode == 0) //正常流程下结束后开始下个流程
                                {
                                    timer5.Start();
                                    DebOutPut.DebLog("Timer5_Start");
                                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "Timer5_Start");
                                }
                                else if (runmode == 1 && isSingleProcess)//当前为非自动运行，且为调试模式  为单流程运行模式 单流程运行完本流程中会后使参数回归正常
                                {
                                    DebOutPut.DebLog("单流程运行结束");
                                    button3.Text = "单流程运行";
                                    label26.Text = "当前工作模式:" + "调试模式";
                                    isSingleProcess = false;
                                    Param.Init_Param(configfileName);//重新读取参数
                                }
                            }
                            break;
                        case 14://回收片
                            huishoupian();
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }

        /// <summary>
        /// 检测内存
        /// </summary>
        private void DetectMemory()
        {
            try
            {
                float remainingDiskSpace = GetHardDiskSpace(Param.BasePath.Substring(0, Param.BasePath.IndexOf(':')));
                DebOutPut.DebLog("程序运行磁盘剩余空间：" + remainingDiskSpace + " GB");
                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "程序运行磁盘剩余空间：" + remainingDiskSpace + " GB");
                if (remainingDiskSpace < 1f)
                {
                    string sql = "Delete * FROM Record";
                    int ret = DB.updateDatabase(sql);
                    if (ret == -1)
                    {
                        DebOutPut.DebLog("程序运行磁盘剩余空间不足，清空数据失败！");
                        DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "程序运行磁盘剩余空间不足，清空数据失败！");
                    }
                    else
                    {
                        string path = Param.BasePath + "\\GrabImg";
                        DeleteFolder(path);
                        float remainingDiskSpace_ = GetHardDiskSpace(Param.BasePath.Substring(0, Param.BasePath.IndexOf(':')));
                        DebOutPut.DebLog("程序运行磁盘剩余空间不足，清空数据成功！目前剩余存储空间：" + remainingDiskSpace_.ToString("F4") + " GB");
                        DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "程序运行磁盘剩余空间不足，清空数据成功！目前剩余存储空间：" + remainingDiskSpace_.ToString("F4") + " GB");
                    }
                }
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }

        /// <summary>
        /// 获取程序运行所在磁盘，可使用存储空间，单位GB
        /// </summary>
        /// <param name="str_HardDiskName"></param>
        /// <returns></returns>
        public float GetHardDiskSpace(string str_HardDiskName)
        {
            try
            {
                float totalSize = 0;
                str_HardDiskName = str_HardDiskName + ":\\";
                System.IO.DriveInfo[] drives = System.IO.DriveInfo.GetDrives();
                foreach (System.IO.DriveInfo drive in drives)
                    if (drive.Name == str_HardDiskName)
                        totalSize = (float)drive.TotalFreeSpace / 1073741824;
                return totalSize;
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
                return -1;
            }
        }

        /// <summary>
        /// 删除文件夹
        /// </summary>
        /// <param name="dir"></param>
        public void DeleteFolder(string dir)
        {
            try
            {
                foreach (string d in Directory.GetFileSystemEntries(dir))
                {
                    if (File.Exists(d))
                    {
                        FileInfo fi = new FileInfo(d);
                        if (fi.Attributes.ToString().IndexOf("ReadOnly") != -1)
                            fi.Attributes = FileAttributes.Normal;
                        File.Delete(d);//直接删除其中的文件   
                    }
                    else
                        DeleteFolder(d);//递归删除子文件夹   
                }
                Directory.Delete(dir);//删除已空文件夹   
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }

        private void focusPhoto()
        {
            MoveUporMove(true, "380");
            //HandleImage()
        }

        int nstep = 2;//轴三上下对焦拍照每次移动步数
        /*机器正常运行时的聚焦拍照*/
        private void getImageOnLine()
        {
            try
            {
                if (!StartOldNewCamera())
                {
                    button12.Text = "自动对焦";
                    startTimer1Time = DateTime.Now;
                    timer1.Start();
                    return;
                }
                GC.Collect();
                GC.WaitForPendingFinalizers();
                CameraPositionCorrection();//镜头位置校正
                if (pc9IsStop)
                {
                    button12.Text = "自动对焦";
                    return;
                }
                Axis3ToX9();
                int minstep = Convert.ToInt16(Param.MinSteps);
                int maxstep = Convert.ToInt16(Param.MaxSteps);
                int count1 = 0;
                int count2 = 0;
                int count3 = 0;
                count1 = (maxstep - minstep) / nstep;//原位共拍摄张数
                if (int.Parse(Param.YJustRange) != 0 || int.Parse(Param.YNegaRange) != 0)
                    count2 = ((int.Parse(Param.YJustRange) + int.Parse(Param.YNegaRange)) / int.Parse(Param.YInterval)) * ((int.Parse(Param.YJustCom) + int.Parse(Param.YNageCom)) / nstep);
                if (int.Parse(Param.rightMaxSteps) != 0 || int.Parse(Param.leftMaxSteps) != 0)
                    count3 = ((int.Parse(Param.rightMaxSteps) + int.Parse(Param.leftMaxSteps)) / int.Parse(Param.moveInterval)) * ((int.Parse(Param.tranStepsMin) + int.Parse(Param.tranStepsMax)) / nstep);
                countdown = count1 + count2 + count3;

                DebOutPut.DebLog("本次对焦共需拍摄:" + countdown + " 张");
                if (minstep != 0)
                {
                    DebOutPut.DebLog("轴三移动到起始位置");
                    MoveUporMove(true, minstep.ToString(), nstep.ToString());//一次移动2步，移动到最小步数处
                    Thread.Sleep(15000);
                }

                List<string> clearPaths = new List<string>();//最清晰照片保存清晰照片的图像列表
                Dictionary<string, double> dicImageAnalysisResult = new Dictionary<string, double>();//保存照片路径和照片中包围性状总个数
                Dictionary<string, double> dicImageLenght = new Dictionary<string, double>();//当包围性状总个数为0时，保存当前照片路径和照片长度
                Dictionary<string, int> dicImageStep = new Dictionary<string, int>();//保存当前照片路径和当前拍摄的照片相机所走步数

                int count = 0;//当前拍摄的是第几张
                int optimumStep = 0;//原位对焦最佳拍照位置
                int optimumStepY = 0;//纵向对焦最佳拍照位置
                int optimumStepX = 0;//横向对焦最佳拍照位置
                int focusingCount = maxstep - minstep;
                for (int i = nstep; i < focusingCount + nstep; i += nstep)
                {
                    count++;
                    int shengyuCount = countdown--;
                    if (shengyuCount < 0)
                        shengyuCount = 0;
                    label30.Text = "拍照剩余对焦:\r\n" + shengyuCount + " 次";
                    if (!StartOldNewCamera())
                    {
                        Thread.Sleep(2000);
                        continue;
                    }
                    DebOutPut.DebLog("拍照剩余对焦:" + shengyuCount + " 次");
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "拍照剩余对焦:" + shengyuCount + " 次");
                    int imageStep = i + int.Parse(Param.MinSteps);
                    string path = PhotographAnalysis(dicImageAnalysisResult, dicImageLenght, dicImageStep, imageStep, nstep.ToString());
                    if (string.IsNullOrEmpty(path))
                    {
                        continue;
                    }
                    Console.WriteLine("当前对焦位置：" + i);
                    if (isCrackSporeAnalysis)
                    {
                        //精准对焦启动分析
                        CrackSporeAnalysis(path, clearPaths, i);
                    }
                }
                SelectionOfMaps(dicImageAnalysisResult, dicImageLenght, dicImageStep, clearPaths, ref optimumStep, int.Parse(Param.clearCount));
                DebOutPut.DebLog("原位对焦最佳位置 " + optimumStep);
                CameraGUIwei();
                //Y正范围和Y负范围其中一个不为0时
                if (int.Parse(Param.YJustRange) != 0 || int.Parse(Param.YNegaRange) != 0)
                {
                    optimumStepY = optimumStep;
                    DebOutPut.DebLog("首次纵向对焦最佳位置 " + optimumStepY);
                    count = 0;
                    CameraPositionCorrection();
                    if (pc9IsStop)
                    {
                        button12.Text = "自动对焦";
                        return;
                    }
                    DebOutPut.DebLog("镜头电机移动使镜头在纵向正距位置");
                    CameraRightLeftMove(false, Param.YJustRange);
                    Thread.Sleep(15000);
                    DebOutPut.DebLog("镜头电机移动使镜头向纵向负距处移动并拍照");
                    List<string> clearPaths_ = new List<string>();//最清晰照片保存清晰照片的图像列表

                    Dictionary<string, double> dicImageAnalysisResult_ = new Dictionary<string, double>();//保存照片路径和照片中包围性状总个数（缓存）
                    Dictionary<string, double> dicImageLenght_ = new Dictionary<string, double>();//当包围性状总个数为0时，保存当前照片路径和照片长度（缓存）

                    Dictionary<string, double> dicImageAnalysisResult__ = new Dictionary<string, double>();//保存照片路径和照片中包围性状总个数（最终）
                    Dictionary<string, double> dicImageLenght__ = new Dictionary<string, double>();//当包围性状总个数为0时，保存当前照片路径和照片长度（最终）

                    focusingCount = int.Parse(Param.YJustRange) + int.Parse(Param.YNegaRange);
                    for (int i = int.Parse(Param.YInterval); i < focusingCount + int.Parse(Param.YInterval); i += int.Parse(Param.YInterval))
                    {
                        dicImageAnalysisResult.Clear();//保存照片路径和照片中包围性状总个数
                        dicImageLenght.Clear();//当包围性状总个数为0时，保存当前照片路径和照片长度
                        count++;
                        DebOutPut.DebLog("当前第：" + count.ToString() + " 次移动镜头电机拍摄");
                        int yJustCom = optimumStepY - int.Parse(Param.YJustCom);//轴七电机移动拍照自动对焦起始位置
                        if (yJustCom < 0)
                            yJustCom = 0;
                        int yNageCom = optimumStepY + int.Parse(Param.YNageCom);//轴七电机移动拍照自动对焦终止位置
                        DebOutPut.DebLog("本次纵向对焦最佳位置 " + optimumStepY);
                        DebOutPut.DebLog("纵向正补后起始位置 " + yJustCom);
                        DebOutPut.DebLog("纵向负补后终止位置 " + yNageCom);
                        CameraRightLeftMove(true, Param.YInterval);//轴七移动
                        Thread.Sleep(15000);
                        DebOutPut.DebLog("开始第：" + count.ToString() + " 次补偿对焦");
                        Axis3ToX9();
                        DebOutPut.DebLog("轴3移动到纵向正补后起始位置：" + yJustCom);
                        MoveUporMove(true, yJustCom.ToString(), nstep.ToString());//移动到最佳拍照位置
                        Thread.Sleep(15000);
                        int count_ = 0;
                        int focusingCount_ = yNageCom - yJustCom;
                        for (int j = nstep; j < focusingCount_ + nstep; j += nstep)
                        {
                            count_++;
                            int shengyuCount = countdown--;
                            if (shengyuCount < 0)
                                shengyuCount = 0;
                            label30.Text = "拍照剩余对焦:\r\n" + shengyuCount + " 次";
                            if (!StartOldNewCamera())
                            {
                                Thread.Sleep(2000);
                                continue;
                            }
                            DebOutPut.DebLog("拍照剩余对焦:" + shengyuCount + " 次");
                            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "拍照剩余对焦:" + shengyuCount + " 次");
                            int imageStep = j + yJustCom;
                            //拍照分析
                            string path = PhotographAnalysis(dicImageAnalysisResult, dicImageLenght, dicImageStep, imageStep, nstep.ToString());
                            if (string.IsNullOrEmpty(path))
                            {
                                continue;
                            }
                            Console.WriteLine("当前对焦位置：" + j);
                            //精准对焦启动分析
                            if (isCrackSporeAnalysis)
                            {
                                CrackSporeAnalysis(path, clearPaths, j);
                            }
                        }
                        //选图 找最佳位置  
                        SelectionOfMaps(dicImageAnalysisResult, dicImageLenght, dicImageStep, clearPaths_, ref optimumStepY, int.Parse(Param.YFirst));
                        //数据缓存
                        foreach (string item in dicImageAnalysisResult.Keys)
                        {
                            dicImageAnalysisResult_.Add(item, dicImageAnalysisResult[item]);
                        }
                        foreach (string item in dicImageLenght.Keys)
                        {
                            dicImageLenght_.Add(item, dicImageLenght[item]);
                        }
                    }
                    //开始横向选图
                    //找到正向补偿、负向补偿 每次移动对焦选出来的所有图的数据信息
                    for (int i = 0; i < clearPaths_.Count; i++)
                    {
                        if (dicImageAnalysisResult_.ContainsKey(clearPaths_[i]))
                            dicImageAnalysisResult__.Add(clearPaths_[i], dicImageAnalysisResult_[clearPaths_[i]]);
                        if (dicImageLenght_.ContainsKey(clearPaths_[i]))
                            dicImageLenght__.Add(clearPaths_[i], dicImageLenght_[clearPaths_[i]]);
                    }
                    //选图 不找最佳位置
                    SelectionOfMaps(dicImageAnalysisResult__, dicImageLenght__, null, clearPaths, ref optimumStepY, int.Parse(Param.YCheck));
                    CameraGUIwei();
                }
                //轴一电机左右移动最大步数其中一个不为0时
                if (int.Parse(Param.rightMaxSteps) != 0 || int.Parse(Param.leftMaxSteps) != 0)
                {
                    optimumStepX = optimumStep;
                    DebOutPut.DebLog("首次横向对焦最佳位置 " + optimumStepX);
                    count = 0;
                    CameraPositionCorrection();
                    if (pc9IsStop)
                    {
                        button12.Text = "自动对焦";
                        return;
                    }
                    if (Param.rightMaxSteps != "0")
                    {
                        DebOutPut.DebLog("轴一向横向负距处移动");
                        Cmd.CommunicateDp(0x12, int.Parse(Param.rightMaxSteps));
                        Thread.Sleep(15000);
                    }

                    DebOutPut.DebLog("轴一向横向正距处移动并拍照");
                    List<string> clearPaths_ = new List<string>();//最清晰照片保存清晰照片的图像列表

                    Dictionary<string, double> dicImageAnalysisResult_ = new Dictionary<string, double>();//保存照片路径和照片中包围性状总个数（缓存）
                    Dictionary<string, double> dicImageLenght_ = new Dictionary<string, double>();//当包围性状总个数为0时，保存当前照片路径和照片长度（缓存）

                    Dictionary<string, double> dicImageAnalysisResult__ = new Dictionary<string, double>();//保存照片路径和照片中包围性状总个数（最终）
                    Dictionary<string, double> dicImageLenght__ = new Dictionary<string, double>();//当包围性状总个数为0时，保存当前照片路径和照片长度（最终）

                    focusingCount = int.Parse(Param.rightMaxSteps) + int.Parse(Param.leftMaxSteps);
                    for (int i = int.Parse(Param.moveInterval); i < focusingCount + int.Parse(Param.moveInterval); i += int.Parse(Param.moveInterval))
                    {
                        dicImageAnalysisResult.Clear();//保存照片路径和照片中包围性状总个数
                        dicImageLenght.Clear();//当包围性状总个数为0时，保存当前照片路径和照片长度
                        count++;
                        DebOutPut.DebLog("当前第：" + count.ToString() + " 次移动轴一拍摄");
                        int tranStepsMin = optimumStepX - int.Parse(Param.tranStepsMin);//轴一电机移动拍照自动对焦起始位置
                        if (tranStepsMin < 0)
                            tranStepsMin = 0;
                        int tranStepsMax = optimumStepX + int.Parse(Param.tranStepsMax);//轴一电机移动拍照自动对焦终止位置
                        DebOutPut.DebLog("本次横向对焦最佳位置 " + optimumStepX);
                        DebOutPut.DebLog("横向正补后起始位置 " + tranStepsMin);
                        DebOutPut.DebLog("横向负补后终止位置 " + tranStepsMax);
                        Cmd.CommunicateDp(0x11, int.Parse(Param.moveInterval));//轴一移动
                        Thread.Sleep(5000);
                        DebOutPut.DebLog("开始第：" + count.ToString() + " 次补偿对焦");
                        Axis3ToX9();
                        DebOutPut.DebLog("轴3移动到横向正补后起始位置：" + tranStepsMin);
                        MoveUporMove(true, tranStepsMin.ToString(), nstep.ToString());//移动到最佳拍照位置
                        Thread.Sleep(15000);
                        int count_ = 0;
                        int focusingCount_ = tranStepsMax - tranStepsMin;
                        for (int j = nstep; j < focusingCount_ + nstep; j += nstep)
                        {
                            count_++;
                            int shengyuCount = countdown--;
                            if (shengyuCount < 0)
                                shengyuCount = 0;
                            label30.Text = "拍照剩余对焦:\r\n" + shengyuCount + " 次";
                            if (!StartOldNewCamera())
                            {
                                Thread.Sleep(2000);
                                continue;
                            }
                            DebOutPut.DebLog("拍照剩余对焦:" + shengyuCount + " 次");
                            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "拍照剩余对焦:" + shengyuCount + " 次");
                            int imageStep = j + tranStepsMin;
                            //拍照分析
                            string path = PhotographAnalysis(dicImageAnalysisResult, dicImageLenght, dicImageStep, imageStep, nstep.ToString());
                            if (string.IsNullOrEmpty(path))
                            {
                                continue;
                            }
                            Console.WriteLine("当前对焦位置：" + j);
                            //精准对焦启动分析
                            if (isCrackSporeAnalysis)
                            {
                                CrackSporeAnalysis(path, clearPaths, j);
                            }
                        }
                        //选图 找最佳位置  
                        SelectionOfMaps(dicImageAnalysisResult, dicImageLenght, dicImageStep, clearPaths_, ref optimumStepX, int.Parse(Param.tranClearCount));
                        //数据缓存
                        foreach (string item in dicImageAnalysisResult.Keys)
                        {
                            dicImageAnalysisResult_.Add(item, dicImageAnalysisResult[item]);
                        }
                        foreach (string item in dicImageLenght.Keys)
                        {
                            dicImageLenght_.Add(item, dicImageLenght[item]);
                        }
                    }
                    //开始横向选图
                    //找到正向补偿、负向补偿 每次移动对焦选出来的所有图的数据信息
                    for (int i = 0; i < clearPaths_.Count; i++)
                    {
                        if (dicImageAnalysisResult_.ContainsKey(clearPaths_[i]))
                            dicImageAnalysisResult__.Add(clearPaths_[i], dicImageAnalysisResult_[clearPaths_[i]]);
                        if (dicImageLenght_.ContainsKey(clearPaths_[i]))
                            dicImageLenght__.Add(clearPaths_[i], dicImageLenght_[clearPaths_[i]]);
                    }
                    //选图 不找最佳位置
                    SelectionOfMaps(dicImageAnalysisResult__, dicImageLenght__, null, clearPaths, ref optimumStepX, int.Parse(Param.liftRightClearCount));
                    CameraGUIwei();
                }
                label30.Text = "无数据";
                //添加水印并插入数据库
                Thread thread = new Thread(new ParameterizedThreadStart(AddWatermarkInsertDatabase));
                thread.IsBackground = true;
                thread.Start(clearPaths);
                button12.Text = "自动对焦";
                if (!isLongRangeDebug)
                {
                    bStep = 14;
                    startTimer1Time = DateTime.Now;
                    timer1.Start();
                }
                tcpclient.SendCurrAction(142, "", "拍照");
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }

        /// <summary>
        /// 相机归位
        /// </summary>
        private void CameraGUIwei()
        {
            bStep = 9;
            Cmd.CommunicateDp(0x13, 4);
            list.Clear();
            list.Add(3);
            /*
             * 开始计时到培养液位置时间
             */
            startTime = DateTime.Now;
            timer2.Start();
            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "Timer2_Start");
            while (!isReady() && !bstop)
            {
                DebOutPut.DebLog("轴一电机左右移动拍照完毕，正在去培养液位置");
                Thread.Sleep(1000);
            }
            if (bstop)
                return;
            Timer2Stop();
            DebOutPut.DebLog("轴一到培养液位置");
            Thread.Sleep(5000);
            bStep = 11;
            Cmd.CommunicateDp(0x13, 5);
            list.Clear();
            list.Add(4);
            /*
            * 开始计时到拍照位置时间
            */
            startTime = DateTime.Now;
            timer2.Start();
            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "Timer2_Start");
            while (!isReady() && !bstop)
            {
                DebOutPut.DebLog("轴一电机左右移动拍照完毕，正在去拍照位置");
                Thread.Sleep(1000);
            }
            if (bstop)
                return;
            Timer2Stop();
            DebOutPut.DebLog("轴一到拍照位置");
        }

        /// <summary>
        /// 相机位置校正
        /// </summary>
        private void CameraPositionCorrection()
        {
            if (Param.XCorrecting != "0")
            {
                Thread.Sleep(15000);
                DebOutPut.DebLog("轴一移动使镜头对准 X 位校正后位置");
                Cmd.CommunicateDp(0x11, int.Parse(Param.XCorrecting));
                Thread.Sleep(15000);
            }
            if (Param.YCorrecting != "0")
            {
                if (Param.XCorrecting == "0")
                {
                    Thread.Sleep(15000);
                }
                CameraMovePc9_();
                if (pc9IsStop)
                    return;
                Thread.Sleep(15000);
                DebOutPut.DebLog("轴七移动使镜头对准 Y 位校正后位置");
                CameraRightLeftMove(true, Param.YCorrecting);
                Thread.Sleep(15000);
            }
        }

        /// <summary>
        /// 轴三到X9限位
        /// </summary>
        private void Axis3ToX9()
        {
            DebOutPut.DebLog("轴三移动到X8位置");
            bStep = 14;
            Cmd.CommunicateDp(0x33, 1);
            list.Clear();
            list.Add(7);
            /*
             * 开始计时此轴三回归X8时间
             */
            startTime = DateTime.Now;
            timer2.Start();
            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "Timer2_Start");
            while (!isReady() && !bstop)
                Thread.Sleep(1000);
            if (bstop)
                return;
            Timer2Stop();
            Thread.Sleep(15000);
            DebOutPut.DebLog("轴三移动到X9位置");
            bStep = 15;
            Cmd.CommunicateDp(0x33, 2);
            list.Clear();
            list.Add(8);
            /*
            * 开始计时此轴三回归X9时间
            */
            startTime = DateTime.Now;
            timer2.Start();
            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "Timer2_Start");
            while (!isReady() && !bstop)
                Thread.Sleep(1000);
            if (bstop)
                return;
            Timer2Stop();
            Thread.Sleep(15000);
        }


        private void timer7_Elapsed(object sender, EventArgs e)
        {
            if (Interlocked.Exchange(ref inTimer7, 1) == 0)
            {
                DateTime dt = DateTime.Now;
                if (bStep_ == 1)
                {
                    if (dt > (startTime_.AddSeconds(60)))//轴七电机找到PC9限位故障
                    {
                        FaultDiagnosis_("B09");
                        pc9IsStop = true;
                        AbnormalStop();
                    }
                }
                Interlocked.Exchange(ref inTimer7, 0);
            }
        }

        /// <summary>
        /// 相机移动到合适位置
        /// </summary>
        /// <param name="isLeftRegit">true：移动至PC8，false：移动至PC9</param>
        /// <param name="step">步数</param>
        private void CameraRightLeftMove(bool isLeftRegit, string step)
        {
            if (serialPort4 == null || !serialPort4.IsOpen)
            {
                DebOutPut.DebLog("副控串口未打开!");
                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "副控串口未打开!");
                return;
            }
            DebOutPut.DebLog("相机正在移动至指定位置");
            int index = 0;
            serialPort4.DiscardInBuffer();
            string frameHead = "5AA5";
            string func = "31";
            string content = (isLeftRegit == true) ? "01" : "00";
            string step1 = Tools.TenToSixteen(step).PadLeft(4, '0');
            string frameTail = "F0";
            string data = frameHead + func + content + step1 + frameTail;
            byte[] bytes = Tools.HexStrTobyte(data);

            for (int i = 0; i < 3; i++)
            {
                serialPort4.Write(bytes, 0, bytes.Length);
                DebOutPut.DebLog("发送:" + Cmd.byteToHexStr(bytes));
                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.ComLog, "发送:" + Cmd.byteToHexStr(bytes));
                Thread.Sleep(350);
                if (serialPort4.BytesToRead <= 0)
                    index++;
                else
                    break;
            }
            if (index == 3)
            {
                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.ComLog, "发送:未收到终端回应！");
                DebOutPut.DebLog("未收到终端回应！请重试!");
                return;
            }
            byte[] rec = new byte[5];
            serialPort4.Read(rec, 0, 5);
            string recStr = Cmd.byteToHexStr(rec);
            string recContent = recStr.Substring(6, 2);
            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.ComLog, "接收到:" + recStr + "     返回内容:" + recContent);
            DebOutPut.DebLog("接收到:" + recStr + "     返回内容:" + recContent);
        }

        /// <summary>
        /// 镜头电机移动至PC9限位
        /// </summary>
        private void CameraMovePc9_()
        {
            if (serialPort4 == null || !serialPort4.IsOpen)
            {
                DebOutPut.DebLog("副控串口未打开!");
                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "副控串口未打开!");
                return;
            }
            /*
             * 开始计时此回到PC9限位时间时间
             */
            DebOutPut.DebLog("轴七移动至PC9限位");
            bStep_ = 1;
            startTime_ = DateTime.Now;
            timer7.Start();
            CameraMovePc9();
            Timer7Stop();
        }
        /// <summary>
        /// 相机移动到Pc9限位
        /// </summary>
        private void CameraMovePc9()
        {
            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                if (serialPort4 == null || !serialPort4.IsOpen)
                {
                    DebOutPut.DebLog("副控串口未打开!");
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "副控串口未打开!");
                    return;
                }
                int index = 0;
                DebOutPut.DebLog("相机正在移动至Pc9位置");
                serialPort4.DiscardInBuffer();
                string frameHead = "5AA5";
                string func = "31";
                string content = "00";
                string step1 = Tools.TenToSixteen("255").PadLeft(4, '0');
                string frameTail = "F0";
                string data = frameHead + func + content + step1 + frameTail;
                byte[] bytes = Tools.HexStrTobyte(data);
                for (int i = 0; i < 3; i++)
                {
                    serialPort4.Write(bytes, 0, bytes.Length);
                    DebOutPut.DebLog("发送:" + Cmd.byteToHexStr(bytes));
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.ComLog, "发送:" + Cmd.byteToHexStr(bytes));
                    Thread.Sleep(350);
                    if (serialPort4.BytesToRead <= 0)
                        index++;
                    else
                        break;
                }
                if (index == 3)
                {
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.ComLog, "发送:未收到终端回应！");
                    DebOutPut.DebLog("未收到终端回应！请重试!");
                    return;
                }
                byte[] rec = new byte[5];
                serialPort4.Read(rec, 0, 5);
                string recStr = Cmd.byteToHexStr(rec);
                string recContent = recStr.Substring(6, 2);
                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.ComLog, "接收到:" + recStr + "     返回内容:" + recContent);
                DebOutPut.DebLog("接收到:" + recStr + "     返回内容:" + recContent);
                if (recContent == "01")//当前在右限位
                {
                    if (pc9IsStop)
                        return;
                    CameraMovePc9();
                }
                else if (recContent == "02")//以到达左限位
                {
                    //终止
                    return;
                }
                else//可自由移动
                {
                    if (pc9IsStop)
                        return;
                    CameraMovePc9();
                }
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }

        /// <summary>
        /// 移动履带
        /// </summary>
        private void MoveTrack()
        {
            if (serialPort4 == null || !serialPort4.IsOpen)
            {
                DebOutPut.DebLog("副控串口未打开!");
                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "副控串口未打开!");
                return;
            }
            DebOutPut.DebLog("移动履带");
            int index = 0;
            serialPort4.DiscardInBuffer();
            string frameHead = "5AA5";
            string func = "33";
            string content = "00";
            string frameTail = "F0";
            string data = frameHead + func + content + frameTail;
            byte[] bytes = Tools.HexStrTobyte(data);
            for (int i = 0; i < 3; i++)
            {
                serialPort4.Write(bytes, 0, bytes.Length);
                DebOutPut.DebLog("发送:" + Cmd.byteToHexStr(bytes));
                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "发送:" + Cmd.byteToHexStr(bytes));
                Thread.Sleep(350);
                if (serialPort4.BytesToRead <= 0)
                    index++;
                else
                    break;
            }
            if (index == 3)
            {
                DebOutPut.DebLog("未收到终端回应！");
                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "未收到终端回应！");
                return;
            }
            byte[] rec = new byte[5];
            serialPort4.Read(rec, 0, 5);
            string recStr = Cmd.byteToHexStr(rec);
            DebOutPut.DebLog("接收:" + recStr);
            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "接收:" + recStr);
        }


        private static object locker = new object();//创建锁

        /// <summary>
        /// 添加水印并插入数据库
        /// </summary>
        /// <param name="clearPaths">最终保存的清晰图像</param>
        private void AddWatermarkInsertDatabase(object myparam)
        {
            lock (locker)//加锁
            {
                List<string> clearPaths = (List<string>)myparam;
                DateTime dt;
                string collectTime = "";
                for (int i = 0; i < clearPaths.Count; i++)
                {
                    string path_ = clearPaths[i];
                    //水印时间
                    collectTime = path_.Substring(path_.Length - 18, 14);
                    collectTime = collectTime.Insert(4, "-").Insert(7, "-").Insert(10, " ").Insert(13, ":").Insert(16, ":");
                    dt = Convert.ToDateTime(collectTime);
                    collectTime = dt.ToString(Param.dataType, System.Globalization.DateTimeFormatInfo.InvariantInfo);
                    if (!File.Exists(path_))
                    {
                        DebOutPut.DebLog("图像：" + path_ + " 不存在");
                        return;
                    }
                    Bitmap bitmap = new Bitmap(path_);

                    //TODO：打水印
                    #region 打水印
                    if (Param.version == "2")//带光控雨控
                    {
                        using (Graphics g = Graphics.FromImage(bitmap))
                        {
                            string devNum = "ID:" + Param.DeviceID;
                            string time = "时间:" + collectTime;
                            string wendu = (this.lb_wd.Text.Trim() == "温度" || this.lb_wd.Text.Trim() == "无数据") ? "温度:---" : this.lb_wd.Text.Trim();
                            string shidu = (this.lb_sd.Text.Trim() == "湿度" || this.lb_sd.Text.Trim() == "无数据") ? "湿度:---" : this.lb_sd.Text.Trim();
                            string guangzhao = (this.lb_gz.Text.Trim() == "光照" || this.lb_gz.Text.Trim() == "无数据") ? "光照:---" : this.lb_gz.Text.Trim();
                            string yukong = (this.lb_yk.Text.Trim() == "雨控" || this.lb_yk.Text.Trim() == "无数据") ? "雨控:---" : this.lb_yk.Text.Trim();
                            string hj = wendu + "   " + shidu + "   " + guangzhao + "   " + yukong;
                            string context = devNum + "   " + time + "   " + hj;
                            g.DrawString(context, new Font("仿宋_GB2312", 40, FontStyle.Bold), System.Drawing.Brushes.Yellow, new PointF(50, 10));
                        }
                    }
                    else if (Param.version == "1")//不带光控雨控
                    {
                        using (Graphics g = Graphics.FromImage(bitmap))
                        {
                            string devNum = "ID:" + Param.DeviceID;
                            string time = "时间:" + collectTime;
                            string context = devNum + "   " + time;
                            g.DrawString(context, new Font("仿宋_GB2312", 40, FontStyle.Bold), System.Drawing.Brushes.Yellow, new PointF(50, 10));
                            //g.DrawString(collectTime, new Font("宋体", 100), System.Drawing.Brushes.Yellow, new PointF(2500, 100));
                            //g.DrawString(Param.DeviceID, new Font("宋体", 100), System.Drawing.Brushes.Yellow, new PointF(100, 100));
                        }
                    }
                    #endregion 打水印
                    Bitmap temp = new Bitmap(bitmap);
                    bitmap.Dispose();
                    temp.Save(path_, ImageFormat.Jpeg);
                    temp.Dispose();
                    String sql = "insert into Record (Flag,CollectTime) values ('0','" + collectTime + "')";
                    if (DB.updateDatabase(sql) != 1)
                    {
                        DebOutPut.DebLog("图像采集时间为：" + collectTime + "  插入数据库失败");
                    }
                    else
                    {
                        DebOutPut.DebLog("图像采集时间为：" + collectTime + "  插入数据库成功");
                    }
                    Thread.Sleep(1000);
                }
                DebOutPut.DebLog("数据库插入完毕，本次插入图像：" + clearPaths.Count + " 张");
                //图像上传
                if (Param.isContinuousUpload == "0")
                {
                    DebOutPut.DebLog("图像上传");
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "图像上传");
                    SendCollectionData();
                }
                //TODO:多余图像清除
                //ImageClear();
            }
        }

        int moveUpSteps = 50;//上移步数
        int moveDownSteps = 50;//下移步数
        int moveInterval = 1;//移动间隔
        int accurateMapSelection = 5;//精准选图
        bool isCrackSporeAnalysis = false;//是否对图像启用裂孢分析处理，如果启用，则请把原位选图，横向首选、复选，纵向首选、复选 改为0
        int triggerPosition = -1;//本次精准对焦触发位置（不算起始位置）
        //int triggerEndPosition = 0;//本次精准对焦触发终止位置（不算起始位置）

        /// <summary>
        /// 裂孢分析、选图
        /// </summary>
        ///  <param name="path">需要分析的图像路径</param>
        /// <param name="currPosition">最佳图像保存路径</param>
        /// <param name="currPosition">当前对焦次数</param>
        /// <param name="optimumStep">最佳位置</param>
        /// 返回是否存在裂孢
        private void CrackSporeAnalysis(string path, List<string> clearPaths, int currPosition)
        {
            int currPosition_ = currPosition + Convert.ToInt16(Param.MinSteps);
            bool result = false;
            if (path == "")
            {
                DebOutPut.DebLog("精准分析图像路径为空！");
                return;
            }
            if (triggerPosition == -1 || currPosition > triggerPosition + moveDownSteps /*triggerEndPosition*/)
            {
                result = Analysis(path);
                if (result == false)
                {
                    DebOutPut.DebLog("不存在裂孢！");
                    return;
                }
                triggerPosition = currPosition;
                DebOutPut.DebLog("精准对焦触发位置（不算起始位置）:" + triggerPosition);
                DebOutPut.DebLog("精准对焦触发图像:" + path);
                result = true;
                Dictionary<string, double> dicImageAnalysisResult = new Dictionary<string, double>();//保存照片路径和照片中包围性状总个数
                Dictionary<string, double> dicImageLenght = new Dictionary<string, double>();//当包围性状总个数为0时，保存当前照片路径和照片长度
                int startMove = currPosition_ - moveUpSteps;//精准对焦起始位置
                if (startMove < 0)
                    startMove = 0;
                int stopMove = currPosition_ + moveDownSteps;//精准对焦结束位置
                DebOutPut.DebLog("当前位置：" + currPosition_);
                DebOutPut.DebLog("精准对焦起始位置：" + startMove);
                DebOutPut.DebLog("精准对焦结束位置：" + stopMove);
                DebOutPut.DebLog("轴三开始移动到X9位置");
                Axis3ToX9();
                DebOutPut.DebLog("轴三开始移动到X9位置完毕");
                if (startMove != 0)
                {
                    DebOutPut.DebLog("轴三移动到精准对焦起始位置");
                    MoveUporMove(true, startMove.ToString(), nstep.ToString());//一次移动2步，移动到起始步数处
                    DebOutPut.DebLog("轴三移动到精准对焦起始位置完毕");
                    Thread.Sleep(15000);
                }
                string Tips = label30.Text;
                int totalSteps = moveUpSteps + moveDownSteps;
                int totalSteps_ = totalSteps;

                //bool isExist = false;
                for (int j = moveInterval; j < totalSteps + moveInterval; j += moveInterval)
                {
                    int shengyuCount = totalSteps_--;
                    if (shengyuCount < 0)
                        shengyuCount = 0;

                    label30.Text = Tips + "\r\n剩余精准对焦:\r\n" + shengyuCount + " 次";
                    if (!StartOldNewCamera())
                    {
                        Thread.Sleep(2000);
                        continue;
                    }
                    DebOutPut.DebLog("剩余精准对焦:" + shengyuCount + " 次");
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "剩余精准对焦:" + shengyuCount + " 次");
                    //拍照分析
                    string pathImage = PhotographAnalysis(dicImageAnalysisResult, dicImageLenght, null, 0, moveInterval.ToString());

                    //if (Analysis(pathImage))
                    //{
                    //    isExist = true;
                    //    DebOutPut.DebLog("发现裂孢！");
                    //}
                    //{
                    //    DebOutPut.DebLog("裂孢已经发现过了，但是目前已经看不见了！");
                    //    //triggerEndPosition = j - moveUpSteps;
                    //    //if (triggerEndPosition < 0)
                    //    //    triggerEndPosition = 0;
                    //    break;
                    //}

                }
                //选图 不找最佳位置  
                int optimumStepX = 0;
                SelectionOfMaps(dicImageAnalysisResult, dicImageLenght, null, clearPaths, ref optimumStepX, accurateMapSelection);
                DebOutPut.DebLog("轴三开始移动到X9位置");
                Axis3ToX9();
                DebOutPut.DebLog("轴三开始移动到X9位置完毕");
                if (stopMove != 0)
                {
                    DebOutPut.DebLog("轴三位置回归至：" + currPosition_.ToString() + "步（起始位置+触发精准对焦的位置）");
                    MoveUporMove(true, currPosition_.ToString(), nstep.ToString());//一次移动2步，轴三位置回归
                    DebOutPut.DebLog("轴三移动到精准对焦终止位置完毕");
                    Thread.Sleep(15000);
                }
                return;
            }
            else
            {

                Console.WriteLine("上一个触发对焦位置(不算起始位置):" + triggerPosition);
                Console.WriteLine("当前试图触发对焦位置(不算起始位置):" + currPosition);
                Console.WriteLine("可触发对焦位置(不算起始位置):" + (triggerPosition + moveDownSteps));
                Console.WriteLine("当前为位置已经被对焦过，无法触发精准对焦");
            }
        }

        /// <summary>
        /// 分析测试
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button8_Click_1(object sender, EventArgs e)
        {
            OpenFileDialog openfile = new OpenFileDialog();
            if (openfile.ShowDialog() == DialogResult.OK && (openfile.FileName != ""))
            {
                bool isSchizospora = Analysis(openfile.FileName);
            }
            openfile.Dispose();
        }

        /// <summary>
        /// 是否是裂孢分析
        /// </summary>
        /// <param name="path"></param>
        private bool Analysis(string path)
        {
            bool result = true;
            GC.Collect();
            if (Directory.Exists(Param.MattingPath))
                DeleteFolder(Param.MattingPath);
            if (!Directory.Exists(Param.MattingPath))
                Directory.CreateDirectory(Param.MattingPath);
            Mat srcImg = CvInvoke.Imread(path);
            Image<Bgr, Byte> src = srcImg.ToImage<Bgr, Byte>();
            Mat grayImg = new Mat();
            CvInvoke.CvtColor(srcImg, grayImg, ColorConversion.Bgr2Gray);
            Mat element = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(7, 7), new Point(-1, -1));//获取基本特征

            #region 1、形态梯度
            //1、膨胀
            Mat dilateImg = new Mat();
            CvInvoke.Dilate(grayImg, dilateImg, element, new Point(-1, -1), 1, BorderType.Default, new MCvScalar());
            //2、腐蚀
            Mat erodeImg = new Mat();
            CvInvoke.Erode(grayImg, erodeImg, element, new Point(-1, -1), 1, BorderType.Default, new MCvScalar());
            //3、膨胀减去腐蚀
            Mat xtimg = new Mat();
            CvInvoke.Subtract(dilateImg, erodeImg, xtimg);
            pictureBox1.BackgroundImage = xtimg.Bitmap;
            #endregion

            #region 2、轮廓绘制前处理
            Mat img = new Image<Bgr, byte>(xtimg.Bitmap).Mat;
            // 均值降噪
            Mat blurImg = new Mat();
            CvInvoke.GaussianBlur(img, blurImg, new Size(15, 15), 0, 0);
            //全局阀值二值化
            Mat binary = new Mat();
            Mat gray_src = new Mat();
            CvInvoke.CvtColor(blurImg, gray_src, ColorConversion.Bgr2Gray);
            CvInvoke.Threshold(gray_src, binary, 0, 255, ThresholdType.Binary | ThresholdType.Triangle);
            // 闭操作进行联通物体内部
            Mat morphImage = new Mat();
            Mat kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), new Point(-1, -1));
            CvInvoke.MorphologyEx(binary, morphImage, MorphOp.Close, kernel, new Point(-1, -1), 2, BorderType.Default, new MCvScalar(255, 0, 0, 255));
            #endregion

            #region 3、目标抠图

            List<Triangle2DF> triangleList = new List<Triangle2DF>();
            List<RotatedRect> boxList = new List<RotatedRect>();
            using (VectorOfVectorOfPoint contours_ = new VectorOfVectorOfPoint())
            {
                CvInvoke.FindContours(morphImage, contours_, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);
                int count = contours_.Size;
                for (int i = 0; i < count; i++)
                {
                    using (VectorOfPoint contour = contours_[i])
                    using (VectorOfPoint approxContour = new VectorOfPoint())
                    {
                        CvInvoke.ApproxPolyDP(contour, approxContour, CvInvoke.ArcLength(contour, true) * 0.01, true);
                        //仅考虑面积大于3000的轮廓
                        if (CvInvoke.ContourArea(approxContour, false) > 3000)
                        {
                            System.Drawing.Point[] pts = approxContour.ToArray();
                            boxList.Add(CvInvoke.MinAreaRect(approxContour));//可以返回一个包围轮廓最小的长方形
                        }
                    }
                }
            }
            //将所有的长方形截取下来
            Rectangle rectangle = new Rectangle(0, 0, src.Width, src.Height);
            int maxWidth = 0;
            int countA = 0;
            for (int i = 0; i < boxList.Count(); i++)
            {
                countA++;
                RotatedRect box = boxList[i];
                Rectangle rectangleTemp = box.MinAreaRect();
                rectangleTemp = new Rectangle(rectangleTemp.X, rectangleTemp.Y, rectangleTemp.Width, rectangleTemp.Height);
                maxWidth = rectangleTemp.Width;
                rectangle = rectangleTemp;
                CvInvoke.cvSetImageROI(src.Ptr, rectangle);//设置兴趣点—ROI
                Image<Bgr, Byte> clone = src.Clone();
                if (!Directory.Exists(Param.MattingPath))
                    Directory.CreateDirectory(Param.MattingPath);
                CvInvoke.Imwrite(Param.MattingPath + "\\抠图" + countA + ".bmp", clone); //保存结果图 
                clone.Dispose();
            }
            src.Dispose();
            srcImg.Dispose();
            src.Dispose();
            grayImg.Dispose();
            element.Dispose();
            dilateImg.Dispose();
            erodeImg.Dispose();
            xtimg.Dispose();
            img.Dispose();
            blurImg.Dispose();
            binary.Dispose();
            gray_src.Dispose();
            morphImage.Dispose();
            kernel.Dispose();
            #endregion

            #region 4、特征判断

            result = CharacteristicPand();

            #endregion
            return result;
        }

        /// <summary>
        /// 特征判断
        /// </summary>
        private bool CharacteristicPand()
        {
            if (!Directory.Exists(Param.MattingPath))
                Directory.CreateDirectory(Param.MattingPath);
            string[] files = Directory.GetFiles(Param.MattingPath, "*.bmp");
            DebOutPut.DebLog("抠图数量为：" + files.Length + " 张");
            if (files.Length == 0)
                return false;
            foreach (string path in files)
            {
                GC.Collect();
                Mat srcImg = CvInvoke.Imread(path);
                Image<Bgr, Byte> src = srcImg.ToImage<Bgr, Byte>();
                Mat grayImg = new Mat();
                CvInvoke.CvtColor(srcImg, grayImg, ColorConversion.Bgr2Gray);
                Mat element = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(7, 7), new Point(-1, -1));//获取基本特征

                #region 1、形态梯度
                //1、膨胀
                Mat dilateImg = new Mat();
                CvInvoke.Dilate(grayImg, dilateImg, element, new Point(-1, -1), 1, BorderType.Default, new MCvScalar());
                //2、腐蚀
                Mat erodeImg = new Mat();
                CvInvoke.Erode(grayImg, erodeImg, element, new Point(-1, -1), 1, BorderType.Default, new MCvScalar());
                //3、膨胀减去腐蚀
                Mat xtimg = new Mat();
                CvInvoke.Subtract(dilateImg, erodeImg, xtimg);
                pictureBox1.BackgroundImage = xtimg.Bitmap;
                #endregion

                #region 2、轮廓绘制前处理
                Mat img = new Image<Bgr, byte>(xtimg.Bitmap).Mat;
                // 均值降噪
                Mat blurImg = new Mat();
                CvInvoke.GaussianBlur(img, blurImg, new Size(15, 15), 0, 0);
                //全局阀值二值化
                Mat binary = new Mat();
                Mat gray_src = new Mat();
                CvInvoke.CvtColor(blurImg, gray_src, ColorConversion.Bgr2Gray);
                CvInvoke.Threshold(gray_src, binary, 0, 255, ThresholdType.Binary | ThresholdType.Triangle);
                //CvInvoke.AdaptiveThreshold(gray_src, binary, 255, AdaptiveThresholdType.MeanC, ThresholdType.Binary, 101, -1);//局部
                // 闭操作进行联通物体内部
                Mat morphImage = new Mat();
                Mat kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), new Point(-1, -1));
                CvInvoke.MorphologyEx(binary, morphImage, MorphOp.Close, kernel, new Point(-1, -1), 2, BorderType.Default, new MCvScalar(255, 0, 0, 255));
                #endregion

                #region 3、特征匹配
                //检测轮廓
                using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
                {
                    CvInvoke.FindContours(morphImage, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);
                    Mat imares = img;
                    Image<Bgr, Byte> outline = new Image<Bgr, byte>(img.Width, img.Height, new Bgr(0, 0, 0));
                    Dictionary<int, int> coordinateResult = new Dictionary<int, int>();//保存四个点，X最小和最大的  Y最小和最大的 Key是X，Value是Y
                    //轮廓绘制2  画出最大轮廓
                    for (int t = 0; t < contours.Size; t++)
                    {
                        double area = CvInvoke.ContourArea(contours[t]);
                        double len = CvInvoke.ArcLength(contours[t], true);
                        if (area < 3000)
                        {
                            //Console.WriteLine("轮廓:" + t + " 过滤");
                            continue;
                        }
                        Dictionary<int, int> coordinate = new Dictionary<int, int>();
                        //轮廓呈现
                        for (int j = 0; j < contours[t].Size; j++)
                        {
                            if (!coordinate.ContainsKey(contours[t][j].X))
                                coordinate.Add(contours[t][j].X, contours[t][j].Y);
                            //Console.WriteLine("X:" + contours[t][j].X + " Y:" + contours[t][j].Y);//组成每个轮廓的所有坐标
                            CvInvoke.Circle(outline, new Point(contours[t][j].X, contours[t][j].Y), 1, new MCvScalar(0, 0, 255));
                        }
                        coordinateResult.Clear();
                        Dictionary<int, int> coordinate_ = coordinate.OrderBy(o => o.Key).ToDictionary(o => o.Key, p => p.Value);//对key进行升序
                        //coordinateResult索引固定 索引0和1，是以X排序，0是X最大的坐标，1 X最小的坐标；索引2和3是以Y进行排序，2是Y最大的坐标，3是Y最小的坐标
                        coordinateResult.Add(KeyExists(coordinateResult, coordinate_.ElementAt(coordinate_.Count - 1).Key), coordinate_.ElementAt(coordinate_.Count - 1).Value);
                        coordinateResult.Add(KeyExists(coordinateResult, coordinate_.ElementAt(0).Key), coordinate_.ElementAt(0).Value);
                        //Console.WriteLine("X最大的坐标:" + coordinate_.ElementAt(coordinate_.Count - 1));
                        //Console.WriteLine("X最小的坐标:" + coordinate_.ElementAt(0));
                        //Console.WriteLine("X轴长度:" + (coordinate_.ElementAt(coordinate_.Count - 1).Key - coordinate_.ElementAt(0).Key));
                        coordinate_ = coordinate.OrderBy(o => o.Value).ToDictionary(o => o.Key, p => p.Value);//对Value进行升序
                        //Console.WriteLine("Y最大的坐标:" + coordinate_.ElementAt(coordinate_.Count - 1));
                        //Console.WriteLine("Y最小的坐标:" + coordinate_.ElementAt(0));
                        //Console.WriteLine("Y轴长度:" + (coordinate_.ElementAt(coordinate_.Count - 1).Value - coordinate_.ElementAt(0).Value));
                        coordinateResult.Add(KeyExists(coordinateResult, coordinate_.ElementAt(coordinate_.Count - 1).Key), coordinate_.ElementAt(coordinate_.Count - 1).Value);
                        coordinateResult.Add(KeyExists(coordinateResult, coordinate_.ElementAt(0).Key), coordinate_.ElementAt(0).Value);
                        CvInvoke.DrawContours(imares, contours, t, new MCvScalar(0, 0, 255), 2);
                    }
                    if (coordinateResult.Count == 0)
                    {
                        Console.WriteLine("图像不满足计算条件");
                        return false;
                    }
                    //Console.WriteLine("=================四点绘制=================");
                    for (int i = 0; i < coordinateResult.Count; i++)
                    {
                        //string txt = "(X:" + coordinateResult.ElementAt(i).Key + ", Y:" + coordinateResult.ElementAt(i).Value + ")";
                        //Console.WriteLine(txt);
                        CvInvoke.Circle(outline, new Point(coordinateResult.ElementAt(i).Key, coordinateResult.ElementAt(i).Value), 5, new MCvScalar(0, 255, 255));
                    }
                    //Console.WriteLine("=================直线绘制=================");
                    for (int i = 0; i < coordinateResult.Count; i += 2)
                    {
                        //Console.WriteLine("点1 " + i + " X:" + coordinateResult.ElementAt(i).Key + " Y:" + coordinateResult.ElementAt(i).Value);
                        //Console.WriteLine("点2 " + i + " X:" + coordinateResult.ElementAt(i + 1).Key + " Y:" + coordinateResult.ElementAt(i + 1).Value);
                        Point pointA1 = new Point(coordinateResult.ElementAt(i).Key, coordinateResult.ElementAt(i).Value);
                        Point pointA2 = new Point(coordinateResult.ElementAt(i + 1).Key, coordinateResult.ElementAt(i + 1).Value);
                        CvInvoke.Line(outline, pointA1, pointA2, new MCvScalar(0, 255, 255), 2);
                    }
                    pictureBox2.BackgroundImage = imares.Bitmap;
                    Point pt1 = new Point(coordinateResult.ElementAt(1).Key, coordinateResult.ElementAt(1).Value);
                    Point pt2 = new Point(coordinateResult.ElementAt(0).Key, coordinateResult.ElementAt(0).Value);
                    double radian = Math.Atan2((pt2.Y - pt1.Y), (pt2.X - pt1.X));//弧度
                    double angle = radian * (180 / Math.PI);//角度
                    //Console.WriteLine("直线角度：" + angle + "°");

                    //Console.WriteLine("=================旋转之后绘制=================");
                    Image<Bgr, Byte> outRotation = new Image<Bgr, byte>(img.Width * 2, img.Height * 2, new Bgr(0, 0, 0));
                    //围绕点
                    Point pointRotation = new Point(coordinateResult.ElementAt(1).Key, coordinateResult.ElementAt(1).Value);
                    Dictionary<int, int> coordinateA = new Dictionary<int, int>();
                    //轮廓绘制  根据旋转之后的点绘制轮廓
                    for (int t = 0; t < contours.Size; t++)
                    {
                        //Console.WriteLine("轮廓:" + t);
                        double area = CvInvoke.ContourArea(contours[t]);
                        double len = CvInvoke.ArcLength(contours[t], true);
                        //Console.WriteLine(area);
                        if (area < 3000)
                        {
                            //Console.WriteLine("轮廓:" + t + " 过滤");
                            //Console.WriteLine("面积:" + area);
                            continue;
                        }
                        coordinateA.Clear();
                        //轮廓呈现
                        for (int j = 0; j < contours[t].Size; j++)
                        {
                            double X = 0;
                            double Y = 0;
                            if (contours[t][j].X != pointRotation.X && contours[t][j].Y != pointRotation.Y)
                            {
                                RotateAngle(pointRotation.X, pointRotation.Y, 0 - angle, contours[t][j].X, contours[t][j].Y, ref X, ref Y);
                                if (!coordinateA.ContainsKey(int.Parse(X.ToString("F0"))))
                                    coordinateA.Add(int.Parse(X.ToString("F0")), int.Parse(Y.ToString("F0")));
                            }
                            else
                            {
                                if (!coordinateA.ContainsKey(pointRotation.X))
                                    coordinateA.Add(pointRotation.X, pointRotation.Y);
                            }

                            //Console.WriteLine("原坐标:(" + contours[t][j].X + "," + contours[t][j].Y + ") 围绕点:(" + pointRotation.X + "," + pointRotation.Y + ") 旋转角度:" + angle + "° 新坐标点:(" + X + "," + Y + ")");
                            CvInvoke.Circle(outRotation, new Point(int.Parse(X.ToString("F0")), int.Parse(Y.ToString("F0"))), 1, new MCvScalar(0, 0, 255));

                        }

                        coordinateResult.Clear();
                        Dictionary<int, int> coordinateA_ = coordinateA.OrderBy(o => o.Key).ToDictionary(o => o.Key, p => p.Value);//对key进行升序
                                                                                                                                   //coordinateResult索引固定 索引0和1，是以X排序，0是X最大的坐标，1 X最小的坐标；索引2和3是以Y进行排序，2是Y最大的坐标，3是Y最小的坐标
                        coordinateResult.Add(KeyExists(coordinateResult, coordinateA_.ElementAt(coordinateA_.Count - 1).Key), coordinateA_.ElementAt(coordinateA_.Count - 1).Value);
                        coordinateResult.Add(KeyExists(coordinateResult, coordinateA_.ElementAt(0).Key), coordinateA_.ElementAt(0).Value);
                        //Console.WriteLine("X最大的坐标:" + coordinateA_.ElementAt(coordinateA_.Count - 1));
                        //Console.WriteLine("X最小的坐标:" + coordinateA_.ElementAt(0));
                        //Console.WriteLine("X轴长度:" + (coordinateA_.ElementAt(coordinateA_.Count - 1).Key - coordinateA_.ElementAt(0).Key));
                        //coordinateA_ = coordinateA.OrderBy(o => o.Value).ToDictionary(o => o.Key, p => p.Value);//对Value进行升序
                        //Console.WriteLine("Y最大的坐标:" + coordinateA_.ElementAt(coordinateA_.Count - 1));
                        //Console.WriteLine("Y最小的坐标:" + coordinateA_.ElementAt(0));
                        //Console.WriteLine("Y轴长度:" + (coordinateA_.ElementAt(coordinateA_.Count - 1).Value - coordinateA_.ElementAt(0).Value));
                        //coordinateResult.Add(KeyExists(coordinateResult, coordinateA_.ElementAt(coordinateA_.Count - 1).Key), coordinateA_.ElementAt(coordinateA_.Count - 1).Value);
                        //coordinateResult.Add(KeyExists(coordinateResult, coordinateA_.ElementAt(0).Key), coordinateA_.ElementAt(0).Value);
                        //Console.WriteLine("=================两点绘制=================");
                        for (int i = 0; i < coordinateResult.Count; i++)
                        {
                            string txt = "(X:" + coordinateResult.ElementAt(i).Key + ", Y:" + coordinateResult.ElementAt(i).Value + ")";
                            //Console.WriteLine(txt);
                            CvInvoke.Circle(outRotation, new Point(coordinateResult.ElementAt(i).Key, coordinateResult.ElementAt(i).Value), 5, new MCvScalar(0, 255, 255));
                            //CvInvoke.PutText(outRotation, txt, new Point(coordinateResult.ElementAt(i).Key - 20, coordinateResult.ElementAt(i).Value - 10), FontFace.HersheyComplex, 0.4/*在原字体的大小基础之上乘以这个值*/, new MCvScalar(0, 255, 0)/*字体颜色*/, 1/*字体粗细*/);
                        }
                        //Console.WriteLine("=================直线绘制=================");
                        //for (int i = 0; i < coordinateResult.Count; i += 2)
                        //{
                        //    Console.WriteLine("点1 " + i + " X:" + coordinateResult.ElementAt(i).Key + " Y:" + coordinateResult.ElementAt(i).Value);
                        //    Console.WriteLine("点2 " + i + " X:" + coordinateResult.ElementAt(i + 1).Key + " Y:" + coordinateResult.ElementAt(i + 1).Value);
                        //    Point point1 = new Point(coordinateResult.ElementAt(i).Key, coordinateResult.ElementAt(i).Value);
                        //    Point point2 = new Point(coordinateResult.ElementAt(i + 1).Key, coordinateResult.ElementAt(i + 1).Value);
                        //    CvInvoke.Line(outRotation, point1, point2, new MCvScalar(0, 255, 255), 2);
                        //}
                        Point point1 = new Point(coordinateResult.ElementAt(0).Key, coordinateResult.ElementAt(0).Value);
                        Point point2 = new Point(coordinateResult.ElementAt(1).Key, coordinateResult.ElementAt(1).Value);
                        //CvInvoke.Line(outRotation, point1, point2, new MCvScalar(0, 255, 255), 2);
                        //Dictionary<Point, Point> coordinateB = new Dictionary<Point, Point>();

                        List<Dictionary<Point, Point>> coordinateBList = new List<Dictionary<Point, Point>>();
                        GetCrossLine(point1, point2, coordinateA, ref coordinateBList);
                        Dictionary<double, MyPoint> coordinateC = new Dictionary<double, MyPoint>();

                        //绘出所有线
                        for (int i = 0; i < coordinateBList.Count; i++)
                            for (int j = 0; j < coordinateBList[i].Count; j++)
                                CvInvoke.Line(outRotation, coordinateBList[i].ElementAt(j).Key, coordinateBList[i].ElementAt(j).Value, new MCvScalar(255, 255, 0), 1);

                        for (int i = 0; i < coordinateBList.Count; i++)
                        {
                            for (int j = 0; j < coordinateBList[i].Count; j++)
                            {
                                double jd = Math.Abs(GetAngle(point1, point2, coordinateBList[i].ElementAt(j).Key, coordinateBList[i].ElementAt(j).Value));
                                //Console.WriteLine("角度:" + jd + "°");
                                double approachValue = jd;
                                if (jd <= 90)
                                    approachValue = Math.Abs(jd - 90);
                                else if (jd > 90)
                                    approachValue = Math.Abs(jd - 270);
                                MyPoint myPoint = new MyPoint();
                                myPoint.x1 = coordinateBList[i].ElementAt(j).Key.X;
                                myPoint.y1 = coordinateBList[i].ElementAt(j).Key.Y;
                                myPoint.x2 = coordinateBList[i].ElementAt(j).Value.X;
                                myPoint.y2 = coordinateBList[i].ElementAt(j).Value.Y;
                                myPoint.jd = jd;
                                if (!coordinateC.ContainsKey(approachValue))
                                    coordinateC.Add(approachValue, myPoint);
                                Thread.Sleep(1);
                                //Console.WriteLine("角度:" + jd + "°");
                            }
                        }
                        //Console.WriteLine("交线总量:" + coordinateC.Count);
                        //绘制X轴距离最长直线线段
                        CvInvoke.Line(outRotation, point1, point2, new MCvScalar(0, 255, 255), 2);
                        Point pointB1 = new Point();
                        Point pointB2 = new Point();
                        //绘制Y轴与X轴交叉且最接近于直角的线段
                        if (coordinateC.Count > 0)
                        {
                            Dictionary<double, MyPoint> coordinateC_ = coordinateC.OrderBy(o => o.Key).ToDictionary(o => o.Key, p => p.Value);//对key进行升序
                            //Console.WriteLine("最接近90°的角度:" + coordinateC_.ElementAt(0).Value.jd + " 接近值:" + coordinateC_.ElementAt(0).Key);
                            pointB1 = new Point(coordinateC_.ElementAt(0).Value.x1, coordinateC_.ElementAt(0).Value.y1);
                            pointB2 = new Point(coordinateC_.ElementAt(0).Value.x2, coordinateC_.ElementAt(0).Value.y2);
                            CvInvoke.Circle(outRotation, pointB1, 5, new MCvScalar(0, 255, 255));
                            CvInvoke.Circle(outRotation, pointB2, 5, new MCvScalar(0, 255, 255));
                            CvInvoke.Line(outRotation, pointB1, pointB2, new MCvScalar(0, 255, 255), 2);
                        }
                        double Xdistance = GetTwoPointDistance(point1, point2);
                        double Ydistance = GetTwoPointDistance(pointB1, pointB2);
                        Console.WriteLine("Xdistance / Ydistance :" + Xdistance / Ydistance);
                        if (Xdistance / Ydistance > 1.7f && Xdistance / Ydistance < 4.3f)
                        {
                            Console.WriteLine("存在目标裂孢");
                            return true;
                        }
                    }

                }
                #endregion
            }
            return false;
        }

        /// <summary>
        /// 获取两点之间的距离
        /// </summary>
        /// <param name="pointA"></param>
        /// <param name="pointB"></param>
        private double GetTwoPointDistance(Point pointA, Point pointB)
        {
            int x1 = pointA.X;
            int y1 = pointA.Y;
            int x2 = pointB.X;
            int y2 = pointB.Y;
            int xdiff = x2 - x1;
            int ydiff = y2 - y1;
            return Math.Sqrt(xdiff * xdiff + ydiff * ydiff);
        }
        /// <summary>
        /// 获取目标线的交叉线
        /// </summary>
        /// <param point1="">目标线起点</param>
        /// <param point2="">目标线终点</param>
        /// <param coordinateA="">所有坐标</param>
        /// 返回 coordinateB 交叉线的两个端点坐标
        private void GetCrossLine(Point point1, Point point2, Dictionary<int, int> coordinateA, ref List<Dictionary<Point, Point>> resultList)
        {
            //List<Dictionary<Point, Point>> resultList = new List<Dictionary<Point, Point>>();
            Dictionary<Point, Point> coordinateB = null;
            //遍历所有坐标找到与其线段(point1，point2)相交的点
            for (int i = 0; i < coordinateA.Count; i++)
            {
                int x1 = coordinateA.ElementAt(i).Key;
                int y1 = coordinateA.ElementAt(i).Value;
                for (int j = 0; j < coordinateA.Count; j++)
                {
                    Point pt = new Point();
                    int x2 = coordinateA.ElementAt(j).Key;
                    int y2 = coordinateA.ElementAt(j).Value;
                    int x = GetIntersection(point2, point1, new Point(x1, y1), new Point(x2, y2), ref pt);
                    if (x == 1)
                    {
                        if (resultList == null || resultList.Count == 0)
                        {
                            coordinateB = new Dictionary<Point, Point>();
                            resultList.Add(coordinateB);
                        }
                        bool keyIsExist = false;
                        for (int m = 0; m < resultList.Count; m++)
                        {
                            if (!resultList[m].ContainsKey(new Point(x1, y1)))
                            {
                                keyIsExist = true;
                                resultList[m].Add(new Point(x1, y1), new Point(x2, y2));
                                //Console.WriteLine("直线:(x1:" + x1 + ",y1:" + y1 + ")-(x2:" + x1 + ",y2:" + y1 + ")与目标直线相交,交点:(" + pt.X + "," + pt.Y + ")");
                                break;
                            }
                        }
                        if (!keyIsExist)
                        {
                            coordinateB = new Dictionary<Point, Point>();
                            resultList.Add(coordinateB);
                            coordinateB.Add(new Point(x1, y1), new Point(x2, y2));
                            //Console.WriteLine("直线:(x1:" + x1 + ",y1:" + y1 + ")-(x2:" + x1 + ",y2:" + y1 + ")与目标直线相交,交点:(" + pt.X + "," + pt.Y + ")");
                        }
                    }
                }
            }
        }

        class MyPoint
        {
            public int x1 = 0;
            public int y1 = 0;
            public int x2 = 0;
            public int y2 = 0;
            public double jd = 0;
        }
        /// <summary>
        /// 判断两条线是否相交
        /// </summary>
        /// <param name="a">线段1起点坐标</param>
        /// <param name="b">线段1终点坐标</param>
        /// <param name="c">线段2起点坐标</param>
        /// <param name="d">线段2终点坐标</param>
        /// <param name="intersection">相交点坐标</param>
        /// <returns>是否相交 0:两线平行  -1:不平行且未相交  1:两线相交</returns>

        private int GetIntersection(Point a, Point b, Point c, Point d, ref Point intersection)
        {
            //判断异常
            if (Math.Abs(b.X - a.Y) + Math.Abs(b.X - a.X) + Math.Abs(d.Y - c.Y) + Math.Abs(d.X - c.X) == 0)
            {
                if (c.X - a.X == 0)
                {
                    //Console.WriteLine("ABCD是同一个点！");
                }
                else
                {
                    //Console.WriteLine("AB是一个点，CD是一个点，且AC不同！");
                }
                return 0;
            }

            if (Math.Abs(b.Y - a.Y) + Math.Abs(b.X - a.X) == 0)
            {
                if ((a.X - d.X) * (c.Y - d.Y) - (a.Y - d.Y) * (c.X - d.X) == 0)
                {
                    //Console.WriteLine("A、B是一个点，且在CD线段上！");
                }
                else
                {
                    // Console.WriteLine("A、B是一个点，且不在CD线段上！");
                }
                return 0;
            }

            if (Math.Abs(d.Y - c.Y) + Math.Abs(d.X - c.X) == 0)
            {
                if ((d.X - b.X) * (a.Y - b.Y) - (d.Y - b.Y) * (a.X - b.X) == 0)
                {
                    //Console.WriteLine("C、D是一个点，且在AB线段上！");
                }
                else
                {
                    //Console.WriteLine("C、D是一个点，且不在AB线段上！");
                }
            }


            if ((b.Y - a.Y) * (c.X - d.X) - (b.X - a.X) * (c.Y - d.Y) == 0)
            {
                //Console.WriteLine("线段平行，无交点！");
                return 0;
            }

            intersection.X = ((b.X - a.X) * (c.X - d.X) * (c.Y - a.Y) - c.X * (b.X - a.X) * (c.Y - d.Y) + a.X * (b.Y - a.Y) * (c.X - d.X)) / ((b.Y - a.Y) * (c.X - d.X) - (b.X - a.X) * (c.Y - d.Y));
            intersection.Y = ((b.Y - a.Y) * (c.Y - d.Y) * (c.X - a.X) - c.Y * (b.Y - a.Y) * (c.X - d.X) + a.Y * (b.X - a.X) * (c.Y - d.Y)) / ((b.X - a.X) * (c.Y - d.Y) - (b.Y - a.Y) * (c.X - d.X));


            if ((intersection.X - a.X) * (intersection.X - b.X) <= 0 && (intersection.X - c.X) * (intersection.X - d.X) <= 0 && (intersection.Y - a.Y) * (intersection.Y - b.Y) <= 0 && (intersection.Y - c.Y) * (intersection.Y - d.Y) <= 0)
            {
                //Console.WriteLine("线段相交于点(" + intersection.X + "," + intersection.Y + ")！");
                return 1; //'相交
            }
            else
            {
                // Console.WriteLine("线段相交于虚交点(" + intersection.X + "," + intersection.Y + ")！");
                return -1; //'相交但不在线段上
            }
        }

        /// <summary>
        /// 获取两条直线相交的角度
        /// </summary>
        /// <param name="a1">线段1起点</param>
        /// <param name="a2">线段1终点</param>
        /// <param name="b1">线段2起点</param>
        /// <param name="b2">线段2终点</param>
        /// <returns></returns>
        private double GetAngle(Point a1, Point a2, Point b1, Point b2)
        {
            var a = Math.Atan2(a2.Y - a1.Y, a2.X - a1.X);
            var b = Math.Atan2(b2.Y - b1.Y, b2.X - b1.X);
            double x = (180 * (b - a) / Math.PI);
            return x;
        }

        /// <summary>
        /// 判断Key是否存在，如果不存在，则返回原来的，如果存在，则原Key进行++，直到不存在
        /// </summary>
        /// <returns></returns>
        private int KeyExists(Dictionary<int, int> coordinateResult, int key)
        {
            int retKey = key;
            while (coordinateResult.ContainsKey(retKey))
                retKey++;
            return retKey;
        }

        /// <summary>
        /// 旋转的点
        /// </summary>
        /// <param name="XRotation">围绕点X</param>
        /// <param name="YRotation">围绕点Y</param>
        /// <param name="ARotate">旋转角度</param>
        /// <param name="XBefore">目标点X</param>
        /// <param name="YBefore">目标点Y</param>
        /// <param name="XAfter">返回值</param>
        /// <param name="YAfter">返回值</param>
        /// <returns></returns>
        public static string RotateAngle(double XRotation, double YRotation, double ARotate, double XBefore, double YBefore, ref double XAfter, ref double YAfter)
        {
            try
            {
                double Rad = 0;
                //Rad = ARotate * Math.Acos(-1) / 180;;
                Rad = ARotate * Math.PI / 180;
                XAfter = (XBefore - XRotation) * Math.Cos(Rad) - (YBefore - YRotation) * Math.Sin(Rad) + XRotation;
                YAfter = (YBefore - YRotation) * Math.Cos(Rad) + (XBefore - XRotation) * Math.Sin(Rad) + YRotation;

                return "OK";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        /// <summary>
        /// 相机微调、拍照保存、图像分析、位置记录
        /// </summary>
        /// <param name="count">当前拍摄张数</param>
        /// <param name="path">图像保存路径</param>
        /// <param name="dicImageAnalysisResult">包围状不为0时，图像分析结果字典，Key 路径  Value 包围状面积</param>
        /// <param name="dicImageLenght">包围状为0时，图像分析结果字典，Key 路径  Value 长度</param>
        /// <param name="dicImageStep">当前拍摄图像步数字典，Key 路径  Value 步数，该值为null时，该参数失效</param>
        /// <param name="imageStep">当前拍摄图像所在步数 dicImageStep为null时，该参数失效</param>
        /// <param name="nstep">相机每次微调的步数</param>
        private string PhotographAnalysis(Dictionary<string, double> dicImageAnalysisResult, Dictionary<string, double> dicImageLenght, Dictionary<string, int> dicImageStep, int imageStep, string nstep)
        {

            MoveUporMove(true, Convert.ToString(nstep));
            Thread.Sleep(2000);
            DateTime dt = DateTime.Now;//采集时间
            string path = Param.BasePath + "\\GrabImg\\" + dt.ToString("yyyyMMddHHmmss", System.Globalization.DateTimeFormatInfo.InvariantInfo) + ".bmp";
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            Image realTimeImage = null;
            if (this.IsHandleCreated)
            {
                this.Invoke(
                (EventHandler)delegate
                {
                    //判断相机
                    if (Param.cameraVersion == "1")
                    {
                        realTimeImage = OldGetPic();
                    }
                    else if (Param.cameraVersion == "2")
                    {
                        realTimeImage = GetPic();
                    }
                });
            }
            if (realTimeImage == null)
            {
                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "拍照失败！");
                return "";
            }
            Param.SaveImage(realTimeImage, dt.ToString("yyyyMMddHHmmss", System.Globalization.DateTimeFormatInfo.InvariantInfo) + ".bmp");
            Thread.Sleep(2000);
            realTimeImage.Dispose();
            DebOutPut.DebLog("图像释放");
            //保存每个照片的照片路径和图像分析结果
            DebOutPut.DebLog("图像分析");
            double analysisResult = ImageAnalysis(path);
            if (analysisResult != 0)
            {
                dicImageAnalysisResult.Add(path, analysisResult);
            }
            else
            {
                FileInfo fileInfo = new FileInfo(path);
                double fileSize = fileInfo.Length;
                dicImageLenght.Add(path, fileSize);
            }
            if (dicImageStep != null)
                dicImageStep.Add(path, imageStep);//保存该照片从X9到现在位置的步数
            return path;
        }

        /// <summary>
        /// 选图、找出最佳位置
        /// </summary>
        /// <param name="dicImageAnalysisResult">包围状不为0时，图像分析结果字典，Key 路径  Value 包围状面积</param>
        /// <param name="dicImageLenght">包围状为0时，图像分析结果字典，Key 路径  Value 长度</param>
        /// <param name="clearPaths">最终选择的图像列表</param>
        /// <param name="dicImageStep">当前拍摄图像步数字典，Key 路径  Value 步数，该值为null时，该参数失效</param>
        /// <param name="path">最清晰图像路径</param>
        /// <param name="optimumStep">最清晰图像步数</param> 
        /// <param name="imageCount">选图数量</param> 
        private void SelectionOfMaps(Dictionary<string, double> dicImageAnalysisResult, Dictionary<string, double> dicImageLenght, Dictionary<string, int> dicImageStep, List<string> clearPaths, ref int optimumStep, int imageCount)
        {
            if (dicImageAnalysisResult.Count == 0 && dicImageLenght.Count == 0)
            {
                return;
            }
            if (dicImageAnalysisResult.Count == 0)//所有图像中都没有找到孢子
            {
                DebOutPut.DebLog("所有图像中都没有找到孢子，图像总数：" + dicImageLenght.Count + " 张");
                //将filmlenght按照升序进行排序，并复制进dicfilmLenght，最后一个照片长度就是最大的
                Dictionary<string, double> dicfilmLenght = dicImageLenght.OrderBy(o => o.Value).ToDictionary(p => p.Key, o => o.Value);
                string path = dicfilmLenght.Keys.Last();
                //拍照最佳位置
                if (dicImageStep != null)
                    optimumStep = dicImageStep[path];
                //连续法选图
                if (Param.mapSelectionScheme == "0")
                    ContinuousMapSelection(dicImageAnalysisResult, dicImageLenght, dicImageLenght, dicfilmLenght, clearPaths, imageCount);
                //间断法选图
                else if (Param.mapSelectionScheme == "1")
                    DiscontinuousMapSelection(dicImageAnalysisResult, dicImageLenght, dicfilmLenght, clearPaths, imageCount);
            }
            else if (dicImageAnalysisResult.Count > 0)
            {
                //在所有检测到包围性状的图片中找到长度最长的，为最清晰的图像
                DebOutPut.DebLog("在图像中检测到有孢子存在，包含孢子图像数量：" + dicImageAnalysisResult.Count + " 张");
                //找到包围性状总面积最大的
                Dictionary<string, double> dicImageAnalysisResult_ = dicImageAnalysisResult.OrderBy(o => o.Value).ToDictionary(p => p.Key, o => o.Value);
                string path = dicImageAnalysisResult_.Keys.Last();
                //拍照最佳位置
                if (dicImageStep != null)
                    optimumStep = dicImageStep[path];
                //连续法选图
                if (Param.mapSelectionScheme == "0")
                    ContinuousMapSelection(dicImageAnalysisResult, dicImageLenght, dicImageAnalysisResult, dicImageAnalysisResult_, clearPaths, imageCount);
                //间断法选图
                else if (Param.mapSelectionScheme == "1")
                    DiscontinuousMapSelection(dicImageAnalysisResult, dicImageLenght, dicImageAnalysisResult_, clearPaths, imageCount);
            }
            if (dicImageStep != null)
                dicImageStep.Clear();
        }

        /// <summary>
        /// 连续选图法
        /// </summary>
        /// <param name="dicImageAnalysisResult">图像分析结果</param>
        /// <param name="dicImageLenght">图像长度字典</param>
        /// <param name="dicSortFront">排序前的图像字典</param>
        /// <param name="dicfilmLenght">排序后的图像字典</param>
        /// <param name="clearPaths">最清晰的图像列表</param>
        /// <param name="imageCount">选图数量</param> 
        private void ContinuousMapSelection(Dictionary<string, double> dicImageAnalysisResult, Dictionary<string, double> dicImageLenght, Dictionary<string, double> dicSortFront, Dictionary<string, double> dicfilmLenght, List<string> clearPaths, int imageCount)
        {
            //不包含孢子
            if (dicImageAnalysisResult.Count == 0)
            {
                DebOutPut.DebLog("连续选图法");
                int clearCount_ = imageCount;
                if (dicfilmLenght.Count < clearCount_)
                    clearCount_ = dicfilmLenght.Count;
                List<string> dicImageLenght_ = new List<string>();
                foreach (string item in dicSortFront.Keys)
                {
                    dicImageLenght_.Add(item);
                }
                int upSelect = clearCount_ / 2;//上选张数
                int lowSelect = clearCount_ - upSelect;//下选张数，下选包含最清晰图像
                int clearIndex = 0;//最清晰图像索引
                for (int i = 0; i < dicImageLenght_.Count; i++)
                {
                    if (dicImageLenght_[i] == dicfilmLenght.Keys.Last())
                    {
                        clearIndex = i;
                        break;
                    }
                }
                int upStartIndex = clearIndex - upSelect;//上选起始索引
                int lowEndIndex = clearIndex + lowSelect;//下选终止索引
                if (upStartIndex <= 0)
                {
                    DebOutPut.DebLog("上选起始索引为小于等于0,上选索引将默认从0开始");
                    upStartIndex = 0;
                }
                if (lowEndIndex >= dicImageLenght_.Count)
                {
                    DebOutPut.DebLog("下选终止索引超出最大范围,下选终止索引将被强制设置为最大值");
                    lowEndIndex = dicImageLenght_.Count;
                }
                DebOutPut.DebLog("上选：" + upSelect + " 张");
                DebOutPut.DebLog("下选：" + lowSelect + " 张");
                DebOutPut.DebLog("目标索引：" + clearIndex);
                DebOutPut.DebLog("上选起始索引：" + upStartIndex);
                DebOutPut.DebLog("下选终止索引：" + lowEndIndex);
                for (int i = upStartIndex; i < clearIndex; i++)
                {
                    string clearPath = dicImageLenght_[i];
                    clearPaths.Add(clearPath);
                    dicfilmLenght.Remove(clearPath);
                    DebOutPut.DebLog("上选→保存清晰图像 " + i + " :" + clearPath);
                }
                for (int i = clearIndex; i < lowEndIndex; i++)
                {
                    string clearPath = dicImageLenght_[i];
                    clearPaths.Add(clearPath);
                    dicfilmLenght.Remove(clearPath);
                    DebOutPut.DebLog("下选→保存清晰图像 " + i + " :" + clearPath);
                }
                //删除剩余的
                foreach (var item in dicfilmLenght)
                {
                    if (File.Exists(item.Key))
                    {
                        File.Delete(item.Key);
                    }
                }
                DebOutPut.DebLog("选图完毕,本次预计选图：" + imageCount + " 张，实际选图：" + (lowEndIndex - upStartIndex) + " 张");
            }
            //包含孢子
            else if (dicImageAnalysisResult.Count > 0)
            {
                DebOutPut.DebLog("连续法选图");
                int clearCount_ = imageCount;
                if (dicfilmLenght.Count < clearCount_)
                    clearCount_ = dicfilmLenght.Count;
                List<string> dicImageLenght_ = new List<string>();
                foreach (string item in dicSortFront.Keys)
                {
                    dicImageLenght_.Add(item);
                }
                int upSelect = clearCount_ / 2;//上选张数
                int lowSelect = clearCount_ - upSelect;//下选张数，下选包含最清晰图像
                int clearIndex = 0;//最清晰图像索引
                for (int i = 0; i < dicImageLenght_.Count; i++)
                {
                    if (dicImageLenght_[i] == dicfilmLenght.Keys.Last())
                    {
                        clearIndex = i;
                        break;
                    }
                }
                int upStartIndex = clearIndex - upSelect;//上选起始索引
                int lowEndIndex = clearIndex + lowSelect;//下选终止索引
                if (upStartIndex <= 0)
                {
                    DebOutPut.DebLog("上选起始索引为小于等于0,上选索引将默认从0开始");
                    upStartIndex = 0;
                }
                if (lowEndIndex >= dicImageLenght_.Count)
                {
                    DebOutPut.DebLog("下选终止索引超出最大范围,下选终止索引将被强制设置为最大值");
                    lowEndIndex = dicImageLenght_.Count;
                }
                DebOutPut.DebLog("上选：" + upSelect + " 张");
                DebOutPut.DebLog("下选：" + lowSelect + " 张");
                DebOutPut.DebLog("目标索引：" + clearIndex);
                DebOutPut.DebLog("上选起始索引：" + upStartIndex);
                DebOutPut.DebLog("下选终止索引：" + lowEndIndex);
                for (int i = upStartIndex; i < clearIndex; i++)
                {
                    string clearPath = dicImageLenght_[i];
                    clearPaths.Add(clearPath);
                    dicfilmLenght.Remove(clearPath);
                    DebOutPut.DebLog("上选→保存清晰图像 " + i + " :" + clearPath);
                }
                for (int i = clearIndex; i < lowEndIndex; i++)
                {
                    string clearPath = dicImageLenght_[i];
                    clearPaths.Add(clearPath);
                    dicfilmLenght.Remove(clearPath);
                    DebOutPut.DebLog("下选→保存清晰图像 " + i + " :" + clearPath);
                }
                //删除剩余的
                foreach (var item in dicfilmLenght)
                {
                    if (File.Exists(item.Key))
                    {
                        File.Delete(item.Key);
                    }
                }
                foreach (var item in dicImageLenght)
                {
                    if (File.Exists(item.Key))
                    {
                        File.Delete(item.Key);
                    }
                }
                DebOutPut.DebLog("选图完毕,本次预计选图：" + imageCount + " 张，实际选图：" + (lowEndIndex - upStartIndex) + " 张");
            }
        }

        /// <summary>
        /// 间断选图法
        /// </summary>
        /// <param name="dicImageAnalysisResult">图像分析结果</param>
        /// <param name="dicImageLenght">图像长度字典</param>
        /// <param name="dicfilmLenght">排过序后的图像字典</param>
        /// <param name="clearPaths">最清晰的图像列表</param>
        /// <param name="imageCount">选图数量</param>
        private void DiscontinuousMapSelection(Dictionary<string, double> dicImageAnalysisResult, Dictionary<string, double> dicImageLenght, Dictionary<string, double> dicfilmLenght, List<string> clearPaths, int imageCount)
        {
            //不包含孢子
            if (dicImageAnalysisResult.Count == 0)
            {
                DebOutPut.DebLog("间断选图法");
                int clearCount_ = imageCount;
                if (dicfilmLenght.Count < clearCount_)
                    clearCount_ = dicfilmLenght.Count;
                for (int i = 0; i < clearCount_; i++)
                {
                    string clearPath = dicfilmLenght.Keys.Last();
                    clearPaths.Add(clearPath);
                    dicfilmLenght.Remove(clearPath);
                    DebOutPut.DebLog("保存清晰图像 " + i + " :" + clearPath);
                }

                //删除剩余的
                foreach (var item in dicfilmLenght)
                {
                    if (File.Exists(item.Key))
                    {
                        File.Delete(item.Key);
                    }
                }
                DebOutPut.DebLog("选图完毕,本次预计选图：" + imageCount + " 张，实际选图：" + clearCount_ + " 张");
            }
            //包含孢子
            else if (dicImageAnalysisResult.Count > 0)
            {
                DebOutPut.DebLog("间断选图法");
                int clearCount_ = imageCount;
                if (dicfilmLenght.Count < clearCount_)
                    clearCount_ = dicfilmLenght.Count;
                for (int i = 0; i < clearCount_; i++)
                {
                    string clearPath = dicfilmLenght.Keys.Last();
                    clearPaths.Add(clearPath);
                    dicfilmLenght.Remove(clearPath);
                    DebOutPut.DebLog("保存清晰图像 " + i + " :" + clearPath);
                }
                //全部删除
                foreach (var item in dicfilmLenght)
                {
                    if (File.Exists(item.Key))
                    {
                        File.Delete(item.Key);
                    }
                }
                foreach (var item in dicImageLenght)
                {
                    if (File.Exists(item.Key))
                    {
                        File.Delete(item.Key);
                    }
                }
                DebOutPut.DebLog("选图完毕,本次预计选图：" + imageCount + " 张，实际选图：" + clearCount_ + " 张");
            }

        }


        /// <summary>
        /// 图像分析
        /// </summary>
        /// <param name="path">图像路径</param>
        /// <returns>包围性状所占总面积</returns>
        private double ImageAnalysis(string path)
        {
            try
            {
                //原始图像
                Mat img = CvInvoke.Imread(path);
                //灰度化
                Mat gray = new Mat();
                CvInvoke.CvtColor(img, gray, ColorConversion.Bgr2Gray);
                //大津法局部阀值二值化
                Mat dst = new Mat();
                CvInvoke.AdaptiveThreshold(gray, dst, 255, AdaptiveThresholdType.MeanC, ThresholdType.Binary, 101, double.Parse(Param.compensate));
                //指定参数获得结构元素 形态学闭运算去噪
                Mat element = CvInvoke.GetStructuringElement(Emgu.CV.CvEnum.ElementShape.Rectangle, new Size(9, 9), new Point(3, 3));
                CvInvoke.MorphologyEx(dst, dst, MorphOp.Open, element, new Point(1, 1), 1, BorderType.Default, new MCvScalar(255, 0, 0, 255));
                //检测轮廓
                VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
                CvInvoke.FindContours(dst, contours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);
                //轮廓绘制
                CvInvoke.DrawContours(dst, contours, -1, new MCvScalar(120, 0, 0), 2);
                //遍历包围性轮廓的最大长度 
                double are = 0;
                double ares = 0;
                double count = 0;
                VectorOfPoint vp = new VectorOfPoint();
                for (int i = 0; i < contours.Size; i++)
                {
                    //计算包围性状的面积 
                    are = CvInvoke.ContourArea(contours[i], false);
                    if (are < 1500/*过滤掉面积小于3000的*/)
                    {
                        continue;
                    }
                    count++;
                    ares += are;
                }
                DebOutPut.DebLog("图像:   " + path);
                DebOutPut.DebLog("包围性状:矩形总面积:   " + ares.ToString());
                DebOutPut.DebLog("包围性状:矩形总个数:   " + count.ToString());
                vp.Dispose();
                contours.Dispose();
                element.Dispose();
                dst.Dispose();
                gray.Dispose();
                img.Dispose();
                return ares;
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
                return 0;
            }
        }


        /// <summary>
        /// 删除数据库中不存在的照片
        /// </summary>
        private void ImageClear()
        {
            DebOutPut.DebLog("正在查找多余图像");
            string QueryString = string.Format("select * from Record");
            DataTable CropDataTable = DB.QueryDatabase(QueryString).Tables[0];
            List<string> imageNames = new List<string>();
            for (int i = 0; i < CropDataTable.Rows.Count; i++)
            {
                DateTime dateTime = DateTime.Parse(CropDataTable.Rows[i].ItemArray[2].ToString());
                string name = dateTime.ToString("yyyyMMddHHmmss", System.Globalization.DateTimeFormatInfo.InvariantInfo) + ".bmp";
                imageNames.Add(name);
            }
            DebOutPut.DebLog("数据库中图像总量:" + imageNames.Count);
            string imagePath = Param.BasePath + "\\GrabImg\\";
            DirectoryInfo file = new DirectoryInfo(imagePath);
            FileInfo[] fileInfos = file.GetFiles("*.bmp");
            DebOutPut.DebLog("图库中图像总数:" + fileInfos.Length);
            int count = 0;
            for (int i = 0; i < fileInfos.Length; i++)
            {
                if (!imageNames.Contains(fileInfos[i].Name))
                {
                    count++;
                    if (!File.Exists(fileInfos[i].FullName))
                    {
                        DebOutPut.DebLog("文件不存在：" + fileInfos[i].FullName);
                        continue;
                    }
                    File.Delete(fileInfos[i].FullName);
                    DebOutPut.DebLog("删除多余图像:" + fileInfos[i].FullName);
                }
            }
            DebOutPut.DebLog("多余图像总数:" + count + "  已清除");
            CropDataTable.Dispose();
        }

        //判断各阶段时间是否到达
        private void timer2_Elapsed(object sender, EventArgs e)
        {
            try
            {
                if (Interlocked.Exchange(ref inTimer2, 1) == 0)
                {
                    DateTime dt = DateTime.Now;
                    //计算设备初始化时间是否超时
                    if (bStep == 0)
                    {
                        if (dt > (startTime.AddSeconds(180)))//设备初始化故障
                        {
                            if (FaultDiagnosis("A0"))
                            {
                                Cmd.CommunicateDp(0x12, 0);
                                Cmd.CommunicateDp(0x22, 0);
                                Cmd.CommunicateDp(0x32, 0);
                                Cmd.CommunicateDp(0x42, 0);
                                AbnormalStop();
                            }
                        }
                    }
                    //计算推片准备时间是否超时
                    else if (bStep == 1)
                    {
                        if (dt > (startTime.AddSeconds(60)))//推片准备故障
                        {
                            if (FaultDiagnosis("A1"))
                            {
                                Cmd.CommunicateDp(0x32, 0);//轴三停止运行
                                Cmd.CommunicateDp(0x41, 0);//轴四停止运行
                                AbnormalStop();
                            }
                        }
                    }
                    //计算轴二推片时间是否超时
                    else if (bStep == 2)
                    {
                        if (dt > (startTime.AddSeconds(60)))//推片故障
                        {
                            if (FaultDiagnosis("A2"))
                            {
                                Cmd.CommunicateDp(0x21, 0);//使轴二运动停止
                                AbnormalStop();
                            }
                        }
                    }
                    //计算轴二复位时间是否超时
                    else if (bStep == 3)
                    {
                        if (dt > (startTime.AddSeconds(60)))//复位故障
                        {
                            if (FaultDiagnosis("A3"))
                            {
                                Cmd.CommunicateDp(0x22, 0);//使轴二运动停止
                                AbnormalStop();
                            }
                        }
                    }
                    //计算轴一到粘附液位置时间是否超时
                    else if (bStep == 4)
                    {
                        if (dt > (startTime.AddSeconds(60)))//轴一到粘附液位置故障
                        {
                            if (FaultDiagnosis("A4"))
                            {
                                Cmd.CommunicateDp(0x11, 0);//轴一停止运行
                                AbnormalStop();
                            }
                        }
                    }
                    //计算滴加粘附液时间是否达标
                    else if (bStep == 5)
                    {
                        label30.Text = "滴液时间倒计时:\r\n" + Tools.GetNowTimeSpanSec(startTime.AddSeconds(int.Parse(Param.dropTime)), dt) + " 秒";
                        //滴加粘附液时间是否达标
                        if (dt > (startTime.AddSeconds(int.Parse(Param.dropTime))))
                        {
                            label30.Text = "无数据";
                            if (!isLongRangeDebug)
                            {
                                Timer2Stop();
                                startTimer1Time = DateTime.Now;
                                timer1.Start();
                            }
                        }
                    }
                    //计算轴一到风机位置时间是否超时
                    else if (bStep == 6)
                    {
                        if (dt > (startTime.AddSeconds(60)))//轴一到采集位置故障
                        {
                            if (FaultDiagnosis("A5"))
                            {
                                Cmd.CommunicateDp(0x11, 0);//轴一停止运行
                                AbnormalStop();
                            }
                        }
                    }
                    //计算风机开启时间是否达到
                    else if (bStep == 7)
                    {
                        double seconds = Convert.ToInt16(Param.FanMinutes) * 60;
                        label30.Text = "采集时间倒计时:\r\n" + Tools.GetNowTimeSpanSec(startTime.AddSeconds(seconds), dt) + " 秒";
                        //恒定
                        if (Param.FanMode == "0")
                        {
                            if (dt > (startTime.AddSeconds(seconds)))
                            {
                                label30.Text = "无数据";
                                DebOutPut.DebLog("采集时间已达标");
                                startTimer1Time = DateTime.Now;
                                timer1.Start();
                                Timer2Stop();
                            }
                        }
                        //双值
                        else if (Param.FanMode == "1")
                        {
                            double middle = double.Parse(Math.Round(((decimal)seconds / 2), 2).ToString());
                            if (dt > (startTime.AddSeconds(middle)))
                            {
                                Cmd.CommunicateDp(0x91, Convert.ToInt16(Param.FanStrengthMin));
                                if (dt > (startTime.AddSeconds(seconds)))
                                {
                                    label30.Text = "无数据";
                                    DebOutPut.DebLog("采集时间已达标");
                                    startTimer1Time = DateTime.Now;
                                    timer1.Start();
                                    Timer2Stop();
                                }
                            }
                        }
                    }
                    //计算轴一到培养液位置时间是否超时
                    else if (bStep == 9)
                    {
                        if (dt > (startTime.AddSeconds(60)))//轴一到培养位置故障
                        {
                            if (FaultDiagnosis("A6"))
                            {
                                Cmd.CommunicateDp(0x11, 0);//轴一停止运行
                                AbnormalStop();
                            }
                        }
                    }
                    //计算滴加培养液时间是否达到
                    else if (bStep == 10)
                    {
                        label30.Text = "培养时间倒计时:\r\n" + Tools.GetNowTimeSpanSec((startTime.AddSeconds(int.Parse(Param.dropTime))).AddMinutes(Convert.ToInt16(Param.peiyangtime)), dt) + " 秒\r\n(含" + Param.dropTime + "秒滴液时间)";
                        //滴加培养液时间是否达标
                        if (dt > (startTime.AddSeconds(int.Parse(Param.dropTime))))
                        {
                            //培养时间是否达标
                            if (dt > ((startTime.AddSeconds(int.Parse(Param.dropTime))).AddMinutes(Convert.ToInt16(Param.peiyangtime))))
                            {
                                label30.Text = "无数据";
                                DebOutPut.DebLog("培养时间已达标");
                                if (!isLongRangeDebug)
                                {
                                    Timer2Stop();
                                    startTimer1Time = DateTime.Now;
                                    timer1.Start();
                                }
                            }

                        }
                    }
                    //计算轴一到拍照位置时间是否超时
                    else if (bStep == 11)
                    {
                        if (dt > (startTime.AddSeconds(60)))//复位故障
                        {
                            if (FaultDiagnosis("A7"))
                            {
                                Cmd.CommunicateDp(0x11, 0);//使轴一运动停止
                                AbnormalStop();
                            }
                        }
                    }
                    //计算回收片时间是否超时
                    else if (bStep == 12)
                    {
                        if (dt > (startTime.AddSeconds(180)))//回收片故障
                        {
                            if (FaultDiagnosis("A8"))
                            {
                                Cmd.CommunicateDp(0x32, 0);
                                Cmd.CommunicateDp(0x42, 0);
                                AbnormalStop();
                            }
                        }
                    }
                    //计算回归原点时间是否超时
                    else if (bStep == 13)
                    {
                        if (dt > (startTime.AddSeconds(180)))//回归原点故障
                        {
                            if (FaultDiagnosis("A9"))
                            {
                                Cmd.CommunicateDp(0x12, 0);
                                Cmd.CommunicateDp(0x22, 0);
                                Cmd.CommunicateDp(0x32, 0);
                                Cmd.CommunicateDp(0x42, 0);
                                AbnormalStop();
                            }
                        }
                    }
                    //计算拍照过程中轴三回到X8位置时间是否超时
                    else if (bStep == 14)
                    {
                        if (dt > (startTime.AddSeconds(60)))//轴三对焦
                        {
                            if (FaultDiagnosis("A7"))
                            {
                                Cmd.CommunicateDp(0x32, 0);
                                AbnormalStop();
                            }
                        }
                    }
                    //计算拍照过程中轴三回到X9位置时间是否超时
                    else if (bStep == 15)
                    {
                        if (dt > (startTime.AddSeconds(60)))//轴三对焦
                        {
                            if (FaultDiagnosis("A7"))
                            {
                                Cmd.CommunicateDp(0x31, 0);
                                AbnormalStop();
                            }
                        }
                    }
                    Interlocked.Exchange(ref inTimer2, 0);
                }
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }

        /// <summary>
        /// 设备出现故障，将异常停止
        /// </summary>
        private void AbnormalStop()
        {
            DebOutPut.DebLog("bStep赋值：" + bStep);
            DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, "bStep赋值：" + bStep);
            bStep = -1;
            berror = true;
            Timer1Stop();
            Timer2Stop();
            Timer7Stop();
        }

        /// <summary>
        /// 故障诊断
        /// </summary>
        /// <param name="diagnosticModule">模块代码</param>
        /// <returns>true 存在故障，false 不存在故障</returns>
        private bool FaultDiagnosis(string diagnosticModule)
        {
            DebOutPut.DebLog("开始故障诊断");
            string str = diagnosticModule;
            List<string> diagnosisResults = new List<string>();//诊断结果列表
            isReady();
            for (int i = 0; i < list.Count; i++)
                if (devstatus.bits[list[i]] == 0)
                    diagnosisResults.Add((list[i] + 1).ToString());
            if (diagnosisResults.Count == 0)
            {
                DebOutPut.DebLog("故障模块：" + diagnosticModule + " 诊断结果列表为空！");
                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "故障模块：" + diagnosticModule + " 诊断结果列表为空！");
                Timer2Stop();
                return false;
            }
            foreach (var item in diagnosisResults)
                str += item;
            statusInfo = "故障码:" + str;
            label18.Text = "故障码:" + str;
            label18.ForeColor = System.Drawing.Color.Red;
            DebOutPut.DebLog("故障码：" + str);
            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "故障码：" + str);
            return true;
        }

        /// <summary>
        /// 镜头电机故障诊断
        /// </summary>
        /// <param name="diagnosticModule"></param>
        private void FaultDiagnosis_(string diagnosticModule)
        {
            DebOutPut.DebLog("开始故障诊断");
            statusInfo = "故障码:" + diagnosticModule;
            label18.Text = "故障码:" + diagnosticModule;
            label18.ForeColor = System.Drawing.Color.Red;
            DebOutPut.DebLog("故障码：" + diagnosticModule);
            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "故障码：" + diagnosticModule);
        }


        /*
          定时器timer3 主要为设备启动初始化判断是否有载玻片及设备复位做准备：
        */
        private void timer3_Elapsed(object sender, EventArgs e)
        {
            try
            {
                if (Interlocked.Exchange(ref inTimer3, 1) == 0)
                {
                    if (!isReady())
                    {
                        if (serialPort1 == null || !serialPort1.IsOpen)
                        {
                            DebOutPut.DebLog("serialPort1未打开");
                            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "serialPort1未打开");
                            return;
                        }
                        DebOutPut.DebLog("未就位");
                        Interlocked.Exchange(ref inTimer3, 0);
                        return;
                    }
                    DebOutPut.DebLog("已就位");
                    locaiton = 11;
                    Timer3Stop();//查询状态成功，准备就绪
                    DebOutPut.DebLog("页面初始化标志：" + nFinishStep);
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "页面初始化标志：" + nFinishStep);
                    switch (nFinishStep)
                    {
                        case 0:
                            DebOutPut.DebLog("执行回收片！");
                            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "执行回收片！");
                            huishoupian();
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }

        }


        /*
         * 判断是否可以开始新的流程了
        */
        private void timer4_Elapsed(object sender, EventArgs e)
        {
            try
            {
                if (Interlocked.Exchange(ref inTimer4, 1) == 0)
                {
                    //DebOutPut.DebLog("timer4判断bStep值：" + bStep);
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "timer4判断bStep值：" + bStep);
                    if (bStep == 13 || bStep == 12 || bStep == 0)
                    {
                        if (berror == true)
                        {
                            DebOutPut.DebLog("设备故障，timer4被意外终止，berror：" + berror);
                            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "设备故障，timer4被意外终止，berror：" + berror);
                            Interlocked.Exchange(ref inTimer4, 0);
                            return;
                        }
                        doLiucheng();
                        Timer4Stop();
                    }
                    Interlocked.Exchange(ref inTimer4, 0);
                }
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }

        static readonly object UpdateLock = new object();
        //最新版本
        NewVersion newVersion = null;
        int timeDown;
        /// <summary>
        /// 版本检测
        /// </summary>
        private void VersionCheck()
        {
            lock (UpdateLock)
            {
                try
                {
                    //int a1 = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.Major;//主版本号
                    //int a3 = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.Minor;//次版本号
                    //int a5 = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.Build;//生成版本号
                    //int a6 = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.Revision;//修正版本号
                    //表名
                    string tableName = Param.Read_ConfigParam(configfileName, "bmob", "TableName");//BZ10
                                                                                                   //获取channel信息
                    string channelName = Param.Read_ConfigParam(configfileName, "bmob", "channel");// BZ10_标准版
                                                                                                   //当前程序集版本号
                    string currVersion = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.Revision.ToString();
                    //新版本
                    newVersion = new NewVersion();
                    //查找
                    BmobWindows bmob = new BmobWindows();
                    bmob.initialize("5c31b12cb34012459fc94587a95521f1", "19fe4a17fe69a82c4b41b181c06fe93f");
                    BmobDebug.Register(msg => { Debug.WriteLine(msg); });
                    BmobQuery query = new BmobQuery();
                    query.WhereEqualTo("channel", channelName);
                    //**解决Win7中问题：cn.bmob.exception.BmobException:基础连接已关闭：无法为SSL/TLS安全通道建立信任关系，响应内容为--->System.Net.WebException:基础连接已关闭：无法为SSL/TLS安全通道建立信任关系。
                    ServicePointManager.ServerCertificateValidationCallback = Callback;
                    bmob.Find<NewVersion>(tableName, query, (resp, exception) =>
                    {
                        if (exception != null)
                        {
                            DebOutPut.DebLog("远程服务器证书无效:" + exception.ToString());
                            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, exception.ToString());
                            timeDown = 15;
                            timer9.Start();
                            MessageBox.Show("更新失败，服务器证书无效！" + exception.ToString(), "倒计时:（" + timeDown + "）", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                            return;
                        }
                        List<NewVersion> list = resp.results;
                        for (int i = 0; i < list.Count; i++)
                        {
                            if (list[i].channel == channelName)
                            {
                                newVersion = list[i];
                            }
                        }
                        if (newVersion == null || int.Parse(newVersion.currVersion) == int.Parse(currVersion) || int.Parse(newVersion.currVersion) < int.Parse(currVersion))
                        {
                            timeDown = 15;
                            timer9.Start();
                            MessageBox.Show("已是最新版本！", "倒计时:（" + timeDown + "）", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                            return;
                        }
                        //强更新、弱更新判断
                        if (int.Parse(newVersion.currVersion) > int.Parse(currVersion))
                        {
                            if (newVersion.isForced.Get())
                            {
                                timeDown = 15;
                                timer9.Start();
                                DialogResult dialogResult = MessageBox.Show("软件有重大版本更新！点击“确定”开始下载更新！倒计时结束系统将自动完成本次更新！\r\n", "倒计时:（" + timeDown + "）", MessageBoxButtons.OKCancel, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
                                Download();
                            }
                            else if (!newVersion.isForced.Get())
                            {
                                timeDown = 15;
                                timer9.Start();
                                DialogResult dialogResult = MessageBox.Show("软件已有新版本！点击“确定”开始下载更新,点击“取消”放弃本次更新！倒计时结束系统将视为放弃本次更新！\r\n", "倒计时:（" + timeDown + "）", MessageBoxButtons.OKCancel, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
                                if (dialogResult == DialogResult.OK)
                                {
                                    Download();
                                }
                            }
                        }
                    });
                }
                catch (Exception ex)
                {
                    DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
                    DebOutPut.DebLog(ex.ToString());
                    timeDown = 15;
                    timer9.Start();
                    MessageBox.Show("更新失败！" + ex.ToString(), "倒计时:（" + timeDown + "）", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                }
            }
        }
        private static bool Callback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        /// <summary>
        /// 下载
        /// </summary>
        private void Download()
        {
            try
            {
                if (newVersion.exeUrl == null || newVersion.exeUrl == "")
                {
                    DebOutPut.DebLog("更新地址为空");
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "更新地址为空");
                    timeDown = 15;
                    timer9.Start();
                    MessageBox.Show("更新失败，更新地址为空！", "倒计时:（" + timeDown + "）", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                    return;
                }
                string address = newVersion.exeUrl;
                WebClient webClient = new WebClient();
                //下载安装包
                webClient.DownloadFile(address, newVersion.exeName);
                //静默安装
                Install();

            }
            catch (Exception ex)
            {
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
                DebOutPut.DebLog(ex.ToString());
                timeDown = 15;
                timer9.Start();
                MessageBox.Show("更新失败！" + ex.ToString(), "倒计时:（" + timeDown + "）", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
            }

        }

        /// <summary>
        /// 安装
        /// </summary>
        private void Install()
        {
            try
            {
                string path = Application.StartupPath + "\\" + newVersion.exeName;
                if (!File.Exists(path))
                {
                    DebOutPut.DebLog("安装包不存在：" + "安装包下载地址：" + newVersion.exeUrl);
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "安装包不存在：" + "安装包下载地址：" + newVersion.exeUrl);
                    timeDown = 15;
                    timer9.Start();
                    MessageBox.Show("安装包不存在！" + "安装包下载地址：" + newVersion.exeUrl, "倒计时:（" + timeDown + "）", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                    return;
                }
                string Md5Hash = GetMD5HashFromFile(path);
                //效验MD5值
                if (Md5Hash.ToLower() == newVersion.md5.ToLower())
                {
                    IntPtr retint = Win32API.ShellExecute(IntPtr.Zero, "open", path, " /S", "", ShellExecute_ShowCommands.SW_SHOWNORMAL);//@/S  静默安装
                    Thread.Sleep(1000);
                    System.Environment.Exit(0);
                }
                else
                {
                    timeDown = 15;
                    timer9.Start();
                    MessageBox.Show("更新失败，软件包不完整，请清理IE缓存后重新更新，请联系管理员确认！\r\n", "倒计时:（" + timeDown + "）", MessageBoxButtons.OKCancel, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
                    DebOutPut.DebLog("更新失败，软件包不完整，请清理IE缓存后重新更新！");
                }
            }
            catch (Exception ex)
            {
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
                DebOutPut.DebLog(ex.ToString());
                timeDown = 15;
                timer9.Start();
                MessageBox.Show("更新失败！" + ex.ToString(), "倒计时:（" + timeDown + "）", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
            }
        }

        /// <summary>
        /// 获取文件的MD5码
        /// </summary>
        /// <param name="fileName">传入的文件名（含路径及后缀名）</param>
        /// <returns></returns>
        private string GetMD5HashFromFile(string fileName)
        {
            try
            {
                FileStream file = new FileStream(fileName, System.IO.FileMode.Open);
                MD5 md5 = new MD5CryptoServiceProvider();
                byte[] retVal = md5.ComputeHash(file);
                file.Close();
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                timeDown = 15;
                timer9.Start();
                MessageBox.Show("MD5校验失败！" + ex.ToString() + " 安装包下载地址：" + newVersion.exeUrl, "倒计时:（" + timeDown + "）", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                return "";
            }
        }

        /// <summary>
        /// MESSAGEBOX窗口定时关闭
        /// </summary>
        private void timer9_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Interlocked.Exchange(ref inTimer9, 1) == 0)
            {
                KillMessageBox(sender);
                Interlocked.Exchange(ref inTimer9, 0);
            }
        }
        private void KillMessageBox(object sender)
        {
            try
            {
                //按照MessageBox的标题，找到MessageBox的窗口 
                IntPtr ptr = Win32API.FindWindow(null, "倒计时:（" + timeDown + "）");
                timeDown--;
                Win32API.SetWindowText(ptr, "倒计时:（" + timeDown + "）");
                if (timeDown == 0)
                {
                    if (ptr != IntPtr.Zero)
                    {
                        //找到则关闭MessageBox窗口
                        Win32API.PostMessage(ptr, Win32API.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                        ((System.Timers.Timer)sender).Stop();
                    }
                }
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }



        private void DeleteButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (listView1.SelectedItems.Count <= 0)
                {
                    MessageBox.Show("请选择要删除的数据项！");
                    return;
                }
                else
                {
                    string time = "";
                    int DeleteNum = 0;
                    if (MessageBox.Show("确定要删除选中的数据吗？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                    {
                        time = (String)listView1.SelectedItems[0].Text;
                        string sql = "delete from Record where CollectTime='" + time + "'";

                        int a = DB.updateDatabase(sql);
                        DeleteNum += a;
                    }
                    if (DeleteNum == listView1.SelectedItems.Count)
                    {
                        string selectPicPath = Path.Combine(Application.StartupPath, "GrabImg");
                        string selectPicName = time.Replace("/", "").Replace("-", "").Replace(" ", "").Replace(":", "");
                        selectPicPath = Path.Combine(selectPicPath, selectPicName + ".bmp");
                        MessageBox.Show(this, "删除数据成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        UpdateListBoxItem();
                        if (File.Exists(selectPicPath))
                        {
                            File.Delete(selectPicPath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }

        }

        /// <summary>
        /// 首页
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button10_Click(object sender, EventArgs e)
        {
            if (pageCount > 1)
            {
                currentPage = 1;
                button10.Enabled = false;
                UpdateListBoxItem();
            }

        }

        /// <summary>
        /// 尾页
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button11_Click(object sender, EventArgs e)
        {
            if (pageCount > 1)
            {
                currentPage = pageCount;
                button11.Enabled = false;
                UpdateListBoxItem();
            }
        }

        /// <summary>
        /// 上一页
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button12_Click(object sender, EventArgs e)
        {
            if (currentPage > 1)
            {
                currentPage--;
                prepage.Enabled = false;
                UpdateListBoxItem();
            }
        }

        /// <summary>
        /// 下一页
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button13_Click(object sender, EventArgs e)
        {
            if (currentPage < pageCount)
            {
                currentPage++;
                nextpage.Enabled = false;
                UpdateListBoxItem();
            }
        }
        /// <summary>
        /// 锁
        /// </summary>
        static readonly object ImageLookLock = new object();
        private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            try
            {
                lock (ImageLookLock)
                {
                    ListViewHitTestInfo info = listView1.HitTest(e.X, e.Y);
                    if (info.Item != null)
                    {
                        String path = (String)listView1.SelectedItems[0].Tag;
                        PicturePreview pic = new PicturePreview();
                        pic.setImageSource(path);
                        pic.Show();
                    }
                }
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }

        /// <summary>
        /// 设备总览
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnDevOverview_Click(object sender, EventArgs e)
        {
            try
            {

                label2.ForeColor = System.Drawing.Color.Green;
                label3.ForeColor = System.Drawing.Color.White;
                label50.ForeColor = System.Drawing.Color.White;
                tabControl1.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }

        /// <summary>
        /// 数据查看
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnLookData_Click(object sender, EventArgs e)
        {
            try
            {
                BtnLookData.Enabled = false;
                label3.ForeColor = System.Drawing.Color.Green;
                label2.ForeColor = System.Drawing.Color.White;
                label50.ForeColor = System.Drawing.Color.White;
                tabControl1.SelectedIndex = 1;
                UpdateListBoxItem();
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }

        /// <summary>
        /// 系统设置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSystemSet_Click_1(object sender, EventArgs e)
        {
            try
            {
                label50.ForeColor = System.Drawing.Color.Green;
                label3.ForeColor = System.Drawing.Color.White;
                label2.ForeColor = System.Drawing.Color.White;
                tabControl1.SelectedIndex = 2;
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {

            Timer1Stop();
            Timer2Stop();
            Timer3Stop();
            Timer4Stop();
            Timer12Stop();

            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "设备被关闭！");
            SoftKeyboardCtrl.CloseWindow();
            System.Environment.Exit(0);
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        /// <summary>
        /// 自动对焦，将载波台调至镜头下方点击本按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button12_Click_1(object sender, EventArgs e)
        {
            try
            {
                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "点击事件_自动对焦");
                if (button12.Text == "自动对焦")
                {
                    button12.Text = "自动对焦中....";
                    String[] Portname = SerialPort.GetPortNames();
                    //主机通讯串口
                    if (Portname.Contains(Param.SerialPortName))
                    {
                        if (serialPort1.IsOpen)
                        {
                            serialPort1.Close();
                        }
                        serialPort1.PortName = Param.SerialPortName;
                        serialPort1.BaudRate = Convert.ToInt32(115200);
                        serialPort1.ReceivedBytesThreshold = 1;
                        serialPort1.Open();
                        Cmd.InitComm(serialPort1);
                        DebOutPut.DebLog("主机通讯串口打开成功");
                    }
                    else
                    {
                        DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "主机通讯串口未找到");
                        return;
                    }
                    if (Portname.Contains(Param.SerialPortCamera))
                    {
                        if (serialPort4.IsOpen)
                        {
                            serialPort4.Close();
                        }
                        serialPort4.PortName = Param.SerialPortCamera;
                        serialPort4.BaudRate = Convert.ToInt32(115200);
                        serialPort4.ReceivedBytesThreshold = 1;
                        serialPort4.Open();
                    }
                    else
                    {
                        DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "副控串口未找到");
                        if (int.Parse(Param.YJustRange) != 0 || int.Parse(Param.YNegaRange) != 0)
                        {
                            statusInfo = "副控串口打开故障";
                            label18.Text = "副控串口打开故障,请处理！";
                            DebOutPut.DebLog("副控串口打开故障");
                            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "副控串口打开故障");
                            label18.ForeColor = System.Drawing.Color.Red;
                            return;
                        }
                    }
                    DebOutPut.DebLog("打开补光灯");
                    int step = Convert.ToInt32("1200");
                    Cmd.CommunicateDp(0x92, step);
                    Thread.Sleep(1000);
                    DebOutPut.DebLog("开始自动对焦");
                    Thread myThread = new Thread(new ThreadStart(getImageOnLine));
                    myThread.IsBackground = true;
                    myThread.Start();
                }
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            Timer1Stop();
            bstop = true;
        }


        //如果模式为定时运行，运行此定时器
        private void timer5_Elapsed(object sender, EventArgs e)
        {
            try
            {
                if (Interlocked.Exchange(ref inTimer5, 1) == 0)
                {
                    System.DateTime currentTime = System.DateTime.Now;
                    if (!autoFlag && Param.RunFlag == "2") //工作时段运行
                    {
                        DebOutPut.DebLog("时段运行");
                        DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "时段运行");
                        bool isRun = false;
                        foreach (WorkTime work in Param.worllist)
                        {
                            string startTime = work.starth + ":" + work.startm;
                            string endTime = work.endh + ":" + work.endm;
                            string currTime = DateTime.Now.ToShortTimeString();//当前时间
                            isRun = GetTimeSpan(currTime, startTime, endTime);
                            if (isRun)
                            {
                                work.bstatus = true;
                                Timer5Stop();
                                timer4.Start();
                                DebOutPut.DebLog("Timer4_Start：开始时段运行");
                                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "Timer4_Start：开始时段运行");
                                label26.Text = "当前工作模式:" + "时段运行,运行时段为:" + work.toString();
                                break;
                            }
                        }
                        if (isRun == false)
                        {
                            label26.Text = "当前工作模式:" + "时段运行,不在时段范围内";
                        }
                    }
                    else  //定时运行
                    {
                        DebOutPut.DebLog("定时运行：isItExecuted：" + isItExecuted);
                        DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "定时运行：isItExecuted：" + isItExecuted);
                        if ((currentTime.Hour == Convert.ToInt16(Param.CollectHour)) && currentTime.Minute == Convert.ToInt16(Param.CollectMinute))
                        {
                            label26.Text = "当前工作模式:" + "定时运行_到点执行";
                            Timer5Stop();
                            timer4.Start();
                            isItExecuted = true;
                            DebOutPut.DebLog("Timer4_Start：到点即开始定时运行，是否已被执行：" + isItExecuted);
                            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "Timer4_Start：到点即开始定时运行，是否已被执行：" + isItExecuted);
                            MoveDateBase();
                        }
                        //超过了定时时间，但是不确定本次任务是否被执行过的情况下，此时timer5在执行
                        else if ((currentTime.Hour == Convert.ToInt16(Param.CollectHour) && currentTime.Minute > Convert.ToInt16(Param.CollectMinute)) || currentTime.Hour > Convert.ToInt16(Param.CollectHour))
                        {
                            if (!isItExecuted)
                            {
                                string sql = "select * from TimedTasks where currYear = '" + Param.recordYear + "' and currMonth='" + Param.recordMonth + "' and currDay='" + Param.recordDay + "' and runHour='" + Param.CollectHour + "' and runMinute='" + Param.CollectMinute + "'";
                                DataTable dataTable = DB.QueryDatabase(sql).Tables[0];
                                if (dataTable.Rows.Count > 0 && dataTable.Rows[0]["runFlag"].ToString() == "0")
                                {
                                    label26.Text = "当前工作模式:" + "定时运行_强制执行";
                                    Timer5Stop();
                                    timer4.Start();
                                    isItExecuted = true;
                                    DebOutPut.DebLog("Timer4_Start：到点未开始定时运行，强制使设备运行");
                                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "Timer4_Start：到点未开始定时运行，强制使设备运行");
                                    MoveDateBase();
                                }
                                else if (dataTable.Rows.Count > 0 && dataTable.Rows[0]["runFlag"].ToString() == "1")
                                {
                                    label26.Text = "当前工作模式:" + "定时运行_执行完毕";
                                    isItExecuted = true;
                                    DebOutPut.DebLog("该任务已被执行！");
                                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "该任务已被执行！");
                                }
                            }
                            else
                            {
                                label26.Text = "当前工作模式:" + "定时运行_执行完毕";
                            }
                        }
                        else
                        {
                            DebOutPut.DebLog("定时运行_等待执行！当前：" + label26.Text);
                            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "定时运行_等待执行！当前：" + label26.Text);
                            if (label26.Text != "当前工作模式:" + "定时运行_等待执行")
                            {
                                label26.Text = "当前工作模式:" + "定时运行_等待执行";
                            }

                        }
                    }
                    Interlocked.Exchange(ref inTimer5, 0);
                }

            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }

        }

        /// <summary>
        /// 修改数据库运行标志
        /// </summary>
        private void MoveDateBase()
        {
            DebOutPut.DebLog("运行模式：autoFlag：" + autoFlag + "  ，Param.RunFlag：" + Param.RunFlag);
            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "运行模式：autoFlag：" + autoFlag + "  ，Param.RunFlag：" + Param.RunFlag);
            //定时运行
            if (!autoFlag && Param.RunFlag == "1")
            {
                string currYear = DateTime.Now.Year.ToString();
                string currMonth = DateTime.Now.Month.ToString();
                string currDay = DateTime.Now.Day.ToString();
                string sql = "update TimedTasks Set runFlag='1',implementTime='" + DateTime.Now.ToString() + "' where currYear='" + currYear + "' and currMonth='" + currMonth + "' and currDay='" + currDay + "' and runHour='" + Param.CollectHour + "' and runMinute='" + Param.CollectMinute + "'";
                DB.updateDatabase(sql);
                DebOutPut.DebLog("更新任务标志成功，任务日期：" + Param.recordYear + "年" + Param.recordMonth + "月" + Param.recordDay + "日,定时：" + Param.CollectHour + "时" + Param.CollectMinute + "分");
                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "更新任务标志成功，任务日期：" + Param.recordYear + "年" + Param.recordMonth + "月" + Param.recordDay + "日,定时：" + Param.CollectHour + "时" + Param.CollectMinute + "分");
            }
        }

        /// <summary>
        /// 判断当前时间是否在工作时间段内
        /// </summary>
        /// <param name="timeStr">当前时间</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns></returns>
        private bool GetTimeSpan(string timeStr, string startTime, string endTime)
        {
            try
            {
                string _strWorkingDayAM = startTime;//工作时间上午08:30
                string _strWorkingDayPM = endTime;
                TimeSpan dspWorkingDayAM = DateTime.Parse(_strWorkingDayAM).TimeOfDay;
                TimeSpan dspWorkingDayPM = DateTime.Parse(_strWorkingDayPM).TimeOfDay;
                //string time1 = "2017-2-17 8:10:00";
                DateTime t1 = Convert.ToDateTime(timeStr);
                TimeSpan dspNow = t1.TimeOfDay;
                if (dspNow > dspWorkingDayAM && dspNow < dspWorkingDayPM)
                {
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
                return false;
            }

        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void radioButton4_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void button5_Click(object sender, EventArgs e)
        {

        }
        private void cb_switch_Click(object sender, EventArgs e)
        {

        }

        private void button5_Click_1(object sender, EventArgs e)
        {
            if (cb_switch.Checked == false)
            {
                Form1 form = new Form1();
                DialogResult dialogResult = form.ShowDialog();
                if (dialogResult != DialogResult.OK)
                    return;
                else
                    cb_switch.Checked = true;
            }
            else
                cb_switch.Checked = false;
        }


        private void cb_switch_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "点击事件_调试模式:" + cb_switch.Checked);
                if (cb_switch.Checked) //进入调试模式
                {
                    hideLocations();
                    bstop = true;
                    Timer1Stop();
                    Timer2Stop();
                    Timer3Stop();
                    Timer4Stop();
                    runmode = 1;//进入调试模式
                    label26.Text = "当前工作模式:" + "调试模式";
                    groupBox10.Enabled = true;

                }
                else    //进入正常模式
                {
                    DevStopWork();
                    runmode = 0;
                    bstop = false;
                    isSingleProcess = false;
                    Param.Init_Param(configfileName); //恢复正常参数
                    //RunFlag 0:自动运行 1:定时运行  2:分时运行 
                    if (Param.RunFlag == "0")
                    {
                        autoFlag = true;
                    }
                    else if (Param.RunFlag == "1" || Param.RunFlag == "2")
                    {
                        autoFlag = false;
                    }
                    if (SerialInit())
                    {
                        Cmd.InitComm(serialPort1);
                        if (autoFlag) //开机自动运行
                            label26.Text = "当前工作模式:" + "自动运行";
                        else if (!autoFlag && Param.RunFlag == "2")
                            label26.Text = "当前工作模式:" + "时段运行";
                        else if (!autoFlag && Param.RunFlag == "1")
                            label26.Text = "当前工作模式:" + "定时运行";
                        InitDev(); //判断设备是否就绪，可以开始新的流程了。
                        if (!timer3.Enabled)
                        {
                            if (autoFlag) //开机自动运行
                                timer4.Start();
                            else if (!autoFlag)
                            {
                                DebOutPut.DebLog("切换进正常模式_设备初始化");
                                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, "工作模式切换_设备初始化");
                                Cmd.CommunicateDp(0x10, 0);
                                DebOutPut.DebLog("切换进正常模式_轴一找原点");
                                Cmd.CommunicateDp(0x20, 0);
                                DebOutPut.DebLog("切换进正常模式_轴二找原点");
                                Cmd.CommunicateDp(0x30, 0);
                                DebOutPut.DebLog("切换进正常模式_轴三找原点");
                                Cmd.CommunicateDp(0x40, 0);
                                DebOutPut.DebLog("切换进正常模式_轴四找原点");
                                Cmd.CommunicateDp(0x93, 0);
                                DebOutPut.DebLog("切换进正常模式_关闭风机和补光");
                                list.Clear();
                                list.Add(0);
                                list.Add(5);
                                list.Add(7);
                                list.Add(9);
                                while (!isReady())
                                    Thread.Sleep(50);
                                setLocation(0);
                                timer5.Start();
                            }
                        }
                    }
                    else
                    {
                        label26.Text = "无数据";
                    }
                    //cb_switch.Enabled = true;
                    button4.Text = "循环运行";
                    button3.Text = "单流程运行";
                    groupBox10.Enabled = false;
                    //MessageBox.Show("正常模式切换成功！");
                }
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }

        /// <summary>
        /// 单流程运行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "点击事件_单流程运行");
            try
            {
                if (button3.Text == "运行中...")
                {
                    DebOutPut.DebLog("正在执行单流程运行模式");
                    MessageBox.Show("正在执行单流程运行模式，请在本次流程执行完毕之后再进行操作！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                    return;
                }
                if (button3.Text == "单流程运行")
                {
                    if (SerialInit())
                    {
                        autoFlag = false;
                        Cmd.InitComm(serialPort1);
                        InitDev(); //判断设备是否就绪，可以开始新的流程了。
                        bstop = false;
                        isSingleProcess = true;
                        DebugTimeAccelerate();
                        if (!timer3.Enabled)
                            timer4.Start();
                        label26.Text = "当前工作模式:" + "调试模式-单流程运行";
                        button3.Text = "运行中...";
                    }
                    else
                    {
                        DebOutPut.DebLog("串口打开失败");
                        MessageBox.Show("串口打开失败！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                    }
                }
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }


        /// <summary>
        /// 循环运行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "点击事件_循环运行");
                //正在执行单流程运行模式
                if (button4.Text == "运行中...")
                {
                    DebOutPut.DebLog("正在执行循环运行模式");
                    MessageBox.Show("正在执行循环运行模式，请先结束循环运行再进行操作！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                    return;
                }
                if (button4.Text == "循环运行")
                {
                    if (SerialInit())
                    {
                        bstop = false;
                        autoFlag = true;
                        Cmd.InitComm(serialPort1);
                        InitDev(); //判断设备是否就绪，可以开始新的流程了。
                        DebugTimeAccelerate();
                        if (!timer3.Enabled)
                            timer4.Start();
                        button4.Text = "运行中...";
                        label26.Text = "当前工作模式:" + "调试模式-循环运行";
                    }
                    else
                    {
                        DebOutPut.DebLog("串口打开失败");
                        MessageBox.Show("串口打开失败！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                    }

                }
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }

        }

        /// <summary>
        /// 调试模式时间加快
        /// </summary>
        private void DebugTimeAccelerate()
        {
            Param.FanMinutes = "1";//采集时间
            Param.FanStrength = "400";//采集强度
            Param.FanStrengthMax = "800";//最大采强
            Param.FanStrengthMin = "200";//最小采强
            Param.peiyangye = "400";//培养液量
            Param.peiyangtime = "1";//培养时间
            Param.MinSteps = "0";//原位起始
            Param.MaxSteps = "20";//原位终止
            Param.clearCount = "5";//原位选图
            if (Param.DripDevice == "0")
                Param.fanshilin = "400";//粘附液量
            else if (Param.DripDevice == "1")
                Param.fanshilin = "15";//粘附液量
            if (int.Parse(Param.rightMaxSteps) != 0 || int.Parse(Param.leftMaxSteps) != 0)
            {
                Param.XCorrecting = "1500";//横向矫正
                Param.leftMaxSteps = "1500";//横向正距
                Param.rightMaxSteps = "1500";//横向负距
                Param.tranStepsMin = "5";//横向正补
                Param.tranStepsMax = "5";//横向负补
                Param.moveInterval = "1500";//横向间隔
                Param.tranClearCount = "5";//横向首选
                Param.liftRightClearCount = "10";//横向复选
            }
            else
            {
                Param.XCorrecting = "0";//横向矫正
                Param.leftMaxSteps = "0";//横向正距
                Param.rightMaxSteps = "0";//横向负距
                Param.tranStepsMin = "0";//横向正补
                Param.tranStepsMax = "0";//横向负补
                Param.moveInterval = "0";//横向间隔
                Param.tranClearCount = "0";//横向首选
                Param.liftRightClearCount = "0";//横向复选
            }
            if (int.Parse(Param.YJustRange) != 0 || int.Parse(Param.YNegaRange) != 0)
            {
                Param.YCorrecting = "80";//纵向矫正
                Param.YJustRange = "40";//纵向正距
                Param.YNegaRange = "160";//纵向负距
                Param.YInterval = "100";//纵向间隔
                Param.YJustCom = "5";//纵向正补
                Param.YNageCom = "5";//纵向负补
                Param.YFirst = "5";//纵向首选
                Param.YCheck = "10";//纵向复选
            }
            else
            {
                Param.YCorrecting = "0";//纵向矫正
                Param.YJustRange = "0";//纵向正距
                Param.YNegaRange = "0";//纵向负距
                Param.YInterval = "0";//纵向间隔
                Param.YJustCom = "0";//纵向正补
                Param.YNageCom = "0";//纵向负补
                Param.YFirst = "0";//纵向首选
                Param.YCheck = "0";//纵向复选
            }
            DebOutPut.DebLog("设置测试参数成功!");
        }

        //停止设备运行
        private void stopDevRun()
        {
            Timer1Stop();
            Timer2Stop();
            Timer3Stop();
            Timer4Stop();
            Timer6Stop();
            serialPort1.Close();
            serialPort2.Close();
            serialPort3.Close();
            serialPort4.Close();
        }
        private void label1_Click(object sender, EventArgs e)
        {
            try
            {
                string debugWindowName = "调试模式 " + "V_" + System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString();
                IntPtr maindHwnd = IntPtr.Zero;
                maindHwnd = Win32API.FindWindow(null, debugWindowName);
                if (maindHwnd != IntPtr.Zero)
                {
                    Win32API.ShowWindow(maindHwnd, 9);
                    return;
                }
                if (MessageBox.Show("即将进入工程师调试模式，将中断设备现有流程，请确定是否进入？待退出调试调试模式后，请恢复设备正常工作模式！", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "点击事件_进入工程师调试模式");
                    cb_switch.Checked = true;
                    stopDevRun();//停止本机工作
                    Login login = new Login();
                    login.ShowDialog();

                }
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }

        }

        private void shutdown_PC()
        {
            try
            {
                System.Diagnostics.Process myProcess = new System.Diagnostics.Process();
                myProcess.StartInfo.FileName = "cmd.exe";//启动cmd命令
                myProcess.StartInfo.UseShellExecute = false;//是否使用系统外壳程序启动进程
                myProcess.StartInfo.RedirectStandardInput = true;//是否从流中读取
                myProcess.StartInfo.RedirectStandardOutput = true;//是否写入流
                myProcess.StartInfo.RedirectStandardError = true;//是否将错误信息写入流
                myProcess.StartInfo.CreateNoWindow = true;//是否在新窗口中启动进程
                myProcess.Start();//启动进程
                myProcess.StandardInput.WriteLine("shutdown -s -t 0");//执行关机命令
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }

        }

        /*GPS 定位串口*/
        private void serialPort2_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            try
            {
                byte[] data = new byte[200];
                int length = serialPort2.Read(data, 0, 200);
                string Read = Encoding.Default.GetString(data, 0, length);
                if (Read.Contains("$GPGLL"))
                {
                    string str = Read.Substring(Read.IndexOf("$GPGLL"));
                    string[] bby = str.Split(',');
                    if (bby.Count() > 5)
                    {

                        if (bby[1] == "")
                            return;
                        if (bby[3] == "")
                            return;
                        double dlat = Convert.ToDouble(bby[1]) * 0.01;
                        double dlon = Convert.ToDouble(bby[3]) * 0.01;
                        dlat = Math.Floor(dlat) + ((dlat - Math.Floor(dlat)) / 0.6);
                        dlon = Math.Floor(dlon) + ((dlon - Math.Floor(dlon)) / 0.6);
                        //MessageBox.Show("fsda");
                        label24.Text = "纬度:" + Convert.ToString(dlat);
                        label25.Text = "经度:" + Convert.ToString(dlon);
                        lat = dlat;
                        lon = dlon;
                        serialPort2.Close();
                        if (tcpclient != null)
                            tcpclient.sendLocation(dlat, dlon);
                    }
                }
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }
        }


        private void timer6_Elapsed(object sender, EventArgs e)
        {
            try
            {
                if (Interlocked.Exchange(ref inTimer6, 1) == 0)
                {
                    double val = 0;
                    val = getHj((byte)0x01);
                    if (val != 65535)
                    {
                        wd = val;
                        lb_wd.Text = String.Format("温度:{0:N1}℃", val);
                    }
                    else
                    {
                        lb_wd.Text = "温度:---";
                    }
                    val = getHj((byte)0x02);
                    if (val != 65535)
                    {
                        lb_sd.Text = String.Format("湿度:{0:N1}%", val);
                        sd = val;
                    }
                    else
                        lb_sd.Text = "湿度:---";

                    val = getHj((byte)0x04);
                    if (val != 65535)
                        lb_gz.Text = "光照:" + Convert.ToInt64(val).ToString() + " lux";
                    else
                        lb_gz.Text = "光照:---";
                    Interlocked.Exchange(ref inTimer6, 0);
                }
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
            }

        }
        Point mPoint;


        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            mPoint = new Point(e.X, e.Y);
        }

        private void panel1_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                this.Location = new Point(this.Location.X + e.X - mPoint.X, this.Location.Y + e.Y - mPoint.Y);
            }
        }



        private double getHj(byte addr)
        {
            try
            {
                double value = 0xFFFF;
                if (!serialPort3.IsOpen)
                    return value;
                long temp = 0L;
                int index = 0;
                serialPort3.DiscardInBuffer();
                byte[] rec = new byte[6];
                byte[] by = { 0xAA, 0x00, 0x64, 0x00, 0xA5 };
                by[3] = addr;

                for (int i = 0; i < 3; i++)
                {
                    serialPort3.Write(by, 0, by.Length);
                    DebOutPut.DebLog("发送:" + Cmd.byteToHexStr(by));
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.ComLog, "发送:" + Cmd.byteToHexStr(by));
                    Thread.Sleep(350);
                    if (serialPort3.BytesToRead <= 0)
                        index++;
                    else
                        break;
                }
                if (index == 3)
                {
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.ComLog, "发送:未收到终端回应！");
                    DebOutPut.DebLog("未收到终端回应！请重试!");
                    return value;
                }
                serialPort3.Read(rec, 0, 6);
                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.ComLog, "接收到:" + Cmd.byteToHexStr(rec));
                DebOutPut.DebLog("接收到:" + Cmd.byteToHexStr(rec));

                if (rec[0] == 0xAA && rec[1] == 0x64 && 0xA5 == rec[5])
                {
                    if (addr != 0x04)
                    {
                        if (addr == 0x01)
                        {
                            if (rec[3] >= 0x80)
                            {
                                rec[3] = (byte)(rec[3] ^ 0xFF);
                                rec[4] = (byte)(rec[4] ^ 0xFF);
                                int i3 = (rec[3] << 8) & 0xff00;
                                int i4 = rec[4] & 0xFF;
                                value = -((i3 | i4) * 0.1);
                            }
                            else
                                value = ((rec[3] << 8) + (rec[4] & 0xFF)) * 0.1;
                        }
                        else

                            value = ((rec[3] << 8) + (rec[4] & 0xFF)) * 0.1;
                    }
                    else
                    {
                        temp = ((rec[3] << 8) + (rec[4] & 0xFF));
                        if (temp >= 0x8000)
                            value = (temp - 0x8000) * 0x10;
                        else
                            value = temp;

                    }


                }
                return value;
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog(ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
                return 0xFFFF;
            }
        }

        #region 老版U口相机

        #region 开始呈像

        private void Start_OldGrabImage()
        {
            if (oldToupCam != null)
            {
                DebOutPut.DebLog("相机已经被打开");
                return;
            }
            ToupCam.Instance[] arr = ToupCam.Enum();
            if (arr.Length <= 0)
            {
                cameraErrStr = "未检索到相机";
                DebOutPut.DebLog("未检索到相机");
            }
            else
            {
                oldToupCam = new ToupCam();
                if (!oldToupCam.Open(arr[0].id))
                {
                    oldToupCam = null;
                    cameraErrStr = "相机打开失败";
                    DebOutPut.DebLog("相机打开失败");
                }
                else
                {
                    int width = 0, height = 0;
                    if (oldToupCam.get_Size(out width, out height))
                    {
                        oldBmp1 = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                        oldBmp2 = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                        oldEverror = new OldDelegateOnError(OldOnEventError);
                        oldEvexposure = new OldDelegateOnExposure(OldOnEventExposure);
                        oldEvimage = new OldDelegateOnImage(OldOnEventImage);
                        oldToupCam.put_ExpoCallback(new ToupCam.DelegateExposureCallback(OldDelegateOnExposureCallback));
                        if (!oldToupCam.StartPushMode(new ToupTek.ToupCam.DelegateDataCallback(OldDelegateOnDataCallback)))
                        {
                            OldOnEventError();
                            cameraErrStr = "相机取像失败";
                            DebOutPut.DebLog("相机取像失败");
                        }
                        else
                        {
                            bool autoexpo = true;
                            oldToupCam.get_AutoExpoEnable(out autoexpo);
                        }
                    }
                    oldToupCam.put_AutoExpoEnable(true);
                }
            }
        }

        private void OldOnEventError()
        {
            if (oldToupCam != null)
            {
                //oldToupCam.Close();
                oldToupCam = null;
            }
        }

        private void OldOnEventExposure()
        {
            if (oldToupCam != null)
            {
                uint nTime = 0;
                if (oldToupCam.get_ExpoTime(out nTime))
                {
                    // trackBar1.Value = (int)nTime;
                    // label1.Text = (nTime / 1000).ToString() + " ms";
                }
            }
        }

        private void OldOnEventImage(int[] roundrobin)
        {
            lock (oldLocker_)
            {
                if (roundrobin[0] == 1)
                    Cv_Main1.Image = oldBmp1;
                else
                    Cv_Main1.Image = oldBmp2;
                odlRoundrobin_ = roundrobin[0];
            }
            Cv_Main1.Invalidate();
        }

        void OldDelegateOnExposureCallback()
        {
            BeginInvoke(oldEvexposure);
        }

        void OldDelegateOnDataCallback(IntPtr pData, ref ToupCam.BITMAPINFOHEADER header, bool bSnap)
        {
            try
            {
                if (pData == null)
                {
                    /* error */
                    BeginInvoke(oldEverror);
                }
                else if (bSnap)
                {
                    Bitmap sbmp = new Bitmap(header.biWidth, header.biHeight, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                    BitmapData bmpdata = sbmp.LockBits(new Rectangle(0, 0, sbmp.Width, sbmp.Height), ImageLockMode.WriteOnly, sbmp.PixelFormat);
                    ToupCam.CopyMemory(bmpdata.Scan0, pData, header.biSizeImage);
                    sbmp.UnlockBits(bmpdata);

                    sbmp.Save("toupcamdemowinformcs3.jpg");
                }
                else
                {
                    int[] roundrobin = new int[] { 1 };
                    lock (oldLocker_)
                    {
                        /* use two round robin bitmap objects to hold the image data */
                        if (1 == odlRoundrobin_)
                        {
                            BitmapData bmpdata = oldBmp2.LockBits(new Rectangle(0, 0, header.biWidth, header.biHeight), ImageLockMode.WriteOnly, oldBmp2.PixelFormat);
                            ToupCam.CopyMemory(bmpdata.Scan0, pData, header.biSizeImage);
                            oldBmp2.UnlockBits(bmpdata);
                            roundrobin[0] = 2;
                        }
                        else
                        {
                            BitmapData bmpdata = oldBmp1.LockBits(new Rectangle(0, 0, header.biWidth, header.biHeight), ImageLockMode.WriteOnly, oldBmp1.PixelFormat);
                            ToupCam.CopyMemory(bmpdata.Scan0, pData, header.biSizeImage);
                            oldBmp1.UnlockBits(bmpdata);
                        }
                    }
                    BeginInvoke(oldEvimage, roundrobin);
                }
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog("相机连接异常，连接线可能脱落：" + ex.ToString());
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, "相机连接异常，连接线可能脱落：" + ex.ToString());
                OldOnEventError();
            }
        }

        /// <summary>
        /// 清空照片
        /// </summary>

        private void BtnClear_Click(object sender, EventArgs e)
        {
            bool isDeb = true;
            try
            {
                string str = "Delete * FROM Record";
                int ret = DB.updateDatabase(str);
                if (ret == -1)
                {
                    DebOutPut.DebLog("清空数据失败！");
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "清空数据失败！");
                    isDeb = false;
                    MessageBox.Show("清空数据失败！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                string imagePath = Param.BasePath + "\\GrabImg\\";
                DirectoryInfo file = new DirectoryInfo(imagePath);
                FileInfo[] fileInfos = file.GetFiles("*.bmp");
                DebOutPut.DebLog("图库中图像总数:" + fileInfos.Length);
                for (int i = 0; i < fileInfos.Length; i++)
                {
                    File.Delete(fileInfos[i].FullName);
                    DebOutPut.DebLog("删除图像:" + fileInfos[i].FullName);
                }
                DebOutPut.DebLog("所有图像已清空");
                UpdateListBoxItem();
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog("清空数据失败！");
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, ex.ToString());
                if (isDeb)
                {
                    MessageBox.Show("清空数据失败！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        /// <summary>
        /// 开启debugView模式
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void label27_DoubleClick(object sender, EventArgs e)
        {
            if (this.label27.Text.Trim() == "发布版")
            {
                this.label27.Text = "测试版";
                this.label27.ForeColor = System.Drawing.Color.Yellow;
                DebOutPut.isDebView = 1;
            }
            else if (this.label27.Text.Trim() == "测试版")
            {
                this.label27.Text = "发布版";
                this.label27.ForeColor = System.Drawing.Color.White;
                DebOutPut.isDebView = 0;
            }
        }

        #endregion

        #region 拍照

        private Image OldGetPic()
        {
            DateTime dt = DateTime.Now;//采集时间
            Bitmap img = null;
            this.Invoke((EventHandler)delegate
                {
                    img = new Bitmap(Cv_Main1.Image);
                });
            return img;
        }

        #endregion

        #endregion 老版U口相机

        /// <summary>
        /// 读取配置文件
        /// </summary>
        public void ReadConfig()
        {
            this.txt_IP.Text = Param.UploadIP;
            this.txt_Port.Text = Param.UploadPort;
            this.txt_Hour.Text = Param.CollectHour;
            this.txt_Mins.Text = Param.CollectMinute;
            this.TxtDevId.Text = Param.DeviceID;
            this.TxtRunMode.SelectedIndex = int.Parse(Param.RunFlag);
            this.TxtFanStartTime.Text = Param.FanMinutes;
            this.TxtFanStartStrength.Text = Param.FanStrength;
            this.TxtPeiyangyeCount.Text = Param.peiyangye;
            this.TxtFanshilinCount.Text = Param.fanshilin;
            this.TxtPeryangTime.Text = Param.peiyangtime;
            this.TxtCameraMaxSteps.Text = Param.MaxSteps;
            this.TxtCameraMinSteps.Text = Param.MinSteps;
            this.TxtRemain.Text = ((int.Parse(Param.remain) < 0) ? 0 : int.Parse(Param.remain)).ToString();
            this.txt_Hour.Text = Param.CollectHour;
            this.txt_Mins.Text = Param.CollectMinute;
            for (int i = 0; i < Param.worllist.Count; i++)
            {
                if (i + 1 == 1)
                {
                    this.TimeStartHour1.Value = Param.worllist[i].starth;
                    this.TimeStartMin1.Value = Param.worllist[i].startm;
                    this.TimeEndHour1.Value = Param.worllist[i].endh;
                    this.TimeEndMin1.Value = Param.worllist[i].endm;
                }
                else if (i + 1 == 2)
                {
                    this.TimeStartHour2.Value = Param.worllist[i].starth;
                    this.TimeStartMin2.Value = Param.worllist[i].startm;
                    this.TimeEndHour2.Value = Param.worllist[i].endh;
                    this.TimeEndMin2.Value = Param.worllist[i].endm;
                }
                else if (i + 1 == 3)
                {
                    this.TimeStartHour3.Value = Param.worllist[i].starth;
                    this.TimeStartMin3.Value = Param.worllist[i].startm;
                    this.TimeEndHour3.Value = Param.worllist[i].endh;
                    this.TimeEndMin3.Value = Param.worllist[i].endm;
                }
                else if (i + 1 == 4)
                {
                    this.TimeStartHour4.Value = Param.worllist[i].starth;
                    this.TimeStartMin4.Value = Param.worllist[i].startm;
                    this.TimeEndHour4.Value = Param.worllist[i].endh;
                    this.TimeEndMin4.Value = Param.worllist[i].endm;
                }
                else if (i + 1 == 5)
                {
                    this.TimeStartHour5.Value = Param.worllist[i].starth;
                    this.TimeStartMin5.Value = Param.worllist[i].startm;
                    this.TimeEndHour5.Value = Param.worllist[i].endh;
                    this.TimeEndMin5.Value = Param.worllist[i].endm;
                }

            }
            List<string> comList = Tools.SerialPortTesting();
            this.TxtMainCmd.Items.Clear();
            this.TxtGPSCmd.Items.Clear();
            this.TxtHJCmd.Items.Clear();
            this.TxtViceCmd.Items.Clear();
            for (int i = 0; comList != null && i < comList.Count; i++)
            {
                this.TxtMainCmd.Items.Add(comList[i]);
                this.TxtGPSCmd.Items.Add(comList[i]);
                this.TxtHJCmd.Items.Add(comList[i]);
                this.TxtViceCmd.Items.Add(comList[i]);
            }
            this.TxtMainCmd.Text = Param.SerialPortName;
            this.TxtGPSCmd.Text = Param.SerialPortGpsName;
            this.TxtHJCmd.Text = Param.SerialPortHjName;
            this.TxtViceCmd.Text = Param.SerialPortCamera;
            this.CbdataType.Text = Param.dataType;
            if (Param.cameraVersion == "1")
                this.CbCameraVersion.Text = "数字相机";
            else if (Param.cameraVersion == "2")
                this.CbCameraVersion.Text = "海康相机";

            if (Param.version == "0")
            {
                this.CbSysVersion.Text = "无水印版";
            }
            else if (Param.version == "1")
            {
                this.CbSysVersion.Text = "普通版";
            }
            else if (Param.version == "2")
            {
                this.CbSysVersion.Text = "定制版";
            }
            this.TxtClearCount.Text = Param.clearCount;
            this.TxtLeftMaxSteps.Text = Param.leftMaxSteps;
            this.TxtRightMaxSteps.Text = Param.rightMaxSteps;
            this.TxtLiftRightClearCount.Text = Param.liftRightClearCount;
            this.TxtMoveInterval.Text = Param.moveInterval;
            this.TxtCompensate.Text = Param.compensate.ToString();
            if (Param.mapSelectionScheme == "0")
                this.CbMapSelectionScheme.Text = "连续法";
            else if (Param.mapSelectionScheme == "1")
                this.CbMapSelectionScheme.Text = "间断法";
            if (Param.FanMode == "0")
                this.CbFanMode.Text = "恒定法";
            else if (Param.FanMode == "1")
                this.CbFanMode.Text = "双值法";
            this.TxtFanStrengthMax.Text = Param.FanStrengthMax;
            this.TxtFanStrengthMin.Text = Param.FanStrengthMin;
            this.TxttranStepsMin.Text = Param.tranStepsMin;
            this.TxttranStepsMax.Text = Param.tranStepsMax;
            this.TxttranClearCount.Text = Param.tranClearCount;
            this.TxtSlideCorrection.Text = Param.slideCorrection;
            this.TxtYJustRange.Text = Param.YJustRange;
            this.TxtYNegaRange.Text = Param.YNegaRange;
            this.TxtYInterval.Text = Param.YInterval;
            this.TxtYJustCom.Text = Param.YJustCom;
            this.TxtYNageCom.Text = Param.YNageCom;
            this.TxtYFirst.Text = Param.YFirst;
            this.TxtYCheck.Text = Param.YCheck;
            this.TxtXCorrecting.Text = Param.XCorrecting;
            this.TxtYCorrecting.Text = Param.YCorrecting;
            if (Param.isSoftKeyBoard == "0")
                this.CbisSoftKeyBoard.Text = "开启";
            else if (Param.isSoftKeyBoard == "1")
                this.CbisSoftKeyBoard.Text = "关闭";
            if (Param.DripDevice == "0")
                this.CbDripDevice.Text = "蠕动泵";
            else if (Param.DripDevice == "1")
                this.CbDripDevice.Text = "注射器";
            if (Param.recoveryDevice == "0")
                this.CbRecoveryDevice.Text = "50mm轴";
            else if (Param.recoveryDevice == "1")
                this.CbRecoveryDevice.Text = "70mm轴";
            this.TxtDropsTime.Text = Param.dropTime;
        }

        /// <summary>
        /// 修改
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnModify_Click(object sender, EventArgs e)
        {
            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "点击事件_系统设置修改");
            ConEnCtrl(true);
        }

        /// <summary>
        /// 应用
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnApply_Click(object sender, EventArgs e)
        {
            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "点击事件_系统设置应用");
            ConEnCtrl(false);
            //同步载玻片数量和工作模式
            string remain = Param.Read_ConfigParam(configfileName, "Config", "remain");
            int runFlag = int.Parse(Param.Read_ConfigParam(configfileName, "Config", "RunFlag"));
            if (remain != this.TxtRemain.Text.Trim() || runFlag != this.TxtRunMode.SelectedIndex)
            {
                //向服务器发送
                int remain1 = int.Parse(this.TxtRemain.Text.Trim());//载玻片数量
                int currRunMode = this.TxtRunMode.SelectedIndex;//当前工作模式
                if (remain1 < 0)
                {
                    remain1 = 0;
                }
                tcpclient.SendSlideGlassCount(134, "", remain1, currRunMode, (float)wd);
                Thread.Sleep(1000);
            }
            //需要重启的设置项 设备编号、日期格式、主串口、GPS串口、环境串口、相机版本、系统版本、运行模式
            string minCom = Param.Read_ConfigParam(configfileName, "Config", "SerialPort");
            string gpsCom = Param.Read_ConfigParam(configfileName, "Config", "SerialPortGps");
            string hjCom = Param.Read_ConfigParam(configfileName, "Config", "SerialPortHj");
            string caCom = Param.Read_ConfigParam(configfileName, "Config", "SerialPortCamera");
            string cameraVersion = Param.Read_ConfigParam(configfileName, "Config", "CameraVersion");
            string mapSelectionScheme = Param.Read_ConfigParam(configfileName, "Config", "MapSelectionScheme");
            string FanMode = Param.Read_ConfigParam(configfileName, "Config", "FanMode");
            string DripDevice = Param.Read_ConfigParam(configfileName, "Config", "DripDevice");
            string RecoveryDevice = Param.Read_ConfigParam(configfileName, "Config", "RecoveryDevice");

            string currDripDevice = "";
            if (this.CbDripDevice.Text == "蠕动泵")
                currDripDevice = "0";
            else if (this.CbDripDevice.Text == "注射器")
                currDripDevice = "1";
            string currFanMode = "";
            if (this.CbFanMode.Text == "恒定法")
                currFanMode = "0";
            else if (this.CbFanMode.Text == "双值法")
                currFanMode = "1";
            string currSelectCameraVersion = "";
            if (this.CbCameraVersion.Text == "数字相机")
                currSelectCameraVersion = "1";
            else if (this.CbCameraVersion.Text == "海康相机")
                currSelectCameraVersion = "2";
            string sysVersion = Param.Read_ConfigParam(configfileName, "Config", "version");
            string currSysVersion = "";

            if (this.CbSysVersion.Text == "无水印版")
                currSysVersion = "0";
            else if (this.CbSysVersion.Text == "普通版")
                currSysVersion = "1";
            else if (this.CbSysVersion.Text == "定制版")
                currSysVersion = "2";

            string runFlag1 = Param.Read_ConfigParam(configfileName, "Config", "RunFlag");
            string currRunFlag = "";
            if (this.TxtRunMode.Text == "自动")
                currRunFlag = "0";
            else if (this.TxtRunMode.Text == "定时")
                currRunFlag = "1";
            else if (this.TxtRunMode.Text == "时段")
                currRunFlag = "2";
            string currMapSelectionScheme = "";
            if (this.CbMapSelectionScheme.Text == "连续法")
                currMapSelectionScheme = "0";
            else if (this.CbMapSelectionScheme.Text == "间断法")
                currMapSelectionScheme = "1";
            string currRecoveryDevice = "";
            if (this.CbRecoveryDevice.Text == "50mm轴")
                currRecoveryDevice = "0";
            else if (this.CbRecoveryDevice.Text == "70mm轴")
                currRecoveryDevice = "1";
            if (this.TxtMainCmd.Text.Trim() != minCom || this.TxtGPSCmd.Text.Trim() != gpsCom || this.TxtHJCmd.Text.Trim() != hjCom || this.TxtViceCmd.Text.Trim() != caCom || cameraVersion != currSelectCameraVersion || sysVersion != currSysVersion || runFlag1 != currRunFlag || mapSelectionScheme != currMapSelectionScheme || FanMode != currFanMode || DripDevice != currDripDevice || RecoveryDevice != currRecoveryDevice)
            {
                SetConfig(configfileName);
                DialogResult dialogResult = MessageBox.Show("检测到您更改了系统关键性配置，将在系统重启之后生效。点击“确定”将立即重启本程序，点击“取消”请稍后手动重启！", "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                if (dialogResult == DialogResult.OK)
                {
                    Tools.RestStart();
                }
            }
            SetConfig(configfileName);
            Param.Init_Param(configfileName);
        }


        /// <summary>
        /// 控件是否启用
        /// </summary>
        /// <param name="isEnabel">是否启用</param>
        private void ConEnCtrl(bool isEnabled)
        {
            this.TxtDropsTime.Enabled = isEnabled;
            this.CbDripDevice.Enabled = isEnabled;
            this.CbisSoftKeyBoard.Enabled = isEnabled;
            this.TxtXCorrecting.Enabled = isEnabled;
            this.TxtYCorrecting.Enabled = isEnabled;
            this.TxtViceCmd.Enabled = isEnabled;
            this.CbFanMode.Enabled = isEnabled;
            this.TxtFanStrengthMax.Enabled = isEnabled;
            this.TxtFanStrengthMin.Enabled = isEnabled;
            this.TxttranStepsMin.Enabled = isEnabled;
            this.TxttranStepsMax.Enabled = isEnabled;
            this.TxttranClearCount.Enabled = isEnabled;
            //this.groupBox21.Enabled = isEnabled;
            this.TxtRunMode.Enabled = isEnabled;
            this.TxtMainCmd.Enabled = isEnabled;
            this.TxtGPSCmd.Enabled = isEnabled;
            this.TxtHJCmd.Enabled = isEnabled;
            this.TxtFanStartTime.Enabled = isEnabled;
            this.TxtFanStartStrength.Enabled = isEnabled;
            this.TxtPeiyangyeCount.Enabled = isEnabled;
            this.TxtFanshilinCount.Enabled = isEnabled;
            this.TxtPeryangTime.Enabled = isEnabled;
            this.TxtCameraMinSteps.Enabled = isEnabled;
            this.TxtCameraMaxSteps.Enabled = isEnabled;
            this.TxtRemain.Enabled = isEnabled;
            this.CbCameraVersion.Enabled = isEnabled;
            this.CbSysVersion.Enabled = isEnabled;
            this.TxtClearCount.Enabled = isEnabled;
            this.TxtLeftMaxSteps.Enabled = isEnabled;
            this.TxtRightMaxSteps.Enabled = isEnabled;
            this.TxtLiftRightClearCount.Enabled = isEnabled;
            this.TxtMoveInterval.Enabled = isEnabled;
            this.TxtCompensate.Enabled = isEnabled;
            this.CbMapSelectionScheme.Enabled = isEnabled;
            this.TxtSlideCorrection.Enabled = isEnabled;
            this.TxtYJustRange.Enabled = isEnabled;
            this.TxtYNegaRange.Enabled = isEnabled;
            this.TxtYInterval.Enabled = isEnabled;
            this.TxtYJustCom.Enabled = isEnabled;
            this.TxtYNageCom.Enabled = isEnabled;
            this.TxtYFirst.Enabled = isEnabled;
            this.TxtYCheck.Enabled = isEnabled;
            this.CbRecoveryDevice.Enabled = isEnabled;
        }
        /// <summary>
        /// 设置配置文件
        /// </summary>
        private void SetConfig(string configfileName)
        {
            Param.Set_ConfigParm(configfileName, "Config", "SerialPort", this.TxtMainCmd.Text);
            Param.Set_ConfigParm(configfileName, "Config", "SerialPortGps", this.TxtGPSCmd.Text);
            Param.Set_ConfigParm(configfileName, "Config", "SerialPortHj", this.TxtHJCmd.Text);
            Param.Set_ConfigParm(configfileName, "Config", "SerialPortCamera", this.TxtViceCmd.Text);
            string runflag = "";
            if (this.TxtRunMode.Text == "自动")
                runflag = "0";
            else if (this.TxtRunMode.Text == "定时")
                runflag = "1";
            else if (this.TxtRunMode.Text == "时段")
                runflag = "2";
            Param.Set_ConfigParm(configfileName, "Config", "RunFlag", runflag);
            Param.Set_ConfigParm(configfileName, "Config", "FanMinutes", this.TxtFanStartTime.Text);
            Param.Set_ConfigParm(configfileName, "Config", "FanStrength", this.TxtFanStartStrength.Text);
            Param.Set_ConfigParm(configfileName, "Config", "peiyangye", this.TxtPeiyangyeCount.Text);
            Param.Set_ConfigParm(configfileName, "Config", "fanshilin", this.TxtFanshilinCount.Text);
            Param.Set_ConfigParm(configfileName, "Config", "peiyangtime", this.TxtPeryangTime.Text);
            Param.Set_ConfigParm(configfileName, "Config", "MaxSteps", this.TxtCameraMaxSteps.Text);
            Param.Set_ConfigParm(configfileName, "Config", "MinSteps", this.TxtCameraMinSteps.Text);
            Param.Set_ConfigParm(configfileName, "Config", "remain", this.TxtRemain.Text);
            string currCaramVersion = "";
            if (this.CbCameraVersion.Text == "数字相机")
                currCaramVersion = "1";
            else if (this.CbCameraVersion.Text == "海康相机")
                currCaramVersion = "2";
            Param.Set_ConfigParm(configfileName, "Config", "CameraVersion", currCaramVersion);

            string currSysVersion = "";
            if (this.CbSysVersion.Text == "普通版")
                currSysVersion = "1";
            else if (this.CbSysVersion.Text == "定制版")
                currSysVersion = "2";
            else if (this.CbSysVersion.Text == "无水印版")
                currSysVersion = "0";

            Param.Set_ConfigParm(configfileName, "Config", "version", currSysVersion);
            Param.Set_ConfigParm(configfileName, "Config", "ClearCount", this.TxtClearCount.Text);

            Param.Set_ConfigParm(configfileName, "Config", "LeftMaxSteps", this.TxtLeftMaxSteps.Text);
            Param.Set_ConfigParm(configfileName, "Config", "RightMaxSteps", this.TxtRightMaxSteps.Text);
            Param.Set_ConfigParm(configfileName, "Config", "LiftRightClearCount", this.TxtLiftRightClearCount.Text);
            Param.Set_ConfigParm(configfileName, "Config", "LiftRightMoveInterval", this.TxtMoveInterval.Text);
            Param.Set_ConfigParm(configfileName, "Config", "Compensate", this.TxtCompensate.Text);
            string currMapSelectionScheme = "";
            if (this.CbMapSelectionScheme.Text == "连续法")
                currMapSelectionScheme = "0";
            else if (this.CbMapSelectionScheme.Text == "间断法")
                currMapSelectionScheme = "1";
            Param.Set_ConfigParm(configfileName, "Config", "MapSelectionScheme", currMapSelectionScheme);

            string currFanMode = "";
            if (this.CbFanMode.Text == "恒定法")
                currFanMode = "0";
            else if (this.CbFanMode.Text == "双值法")
                currFanMode = "1";
            Param.Set_ConfigParm(configfileName, "Config", "FanMode", currFanMode);
            Param.Set_ConfigParm(configfileName, "Config", "FanStrengthMax", this.TxtFanStrengthMax.Text);
            Param.Set_ConfigParm(configfileName, "Config", "FanStrengthMin", this.TxtFanStrengthMin.Text);
            Param.Set_ConfigParm(configfileName, "Config", "tranStepsMin", this.TxttranStepsMin.Text);
            Param.Set_ConfigParm(configfileName, "Config", "tranStepsMax", this.TxttranStepsMax.Text);
            Param.Set_ConfigParm(configfileName, "Config", "tranClearCount", this.TxttranClearCount.Text);
            Param.Set_ConfigParm(configfileName, "Config", "slideCorrection", this.TxtSlideCorrection.Text);
            Param.Set_ConfigParm(configfileName, "Config", "YJustRange", this.TxtYJustRange.Text);
            Param.Set_ConfigParm(configfileName, "Config", "YNegaRange", this.TxtYNegaRange.Text);
            Param.Set_ConfigParm(configfileName, "Config", "YInterval", this.TxtYInterval.Text);
            Param.Set_ConfigParm(configfileName, "Config", "YJustCom", this.TxtYJustCom.Text);
            Param.Set_ConfigParm(configfileName, "Config", "YNageCom", this.TxtYNageCom.Text);
            Param.Set_ConfigParm(configfileName, "Config", "YFirst", this.TxtYFirst.Text);
            Param.Set_ConfigParm(configfileName, "Config", "YCheck", this.TxtYCheck.Text);
            Param.Set_ConfigParm(configfileName, "Config", "XCorrecting", this.TxtXCorrecting.Text);
            Param.Set_ConfigParm(configfileName, "Config", "YCorrecting", this.TxtYCorrecting.Text);
            string currisSoftKeyBoadrd = "";
            if (this.CbisSoftKeyBoard.Text == "开启")
                currisSoftKeyBoadrd = "0";
            else if (this.CbisSoftKeyBoard.Text == "关闭")
                currisSoftKeyBoadrd = "1";
            Param.Set_ConfigParm(configfileName, "Config", "IsSoftKeyBoadrd", currisSoftKeyBoadrd);
            string currDripDevice = "";
            if (this.CbDripDevice.Text == "蠕动泵")
                currDripDevice = "0";
            else if (this.CbDripDevice.Text == "注射器")
                currDripDevice = "1";
            Param.Set_ConfigParm(configfileName, "Config", "DripDevice", currDripDevice);
            string currRecoveryDevice = "";
            if (this.CbRecoveryDevice.Text == "50mm轴")
                currRecoveryDevice = "0";
            else if (this.CbRecoveryDevice.Text == "70mm轴")
                currRecoveryDevice = "1";
            Param.Set_ConfigParm(configfileName, "Config", "RecoveryDevice", currRecoveryDevice);
            Param.Set_ConfigParm(configfileName, "Config", "DropTime", this.TxtDropsTime.Text);
            Param.Set_ConfigParm(configfileName, "Config", "SingleAspiration", Param.SingleAspiration);
            Param.Set_ConfigParm(configfileName, "Config", "AspirationCount", Param.AspirationCount);
            Param.Set_ConfigParm(configfileName, "Config", "AspirationIntervalMs", Param.AspirationIntervalMs);
            Param.Set_ConfigParm(configfileName, "Config", "isWinRestart", Param.isWinRestart);
            Param.Set_ConfigParm(configfileName, "Config", "isContinuousUpload", Param.isContinuousUpload);
            Param.Set_ConfigParm(configfileName, "Config", "SearchInterval", Param.SearchInterval);
        }



        private void TxtLiftRightClearCount_Click(object sender, EventArgs e)
        {
            if (Param.isSoftKeyBoard == "0")
                SoftKeyboardCtrl.OpenAndSetWindow();

        }

        private void TxtMainCmd_TextChanged(object sender, EventArgs e)
        {
            if (TxtMainCmd.Text == "")
            {
                TxtMainCmd.Text = "COM";
            }
        }

        private void TxtGPSCmd_TextChanged(object sender, EventArgs e)
        {
            if (TxtGPSCmd.Text == "")
            {
                TxtGPSCmd.Text = "COM";
            }
        }

        private void TxtHJCmd_TextChanged(object sender, EventArgs e)
        {
            if (TxtHJCmd.Text == "")
            {
                TxtHJCmd.Text = "COM";
            }
        }
        private void TxtViceCmd_TextChanged(object sender, EventArgs e)
        {
            if (TxtViceCmd.Text == "")
            {
                TxtViceCmd.Text = "COM";
            }
        }



        private void button50_Click(object sender, EventArgs e)
        {
            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "点击事件_时段设置修改");
            ControlEnd(true);
        }

        private void ControlEnd(bool isEnabled)
        {
            this.TimeStartHour1.Enabled = isEnabled;
            this.TimeStartHour2.Enabled = isEnabled;
            this.TimeStartHour3.Enabled = isEnabled;
            this.TimeStartHour4.Enabled = isEnabled;
            this.TimeStartHour5.Enabled = isEnabled;
            this.TimeEndHour1.Enabled = isEnabled;
            this.TimeEndHour2.Enabled = isEnabled;
            this.TimeEndHour3.Enabled = isEnabled;
            this.TimeEndHour4.Enabled = isEnabled;
            this.TimeEndHour5.Enabled = isEnabled;
            this.TimeStartMin1.Enabled = isEnabled;
            this.TimeStartMin2.Enabled = isEnabled;
            this.TimeStartMin3.Enabled = isEnabled;
            this.TimeStartMin4.Enabled = isEnabled;
            this.TimeStartMin5.Enabled = isEnabled;
            this.TimeEndMin1.Enabled = isEnabled;
            this.TimeEndMin2.Enabled = isEnabled;
            this.TimeEndMin3.Enabled = isEnabled;
            this.TimeEndMin4.Enabled = isEnabled;
            this.TimeEndMin5.Enabled = isEnabled;
        }

        private void button51_Click(object sender, EventArgs e)
        {
            DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "点击事件_时段设置应用");
            SetTimeDuan(configfileName);
            Param.Init_Param(configfileName);
            ControlEnd(false);
        }
        private void SetTimeDuan(string configfileName)
        {
            string work1 = this.TimeStartHour1.Value.ToString().PadLeft(2, '0') + ":" + this.TimeStartMin1.Value.ToString().PadLeft(2, '0') + "-" + this.TimeEndHour1.Value.ToString().PadLeft(2, '0') + ":" + this.TimeEndMin1.Value.ToString().PadLeft(2, '0');
            string work2 = this.TimeStartHour2.Value.ToString().PadLeft(2, '0') + ":" + this.TimeStartMin2.Value.ToString().PadLeft(2, '0') + "-" + this.TimeEndHour2.Value.ToString().PadLeft(2, '0') + ":" + this.TimeEndMin2.Value.ToString().PadLeft(2, '0');
            string work3 = this.TimeStartHour3.Value.ToString().PadLeft(2, '0') + ":" + this.TimeStartMin3.Value.ToString().PadLeft(2, '0') + "-" + this.TimeEndHour3.Value.ToString().PadLeft(2, '0') + ":" + this.TimeEndMin3.Value.ToString().PadLeft(2, '0');
            string work4 = this.TimeStartHour4.Value.ToString().PadLeft(2, '0') + ":" + this.TimeStartMin4.Value.ToString().PadLeft(2, '0') + "-" + this.TimeEndHour4.Value.ToString().PadLeft(2, '0') + ":" + this.TimeEndMin4.Value.ToString().PadLeft(2, '0');
            string work5 = this.TimeStartHour5.Value.ToString().PadLeft(2, '0') + ":" + this.TimeStartMin5.Value.ToString().PadLeft(2, '0') + "-" + this.TimeEndHour5.Value.ToString().PadLeft(2, '0') + ":" + this.TimeEndMin5.Value.ToString().PadLeft(2, '0');
            Param.Set_ConfigParm(configfileName, "Config", "work1", work1);
            Param.Set_ConfigParm(configfileName, "Config", "work2", work2);
            Param.Set_ConfigParm(configfileName, "Config", "work3", work3);
            Param.Set_ConfigParm(configfileName, "Config", "work4", work4);
            Param.Set_ConfigParm(configfileName, "Config", "work5", work5);
        }



        private void label50_Enter(object sender, EventArgs e)
        {
            label50.Enabled = false;
            label50.Enabled = true;
        }

        private void label2_Enter(object sender, EventArgs e)
        {
            label2.Enabled = false;
            label2.Enabled = true;
        }

        private void label3_Enter(object sender, EventArgs e)
        {
            label3.Enabled = false;
            label3.Enabled = true;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            SoftKeyboardCtrl.CloseWindow();
        }

        /// <summary>
        /// 恢复默认参数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                DialogResult dialogResult = MessageBox.Show("系统将重置所有已设置的参数，包含您在【设备总览】中设置的“参数设置”、“时段设置”，将在系统重启之后生效。点击“确定”将确认重置并在结束后立即重启本程序，点击“取消”将取消本次重置操作！\r\n\r友情提示：您正在执行的为不可逆操作，请谨慎处理！", "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                if (dialogResult == DialogResult.OK)
                {
                    DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "点击事件_参数重置操作");
                    string dataType = Param.Read_ConfigParam(configfileName, "Config", "dataType");
                    Param.Set_ConfigParm(configfileName, "Config", "UploadIP", "testfood.cn");
                    Param.Set_ConfigParm(configfileName, "Config", "UploadPort", "9126");
                    Param.Set_ConfigParm(configfileName, "Config", "DeviceID", "okq20201217001");
                    Param.Set_ConfigParm(configfileName, "Config", "CollectHour", "10");
                    Param.Set_ConfigParm(configfileName, "Config", "CollectMinute", "20");
                    Param.Set_ConfigParm(configfileName, "Config", "SerialPort", "COM2");
                    Param.Set_ConfigParm(configfileName, "Config", "SerialPortGps", "COM4");
                    Param.Set_ConfigParm(configfileName, "Config", "SerialPortHj", "COM5");
                    Param.Set_ConfigParm(configfileName, "Config", "SerialPortCamera", "COM3");
                    Param.Set_ConfigParm(configfileName, "Config", "RunFlag", "0");
                    Param.Set_ConfigParm(configfileName, "Config", "FanMode", "1");
                    Param.Set_ConfigParm(configfileName, "Config", "FanMinutes", "120");
                    Param.Set_ConfigParm(configfileName, "Config", "FanStrength", "400");
                    Param.Set_ConfigParm(configfileName, "Config", "FanStrengthMax", "800");
                    Param.Set_ConfigParm(configfileName, "Config", "FanStrengthMin", "200");
                    Param.Set_ConfigParm(configfileName, "Config", "peiyangye", "0");
                    Param.Set_ConfigParm(configfileName, "Config", "fanshilin", "15");
                    Param.Set_ConfigParm(configfileName, "Config", "peiyangtime", "0");
                    Param.Set_ConfigParm(configfileName, "Config", "MaxSteps", "700");
                    Param.Set_ConfigParm(configfileName, "Config", "MinSteps", "100");
                    Param.Set_ConfigParm(configfileName, "Config", "ClearCount", "5");
                    Param.Set_ConfigParm(configfileName, "Config", "tranStepsMin", "30");
                    Param.Set_ConfigParm(configfileName, "Config", "tranStepsMax", "30");
                    Param.Set_ConfigParm(configfileName, "Config", "tranClearCount", "5");
                    Param.Set_ConfigParm(configfileName, "Config", "LeftMaxSteps", "1500");
                    Param.Set_ConfigParm(configfileName, "Config", "RightMaxSteps", "1500");
                    Param.Set_ConfigParm(configfileName, "Config", "LiftRightClearCount", "15");
                    Param.Set_ConfigParm(configfileName, "Config", "LiftRightMoveInterval", "1000");
                    Param.Set_ConfigParm(configfileName, "Config", "work1", "00:00-00:00");
                    Param.Set_ConfigParm(configfileName, "Config", "work2", "00:00-00:00");
                    Param.Set_ConfigParm(configfileName, "Config", "work3", "00:00-00:00");
                    Param.Set_ConfigParm(configfileName, "Config", "work4", "00:00-00:00");
                    Param.Set_ConfigParm(configfileName, "Config", "work5", "00:00-00:00");
                    Param.Set_ConfigParm(configfileName, "Config", "remain", "300");
                    Param.Set_ConfigParm(configfileName, "Config", "slideCorrection", "0");
                    Param.Set_ConfigParm(configfileName, "Config", "dataType", "0");
                    Param.Set_ConfigParm(configfileName, "Config", "version", "0");
                    Param.Set_ConfigParm(configfileName, "Config", "CameraVersion", "2");
                    Param.Set_ConfigParm(configfileName, "Config", "Compensate", "-1");
                    Param.Set_ConfigParm(configfileName, "Config", "MapSelectionScheme", "1");
                    Param.Set_ConfigParm(configfileName, "Config", "YJustRange", "40");
                    Param.Set_ConfigParm(configfileName, "Config", "YNegaRange", "200");
                    Param.Set_ConfigParm(configfileName, "Config", "YInterval", "80");
                    Param.Set_ConfigParm(configfileName, "Config", "YJustCom", "30");
                    Param.Set_ConfigParm(configfileName, "Config", "YNageCom", "30");
                    Param.Set_ConfigParm(configfileName, "Config", "YFirst", "5");
                    Param.Set_ConfigParm(configfileName, "Config", "YCheck", "15");
                    Param.Set_ConfigParm(configfileName, "Config", "XCorrecting", "1500");
                    Param.Set_ConfigParm(configfileName, "Config", "YCorrecting", "80");
                    Param.Set_ConfigParm(configfileName, "Config", "IsSoftKeyBoadrd", "1");
                    Param.Set_ConfigParm(configfileName, "Config", "DripDevice", "1");
                    Param.Set_ConfigParm(configfileName, "Config", "RecoveryDevice", "1");
                    Param.Set_ConfigParm(configfileName, "Config", "DropTime", "120");
                    //修改数据库
                    if (dataType != "0")
                    {
                        ProgressForm progressForm = null;
                        string sql = "select * from Record";
                        DataTable dataTable = DB.QueryDatabase(sql).Tables[0];
                        if (dataTable.Rows.Count > 0)
                        {
                            progressForm = new ProgressForm(0, dataTable.Rows.Count);
                            progressForm.Show();
                        }
                        for (int i = 0; i < dataTable.Rows.Count; i++)
                        {
                            progressForm.AddProgress();
                            string collectTime = dataTable.Rows[i]["CollectTime"].ToString();
                            string newCollectTime = collectTime.Replace("/", "-");
                            sql = "update Record Set CollectTime = '" + newCollectTime + "' where CollectTime='" + collectTime + "'";
                            int x = DB.updateDatabase(sql);
                            if (x == -1)
                            {
                                DebOutPut.DebLog("检测到您更改了日期格式，日期格式更改之后，系统将自动更新数据库必要信息，此信息表示数据库信息更新失败！");
                                DebOutPut.WriteLog(LogType.Normal, LogDetailedType.Ordinary, "检测到您更改了日期格式，日期格式更改之后，系统将自动更新数据库必要信息，此信息表示数据库信息更新失败！");
                            }
                        }
                        if (progressForm != null)
                        {
                            progressForm.Close();
                            progressForm = null;
                        }
                    }
                    Thread.Sleep(500);
                    Tools.RestStart();
                }
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog("参数重置失败！");
                DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, "参数重置失败!" + ex.ToString());
            }
        }

        /// <summary>
        /// 手动上传数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button14_Click(object sender, EventArgs e)
        {
            Thread thread = new Thread(SendCollectionData);
            thread.IsBackground = true;
            thread.Start();
        }

        int aspirationCount = 0;
        /// <summary>
        /// 吸粘附液
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonX18_Click(object sender, EventArgs e)
        {
            if (buttonX18.Text == "吸粘附液")
            {
                buttonX19.Enabled = false;
                aspirationCount = 0;
                buttonX18.Text = "终止";
                buttonX18.BackColor = System.Drawing.Color.Red;
                Thread thread = new Thread(Aspiration);
                thread.IsBackground = true;
                thread.Start();
            }
            else if (buttonX18.Text == "终止")
            {
                buttonX18.Enabled = false;
                aspirationCount = int.Parse(Param.AspirationCount);
            }
        }

        /// <summary>
        /// 吸粘附液
        /// </summary>
        private void Aspiration()
        {
            while (aspirationCount < int.Parse(Param.AspirationCount))
            {
                PushingFluidMove(false, Param.SingleAspiration);
                Thread.Sleep(int.Parse(Param.AspirationIntervalMs));
                aspirationCount++;
            }
            aspirationCount = 0;
            buttonX18.Text = "吸粘附液";
            buttonX18.BackColor = System.Drawing.Color.Transparent;
            buttonX19.Enabled = true;
            buttonX18.Enabled = true;
        }

        /// <summary>
        /// 推粘附液
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonX19_Click(object sender, EventArgs e)
        {
            if (buttonX19.Text == "推粘附液")
            {
                buttonX18.Enabled = false;
                aspirationCount = 0;
                buttonX19.Text = "终止";
                buttonX19.BackColor = System.Drawing.Color.Red;
                Thread thread = new Thread(PushFluid);
                thread.IsBackground = true;
                thread.Start();
            }
            else if (buttonX19.Text == "终止")
            {
                buttonX19.Enabled = false;
                aspirationCount = int.Parse(Param.AspirationCount);
            }
        }

        /// <summary>
        /// 推粘附液
        /// </summary>
        private void PushFluid()
        {
            while (aspirationCount < int.Parse(Param.AspirationCount))
            {
                PushingFluidMove(true, Param.SingleAspiration);
                Thread.Sleep(int.Parse(Param.AspirationIntervalMs));
                aspirationCount++;
            }
            aspirationCount = 0;
            buttonX19.Text = "推粘附液";
            buttonX19.BackColor = System.Drawing.Color.Transparent;
            buttonX18.Enabled = true;
            buttonX19.Enabled = true;
        }


    }
}
