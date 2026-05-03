using JiXingFlashTool.Enums;
using JiXingFlashTool.Extensions;
using JiXingFlashTool.Model;
using JiXingFlashTool.Models;
using JiXingFlashTool.ObservableModel;
using JiXingFlashTool.Utils;
using JXAdbCore.Receivers;
using SharpDX.Direct3D9;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JiXingFlashTool.Services
{
    public class DeviceTaskService
    {
        private DeviceObservableModel ob;
        public DeviceObservableModel OB { get { return ob; } }

        public DeviceModel Device { get { return OB.Device; } }

        public DeviceTaskService(DeviceObservableModel ob) {
            this.ob = ob;
        }
        public void StartAdbCommand(AdbCommandModel adbModel) {
            new Thread(() =>
            {
                string name = adbModel.Name;
                if (adbModel.IsCustom) name = "自定义指令";
                OB.TaskDetailMessage = $"开始执行{name}";
                string cmd = adbModel.Command;
                if (adbModel.NeedRoot && Device.RootType == SystemRootType.Kernelsu) cmd = "su -c " + cmd; 
                AdbService.Instance.ExecuteShellCommand(ob.Device, cmd, null);
                OB.TaskDetailMessage = $"执行成功{name}";

            }).Start();
        }

        #region TWRP指令
        public void ExecuteTWRPCommand(CommandModel commandModel) {
            new Thread(() =>
            {
                switch ((TWRPCommandType)commandModel.Tag)
                {
                    case TWRPCommandType.RebootSystem: RebootToSystem(); break;
                    case TWRPCommandType.Wipe: Wipe(); break;
                    case TWRPCommandType.ClearSystem: DeleteSystem(); break;
                    case TWRPCommandType.Sideload: Sideload(); break;
                    case TWRPCommandType.FlashKernel: FlashKernel(commandModel.FilePath); break;
                    case TWRPCommandType.FlashFile: FlashFile(commandModel.FilePath); break;
                    case TWRPCommandType.RebootTWRP: RebootToTWRP(); break;
                    case TWRPCommandType.Format: FormatPhone(); break;
                    case TWRPCommandType.Decryption: Decryption(); break;
                    case TWRPCommandType.CheckDecryption: CheckDecryption(); break;
                    case TWRPCommandType.CheckCommandResult: CheckCommandResult(); break;
                    case TWRPCommandType.UpdateTWRP:UpdateTWRP(commandModel.FilePath); break;
                }

            }).Start();
        }
        public void Wipe() {
            if (!CheckIsInRecovery()) return;
            new Thread(() =>
            {
                try
                {
                    OB.TaskDetailMessage = $"执行双清";
                    //AdbService.Instance.ExecuteShellCommand(ob.Device, "twrp wipe dalvik", null);
                    //AdbService.Instance.ExecuteShellCommand(ob.Device, "twrp wipe cache", null);
                    //AdbService.Instance.ExecuteShellCommand(ob.Device, "twrp wipe data", null);
                    //      AdbService.Instance.ExecuteShellCommand(ob.Device, "twrp wipe dalvik && twrp wipe cache && twrp wipe data", null);
                    string cmd = $"-s {ob.Device.Serial} shell twrp wipe dalvik && twrp wipe cache && twrp wipe data";
                    AdbService.Instance.CMDExcute(cmd);
                    Console.WriteLine("双清完毕");
                    OB.TaskDetailMessage = $"执行双清完毕";
                }
                catch (Exception ex) {
                    ob.TaskDetailMessage = ex.Message;
                }

            }).Start();
        }
        public void DeleteSystem()
        {
            if (!CheckIsInRecovery()) return;
            new Thread(() =>
            {
                try
                {
                    OB.TaskDetailMessage = $"清除系统";
                    //AdbService.Instance.ExecuteShellCommand(ob.Device, "twrp wipe system", null);
                    String cmd = $"-s {Device.Serial} shell twrp wipe system";
                    AdbService.Instance.CMDExcute(cmd);
                    OB.TaskDetailMessage = $"清除系统完毕";
                }
                catch (Exception ex) {
                    OB.TaskDetailMessage = ex.Message;
                }

            }).Start();
        }
        public void FlashKernel(string kernelPath)
        {
            if (!CheckIsInRecovery()) return;
            string ufsName = GetUfsName();
            string bootName = GetBOOT();
            if (ufsName == string.Empty) {
                OB.TaskDetailMessage = $"不支持该型号";
                return;
            }
            new Thread(() =>
            {
                try
                {
                    OB.TaskDetailMessage = $"开始推送文件";
                    //推送文件
                    AdbService.Instance.Push(Device, kernelPath, StaticConstant.UpdateKernelPath);
                    if (!AdbService.Instance.FileExist(Device, StaticConstant.UpdateKernelPath))
                    {
                        OB.TaskDetailMessage = $"内核文件推送失败,请尝试重试";
                        return;
                    }

                    //执行更新指令
                    ConsoleOutputReceiver receiver = new ConsoleOutputReceiver();
                    AdbService.Instance.ExecuteShellCommand(Device, $"dd if=/sdcard/boot.img of=/dev/block/platform/{ufsName}/by-name/{bootName}", receiver);
                    string resultString = receiver.ToString();
                    if (resultString.IndexOf("records in") == -1 || resultString.IndexOf("records out") == -1) OB.TaskDetailMessage = "更新内核成功";
                    else OB.TaskDetailMessage = "更新内核失败";
                }
                catch (Exception ex) {
                    OB.TaskDetailMessage = ex.Message;
                }

            }).Start();
        }
        public void Sideload()
        {
            if (!CheckIsInRecovery()) return;
            new Thread(() =>
            {
                try
                {
                    OB.TaskDetailMessage = $"开启侧载";
                    //AdbService.Instance.ExecuteShellCommand(ob.Device, "twrp sideload", null);
                    AdbService.Instance.CMDExcute($"-s {Device.Serial} shell twrp sideload");
                    OB.TaskDetailMessage = $"执行完毕";
                }
                catch (Exception ex)
                {
                    ob.TaskDetailMessage = ex.Message;
                }

            }).Start();
        }
        public void RebootToSystem() {
            new Thread(() =>
            {
                try
                {
                    OB.TaskDetailMessage = $"重启到系统";
                    AdbService.Instance.ExecuteShellCommand(ob.Device, "twrp reboot system", null);
                    OB.TaskDetailMessage = $"执行完毕";
                }
                catch (Exception ex)
                {
                    ob.TaskDetailMessage = ex.Message;
                }

            }).Start();
        }
        public void RebootToTWRP()
        {
            new Thread(() =>
            {
                try
                {
                    OB.TaskDetailMessage = $"重启到TWRP";
                    AdbService.Instance.ExecuteShellCommand(ob.Device, "twrp reboot recovery", null);
                    OB.TaskDetailMessage = $"执行完毕";
                }
                catch (Exception ex)
                {
                    ob.TaskDetailMessage = ex.Message;
                }

            }).Start();
        }
        public void FlashFile(string filePath) {
            if (Device.State != JXAdbCore.Enums.DeviceState.Sideload)
            {
                OB.TaskDetailMessage = $"手机不在侧载模式,请先开启侧载";
                return;
            }
            new Thread(async () =>
            {
                try
                {
                    OB.TaskDetailMessage = $"刷入文件";
                    ConsoleOutputReceiver receiver = new ConsoleOutputReceiver();
                    string result = AdbService.Instance.SideloadFile(Device, filePath, (line) => {
                        if (!line.IsNull()) {
                            string percentage = line.MatchedPercentage();
                            if (!percentage.IsNull()) {
                                OB.TaskDetailMessage = $"{percentage}";
                            }
                        }
                        Console.WriteLine(line);
                    });
                    OB.TaskDetailMessage = "刷入成功";
                }
                catch (Exception ex)
                {
                    ob.TaskDetailMessage = ex.Message;
                }

            }).Start();
        }
        public void DeleteUpdateFile() {
            new Thread(() =>
            {
                try
                {
                    AdbService.Instance.ExecuteShellCommand(ob.Device, $"rm -rf {StaticConstant.UpdateSystemPath}", null);
                }
                catch (Exception ex)
                {
                    ob.TaskDetailMessage = ex.Message;
                }

            }).Start();
        }

        /// <summary>
        /// 格式化手机
        /// </summary>
        public void FormatPhone() {
            if (!CheckIsInRecovery()) return;
            string ufsName = GetUfsName();
            string userdata = GetUserdata();
            if (ufsName == string.Empty)
            {
                OB.TaskDetailMessage = $"不支持该型号";
                return;
            }
            new Thread(() =>
            {
                try
                {
                    OB.TaskDetailMessage = $"开始格式化";
                    Thread.Sleep(1000);
                    OB.TaskDetailMessage = $"卸载Data分区";
                    AdbService.Instance.CMDExcute($"-s {Device.Serial} shell twrp unmount data");
                    // AdbService.Instance.ExecuteShellCommand(Device, cmd, receiver);
                    // String cmd = $"-s {Device.Serial} shell make_ext4fs /dev/block/platform/{ufsName}/by-name/{userdata}";
                    //  AdbService.Instance.CMDExcute(cmd);
                    //string resultString = receiver.ToString();

                    string formatCmd = $"/dev/block/platform/{ufsName}/by-name/{userdata}";
                    if (Device.Name == DeviceModelEnum.dreamlte.ToString() || Device.Name == DeviceModelEnum.dream2lte.ToString()) formatCmd = "mke2fs " + formatCmd;
                    else formatCmd = $"make_ext4fs " + formatCmd;
                    formatCmd = $"-s {Device.Serial} shell {formatCmd}";
                    string result = AdbService.Instance.CMDExcute(formatCmd);
                    Thread.Sleep(1000);
                    AdbService.Instance.CMDExcute($"-s {Device.Serial} shell twrp mount data");
                    OB.TaskDetailMessage = $"格式化完成";
                }
                catch (Exception ex)
                {
                    OB.TaskDetailMessage = ex.Message;
                }

            }).Start();
        }

        private void Decryption()
        {
            try
            {
                OB.TaskDetailMessage = "加载分区";

                //先判断分期是否已经加载，如果没有加载就加载分期
                //加载vendroid
                AdbService.Instance.CMDExcute($"-s {Device.Serial} shell twrp mount vendor");

                ////判断/vendor/etc/是否存在
                if (AdbService.Instance.CMDExcute($"-s {Device.Serial} shell ls -l /vendor/etc/").IndexOf("No such file or directory") != -1)
                {
                    OB.TaskDetailMessage = "解密失败,加载分区失败";
                    return;
                }

                OB.TaskDetailMessage = "重写分区为读写";
                //加载读写
                AdbService.Instance.CMDExcute($"-s {Device.Serial} shell mount -o rw,remount /vendor");

                OB.TaskDetailMessage = "开始解密";
                //修改文件加密
                AdbService.Instance.CMDExcute($"-s {Device.Serial} shell sed -i 's/forceencrypt=footer,//g' /vendor/etc/fstab.qcom");

                //获取修改结果
                if (AdbService.Instance.CMDExcute($"-s {Device.Serial} shell cat /vendor/etc/fstab.qcom").IndexOf("forceencrypt=footer") != -1)
                {
                    OB.TaskDetailMessage = "解密失败,编辑分区失败";
                    return;
                }

                OB.TaskDetailMessage = "解密成功";
            }
            catch (Exception ex) {
                OB.TaskDetailMessage = ex.ToString();
            }
        }

        public void CheckDecryption() {
            OB.TaskDetailMessage = "获取解密状态";
            //加载vendroid
            //AdbService.Instance.ExecuteShellCommand(ob.Device, "twrp mount vendor",null);
            AdbService.Instance.CMDExcute($"-s {Device.Serial} shell twrp mount vendor");
            ////删除forceencrypt=footer,
            ////判断/vendor/etc/是否存在
            // AdbService.Instance.CMDExcute($"-s {Device.Serial} shell ls -l /vendor/etc/ ")
            if (!AdbService.Instance.FolderExist(ob.Device, "/vendor/etc/"))
            {
                OB.TaskDetailMessage = "获取解密失败,加载分区失败";
                return;
            }


            AdbService.Instance.CMDExcute($"-s {Device.Serial} shell mount -o rw,remount /vendor");
            if (!AdbService.Instance.FolderExist(ob.Device, "/vendor/etc/"))
            {
                OB.TaskDetailMessage = "获取解密失败,重载分区失败";
                return;
            }
            //读取文本并且判断是否存在
            string text = AdbService.Instance.Read(Device, "/vendor/etc/fstab.qcom");
            if (text.IndexOf("forceencrypt=footer") != -1)
            {
                OB.TaskDetailMessage = "获取解密失败,编辑分区失败";
                return;
            }
            OB.TaskDetailMessage = "已经解密成功";
        }

        private void CheckCommandResult() {
            OB.TaskDetailMessage = "获取指令结果";
            if (AdbService.Instance.CMDExcute($"-s {Device.Serial} shell echo $?").IndexOf("0") != -1)
            {
                OB.TaskDetailMessage = "成功";
            }
            else {
                ob.TaskDetailMessage = "失败";
            }
        }

        /// <summary>
        /// 更新TWRP
        /// </summary>
        private void UpdateTWRP(string filePath) {
            string recoveryPath = string.Empty;
            if (Device.Name == DeviceModelEnum.starqlte.ToString() || Device.Name == DeviceModelEnum.starqltechn.ToString())
            {
                recoveryPath = "/dev/block/platform/soc/1d84000.ufshc/by-name/recovery";
            }
            else if (Device.Name == DeviceModelEnum.dreamlte.ToString() || Device.Name == DeviceModelEnum.dream2lte.ToString()) {
                recoveryPath = "/dev/block/platform/11120000.ufs/by-name/RECOVERY";
            }
            if (recoveryPath.IsNull())
            {
                OB.TaskDetailMessage = "不支持该型号";
                return;
            }

            string twrpPhonePath = "/sdcard/twrp.img";

            OB.TaskDetailMessage = "传输文件";
            //先传输
            AdbService.Instance.Push(Device,filePath, twrpPhonePath);
            //校验MD5

            if (!FileUtil.CalculateMD5(filePath).Equals(AdbService.Instance.SumMD5(Device, twrpPhonePath))) {
                OB.TaskDetailMessage = "校验文件失败,请重新尝试";
                return;
            }

            OB.TaskDetailMessage = "刷新TWRP";
            string updateResult = AdbService.Instance.ExecuteRemoteCommand($"dd if={twrpPhonePath} of={recoveryPath}", Device);
            if (updateResult.IndexOf("records in") != -1 && updateResult.IndexOf("records out") != -1) {
                OB.TaskDetailMessage = "更新成功,重新进入TWRP生效";
                return;
            }

            OB.TaskDetailMessage = "更新失败";

        }

        #endregion

        #region 更新系统资料
        public void UpdateSystem(string filePath) {
            if (Device.State != JXAdbCore.Enums.DeviceState.Online)
            {
                OB.TaskDetailMessage = $"手机不在系统";
                return;
            }
            new Thread(() =>
            {
                try
                {
                    OB.TaskDetailMessage = $"推送更新文件";
                    AdbService.Instance.Push(Device, filePath, StaticConstant.UpdateSystemPath);
                    OB.TaskDetailMessage = "检查更新文件";

                    //验证文件是否存在
                    bool fileExist = false;
                    fileExist = AdbService.Instance.FileExist(Device, StaticConstant.UpdateSystemPath);
                    if (fileExist == false)
                    {
                        OB.TaskDetailMessage = "推送文件失败,请重试";
                        return;
                    }

                    //对比大小
                    try
                    {
                        long deviceFileSize = AdbService.Instance.FileSize(Device, StaticConstant.UpdateSystemPath);
                        long pcFileSize = new FileInfo(filePath).Length;
                        if (deviceFileSize != pcFileSize)
                        {
                            ob.TaskDetailMessage = "更新文件校验失败,请重试";
                            return;
                        }
                    }
                    catch
                    {
                        ob.TaskDetailMessage = "更新文件校验失败,请重试";
                        return;
                    }

                    if (Device.RootType == SystemRootType.Shell)
                    {

                        AdbService.Instance.CreateFile(Device, StaticConstant.UpdateSystemFileContext, StaticConstant.UpdateSystemCommandPath);
                        fileExist = AdbService.Instance.FileExist(Device, StaticConstant.UpdateSystemCommandPath);
                        if (fileExist == false)
                        {
                            OB.TaskDetailMessage = "推送文件失败,请重试";
                            return;
                        }
                    }
                    else if (Device.RootType == SystemRootType.Kernelsu)
                    {
                        string updateCommand = "/data/local/tmp/command";
                        //创建
                        bool createResult = AdbService.Instance.CreateFile(Device, StaticConstant.UpdateSystemFileContext, updateCommand);
                        AdbService.Instance.ExecuteRemoteCommand($"su -c mv {updateCommand} {StaticConstant.UpdateSystemCommandPath}",Device);
                        //然后移动
                        fileExist = AdbService.Instance.FileExist(Device, StaticConstant.UpdateSystemCommandPath);
                        if (fileExist == false)
                        {
                            OB.TaskDetailMessage = "推送文件失败,请重试";
                            return;
                        }
                    }
                    else {
                        OB.TaskDetailMessage = "没有合适的权限";
                        return;
                    }


                    //写入属性
                    AdbService.Instance.ExecuteShellCommand(Device, "setprop persist.vendor.recovery_update false", null);

                    OB.TaskDetailMessage = "推送文件完毕,进入recovery进行更新";
                    ConsoleOutputReceiver receiver = new ConsoleOutputReceiver();
                    AdbService.Instance.ExecuteShellCommand(Device, "reboot recovery", receiver);
                    if (receiver.ToString() != null && receiver.ToString().Length > 0) OB.TaskDetailMessage = $"执行重启动指令失败:{receiver.ToString()}";
                    else OB.TaskDetailMessage = "等待进入recovery自动更新";
                }
                catch (Exception ex)
                {
                    ob.TaskDetailMessage = ex.Message;
                }

            }).Start();
        }

        #endregion

        public void RestoreFactory() {
            if (Device.State != JXAdbCore.Enums.DeviceState.Online)
            {
                OB.TaskDetailMessage = $"手机不在系统";
                return;
            }
            new Thread(() =>
            {
                try
                {
                    OB.TaskDetailMessage = "开始还原任务";
                    Thread.Sleep(2 * 1000);

                    OB.TaskDetailMessage = "写入还原指令";
                    AdbService.Instance.CreateFile(Device, "--wipe_data\r\n--wipe_cache", StaticConstant.TWRPCommandPath);
                    if (AdbService.Instance.FileExist(Device, StaticConstant.TWRPCommandPath) == false)
                    {
                        OB.TaskDetailMessage = "写入指令失败,请重试";
                        return;
                    }

                    OB.TaskDetailMessage = "写入还原指令";
                    ConsoleOutputReceiver ctr = new ConsoleOutputReceiver();
                    AdbService.Instance.ExecuteShellCommand(Device, "reboot recovery", ctr);
                    string result = ctr.ToString();
                    if (result.IsNull()) OB.TaskDetailMessage = "等待手机恢复出厂设置完成";
                    else OB.TaskDetailMessage = "还原失败，请重新尝试";
                }
                catch (Exception ex)
                {
                    ob.TaskDetailMessage = ex.Message;
                }

            }).Start();
        }

        #region 内部私有函数

        private bool CheckIsInRecovery() {
            if (Device.State != JXAdbCore.Enums.DeviceState.Recovery)
            {
                OB.TaskDetailMessage = $"手机不在恢复模式";
                return false;
            }
            return true;
        }

        /// <summary>
        /// 获取当前手机的ufsName
        /// </summary>
        /// <returns></returns>
        private string GetUfsName() {
            string ufsName = string.Empty;
            if (Device.Model.IndexOf("G920") != -1)//S6
            {
                ufsName = "15570000.ufs";
            }
            else if (Device.Model.IndexOf("G930") != -1 || Device.Model.IndexOf("G935") != -1)//S7 
            {
                if (Device.Model.IndexOf("G9300") != -1 || Device.Model.IndexOf("G9308") != -1 || Device.Model.IndexOf("G9350") != -1 || Device.Model.IndexOf("G9358") != -1)//S7 China
                {
                    ufsName = "624000.ufshc";
                }
                else
                {
                    ufsName = "155a0000.ufs";
                }
            }
            else if (Device.Model.IndexOf("G950") != -1 || Device.Model.IndexOf("G955") != -1 || Device.Model.IndexOf("N950") != -1 || Device.Model.IndexOf("Galaxy_S8") != -1)//S8
            {
                ufsName = "11120000.ufs";
            }
            else if (Device.Model.IndexOf("G960") != -1) {
                ufsName = "soc/1d84000.ufshc";
            }
            else if(Device.Model.IndexOf("hero2lte") != -1 || Device.Model.IndexOf("hero2lte") != -1){
                ufsName = "155a0000.ufs";
            }
            
            return ufsName;
        }

        private string GetUserdata() {
            string userdata = string.Empty;
            if (Device.Model.IndexOf("G920") != -1)//S6
            {
                userdata = "userdata";
            }
            else if (Device.Model.IndexOf("G930") != -1 || Device.Model.IndexOf("G935") != -1)//S7 
            {
                if (Device.Model.IndexOf("G9300") != -1 || Device.Model.IndexOf("G9308") != -1 || Device.Model.IndexOf("G9350") != -1 || Device.Model.IndexOf("G9358") != -1)//S7 China
                {
                    userdata = "userdata";
                }
                else
                {
                    userdata = "USERDATA";
                }
            }
            else if (Device.Model.IndexOf("G950") != -1 || Device.Model.IndexOf("G955") != -1 || Device.Model.IndexOf("N950") != -1 || Device.Model.IndexOf("Galaxy_S8") != -1)//S8
            {
                userdata = "USERDATA";
            }
            else if (Device.Model.IndexOf("G960") != -1)
            {
                userdata = "userdata";
            }
            else if (Device.Model.IndexOf("hero2lte") != -1 || Device.Model.IndexOf("hero2lte") != -1)
            {
                userdata = "USERDATA";
            }
            return userdata;
        }

        private string GetBOOT() {
            return "BOOT";
        }

        #endregion


    }
}
