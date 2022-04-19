/// ***********************************************************************
///
/// =================================
/// CLR 版本    ：4.0.30319.42000
/// 项目名称    ：BZ10
/// 文件名称    ：AutoSizeFormClass.cs
/// 命名空间    ：BZ10
/// =================================
/// 创 建 者    ：赵新雨
/// 创建日期    ：2020/09/03 10:44:37 
/// 邮    箱    ：zhao2271154036@163.com
/// 功能描述    ：
/// 使用说明    ：
/// =================================
/// 修 改 者    ：
/// 修改日期    ：
/// 修改内容    ：
/// =================================
/// * Copyright @ OuKeQi 2020. All rights reserved.
/// ***********************************************************************
using BZ10.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BZ10
{
    /// <summary>
    /// 项目名称 ：BZ10
    /// 命名空间 ：BZ10
    /// 类 名 称 ：AutoSizeFormClass
    /// 作    者 ：赵新雨 
    /// 创建时间 ：2020/09/03 10:44:37 
    /// 更新时间 ：2020/09/03 10:44:37
    /// </summary>
    class AutoSizeFormClass
    {
        public struct controlRect
        {
            public int Left;
            public int Top;
            public int Width;
            public int Height;
        }
        public List<controlRect> oldCtrl = new List<controlRect>();
        int ctrlNo = 0;
        public void controllInitializeSize(Control mForm)
        {
            if (mForm == null)
                return;
            try
            {
                controlRect cR;
                cR.Left = mForm.Left;
                cR.Top = mForm.Top;
                cR.Width = mForm.Width;
                cR.Height = mForm.Height;
                oldCtrl.Add(cR);
                AddControl(mForm);
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog("UI自适应，控件初始化大小失败！" + ex.ToString());
                 DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, "UI自适应，控件初始化大小失败！" + ex.ToString());
            }

        }
        private void AddControl(Control ctl)
        {
            if (ctl == null)
                return;
            try
            {
                foreach (Control c in ctl.Controls)
                {
                    controlRect objCtrl;
                    objCtrl.Left = c.Left; objCtrl.Top = c.Top; objCtrl.Width = c.Width; objCtrl.Height = c.Height;
                    oldCtrl.Add(objCtrl);
                    if (c.Controls.Count > 0)
                        AddControl(c);
                }
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog("UI自适应，控件信息初始化添加失败！" + ex.ToString());
                 DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary,"UI自适应，控件信息初始化添加失败！" + ex.ToString());
            }

        }
        //(3.2)控件自适应大小,
        public void controlAutoSize(Control mForm)
        {
            if (mForm == null)
                return;
            try
            {
                if (ctrlNo == 0)
                {
                    controlRect cR;
                    cR.Left = 0; cR.Top = 0; cR.Width = mForm.PreferredSize.Width; cR.Height = mForm.PreferredSize.Height;
                    oldCtrl.Add(cR);
                    AddControl(mForm);
                }
                float wScale = (float)mForm.Width / (float)oldCtrl[0].Width;
                float hScale = (float)mForm.Height / (float)oldCtrl[0].Height;
                ctrlNo = 1;
                AutoScaleControl(mForm, wScale, hScale);
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog("UI自适应，控件自适应大小数据获取失败！" + ex.ToString());
                 DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary, "UI自适应，控件自适应大小数据获取失败！" + ex.ToString());
            }

        }
        private void AutoScaleControl(Control ctl, float wScale, float hScale)
        {
            if (ctl == null)
                return;
            try
            {
                int ctrLeft0, ctrTop0, ctrWidth0, ctrHeight0;
                foreach (Control c in ctl.Controls)
                {
                    ctrLeft0 = oldCtrl[ctrlNo].Left;
                    ctrTop0 = oldCtrl[ctrlNo].Top;
                    ctrWidth0 = oldCtrl[ctrlNo].Width;
                    ctrHeight0 = oldCtrl[ctrlNo].Height;
                    if (c.Name != "listView1")
                    {
                        c.Left = (int)((ctrLeft0) * wScale);
                        c.Top = (int)((ctrTop0) * hScale);
                        c.Width = (int)(ctrWidth0 * wScale);
                        c.Height = (int)(ctrHeight0 * hScale);
                    }
                    else if (c.Name == "listView1")
                    {
                        c.Left = (c.Parent.Width - c.Width) / 2;
                    }
                    ctrlNo++;
                    if (c.Controls.Count > 0)
                    {
                        AutoScaleControl(c, wScale, hScale);
                    }

                }
            }
            catch (Exception ex)
            {
                DebOutPut.DebLog("UI自适应，控件自适应大小失败！" + ex.ToString());
                 DebOutPut.WriteLog(LogType.Error, LogDetailedType.Ordinary,  "UI自适应，控件自适应大小失败！" + ex.ToString());
            }
        }
    }
}
