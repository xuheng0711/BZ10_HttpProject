/// ***********************************************************************
///
/// =================================
/// CLR 版本    ：4.0.30319.42000
/// 项目名称    ：BZ10.Common
/// 文件名称    ：PubField.cs
/// 命名空间    ：BZ10.Common
/// =================================
/// 创 建 者    ：ZhaoXinYu
/// 创建日期    ：2019/11/28 17:33:23 
/// 邮    箱    ：zhaoxinyu12580@163.com
/// 功能描述    ：公共字段
/// 使用说明    ：
/// =================================
/// 修 改 者    ：
/// 修改日期    ：
/// 修改内容    ：
/// =================================
/// * Copyright @ OuKeQi 2019. All rights reserved.
/// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace BZ10.Common
{
    /// <summary>
    /// 项目名称 ：BZ10.Common
    /// 命名空间 ：BZ10.Common
    /// 类 名 称 ：PubField
    /// 作    者 ：ZhaoXinYu 
    /// 创建时间 ：2019/11/28 17:33:23 
    /// 更新时间 ：2019/11/28 17:33:23
    /// </summary>
    public class PubField
    {
        public static Mutex mutex = null;
        /// <summary>
        /// 可执行文件目录，不包含可执行文件名称
        /// </summary>
        public static string pathBase = System.Windows.Forms.Application.StartupPath;
    }

    /// <summary>
    /// 日志类型
    /// </summary>
    public enum LogType
    {
        /// <summary>
        /// 正常日志
        /// </summary>
        Normal,
        /// <summary>
        /// 错误日志
        /// </summary>
        Error,
    }

    /// <summary>
    /// 日志详细类型
    /// </summary>
    public enum LogDetailedType
    {
        /// <summary>
        /// Socket日志
        /// </summary>
        KeepAliveLog,
        /// <summary>
        /// 串口日志
        /// </summary>
        ComLog,
        /// <summary>
        /// 普通日志
        /// </summary>
        Ordinary,
    }
}
