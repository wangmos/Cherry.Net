using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Cherry.Net.Utils
{
    public static class Tool
    { 

        private static Icon _softIcon;

        /// <summary>
        ///     软件图标
        /// </summary>
        public static Icon SoftIcon => _softIcon = _softIcon ?? Icon.ExtractAssociatedIcon(Application.ExecutablePath);

        /// <summary>
        ///     当前路径 +\
        /// </summary>
        public static string CurDir => AppDomain.CurrentDomain.BaseDirectory;

        /// <summary>
        ///     个人文档路径 + \Cherry\
        /// </summary>
        public static string PersonalDir =>
            $"{Environment.GetFolderPath(Environment.SpecialFolder.Personal)}\\Cherry\\";

         
        /// <summary>
        ///     打开文件夹并选中文件
        /// </summary>
        /// <param name="path"></param>
        public static void OpenDirAndSelectFile(string path)
        {
            Process.Start("Explorer", $"/select,{path}");
        }

        /// <summary>
        ///     从Vb对话框取得字符串
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="title"></param>
        /// <param name="defVal"></param>
        /// <returns></returns>
        public static string GetStrByVb(string msg, string title = "提示", string defVal = "")
        {
            return Interaction.InputBox(msg, title, defVal);
        }

        /// <summary>
        ///     信息弹窗
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="title"></param>
        /// <param name="button"></param>
        /// <param name="icon"></param>
        /// <param name="defButton"></param>
        /// <returns></returns>
        public static DialogResult ShowMsgBox(string msg, string title = "提示",
            MessageBoxButtons button = MessageBoxButtons.OK,
            MessageBoxIcon icon = MessageBoxIcon.Information,
            MessageBoxDefaultButton defButton = MessageBoxDefaultButton.Button1)
        {
            return MessageBox.Show(msg, title, button, icon, defButton);
        }

        /// <summary>
        ///     警告弹窗
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        public static void ShowMsgBoxWarning(string msg, string title = "警告")
        {
            MessageBox.Show(msg, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        /// <summary>
        ///     错误弹窗
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        public static void ShowMsgBoxError(string msg, string title = "错误")
        {
            MessageBox.Show(msg, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>
        ///     确认弹窗
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        public static bool ShowMsgBoxConfirm(string msg, string title = "提示")
        {
            return MessageBox.Show(msg, title, MessageBoxButtons.OKCancel, MessageBoxIcon.Information,
                       MessageBoxDefaultButton.Button2) == DialogResult.OK;
        }


        /// <summary>
        ///     打开文件
        /// </summary>
        /// <param name="fn"></param>
        /// <param name="filter"></param>
        /// <param name="lineAction">读取每行文本回调</param>
        /// <param name="curDir">初始文件夹</param>
        /// <returns></returns>
        public static bool OpenFile(out string fn, string filter = "文本文件 *.txt|*.txt",
            Action<string> lineAction = null, string curDir = null)
        {
            fn = null;
            var ofd = new OpenFileDialog
            {
                Filter = filter,
                InitialDirectory = curDir,
                Multiselect = false
            };
            var isOk = ofd.ShowDialog() == DialogResult.OK;

            if (isOk)
            {
                fn = ofd.FileName;

                if (lineAction != null) ReadFile(fn, lineAction);
            }

            return isOk;
        }

        public static void ReadFile(string fn, Action<string> lineAction)
        {
            using (var sr = new StreamReader(fn, CheckFileEncoding(fn)))
            {
                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();
                    if (!string.IsNullOrEmpty(line))
                        lineAction(line);
                }
            }
        }


        /// <summary>
        ///     打开文件
        /// </summary>
        /// <param name="fn"></param>
        /// <param name="filter"></param>
        /// <param name="lineAction">读取每行文本回调</param>
        /// <param name="curDir">初始文件夹</param>
        /// <returns></returns>
        public static bool OpenFile(out string[] fn, string filter = "文本文件 *.txt|*.txt",
            Action<string> lineAction = null, string curDir = null)
        {
            fn = null;
            var ofd = new OpenFileDialog
            {
                Filter = filter,
                InitialDirectory = curDir,
                Multiselect = true
            };
            var isOk = ofd.ShowDialog() == DialogResult.OK;

            if (isOk)
            {
                fn = ofd.FileNames;

                if (lineAction != null)
                    foreach (var f in fn)
                        ReadFile(f, lineAction);
            }

            return isOk;
        }

        /// <summary>
        ///     保存文件
        /// </summary>
        /// <param name="fn"></param>
        /// <param name="filter"></param>
        /// <param name="cb">打开文件流回调</param>
        /// <param name="curDir"></param>
        /// <param name="defFn"></param>
        /// <returns></returns>
        public static bool SaveFile(out string fn, string filter = "文本文件 *.txt|*.txt",
            Action<StreamWriter> cb = null, string curDir = null, string defFn = null)
        {
            fn = null;
            var ofd = new SaveFileDialog
            {
                Filter = filter,
                InitialDirectory = curDir,
                FileName = defFn
            };
            var isOk = ofd.ShowDialog() == DialogResult.OK;

            if (isOk)
            {
                fn = ofd.FileName;

                if (cb != null)
                    using (var sw = new StreamWriter(File.Open(fn, FileMode.Create), Encoding.UTF8))
                    {
                        cb(sw);
                    }
            }

            return isOk;
        }


        public static bool OpenDirectory(out string dir, Action<string> handleFile = null, string ext = null)
        {
            dir = null;
            var sltDir = new FolderBrowserDialog();

            if (sltDir.ShowDialog() == DialogResult.OK)
            {
                dir = sltDir.SelectedPath;
                if (handleFile != null) ReadDirFile(dir, handleFile, ext);
                return true;
            }

            return false;
        }

        public static void ReadDirFile(string dir, Action<string> handleFile, string ext = null)
        {
            foreach (var file in Directory.GetFiles(dir))
                if (ext == null)
                    handleFile(file);
                else if (file.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) handleFile(file);

            foreach (var directory in Directory.GetDirectories(dir)) ReadDirFile(directory, handleFile, ext);
        }

        /// <summary>
        ///     过滤非法文件路径
        /// </summary>
        /// <param name="fn"></param>
        /// <returns></returns>
        public static string PathFilter(string fn)
        {
            fn = Regex.Replace(fn, "[/*?\"<>|\r\n]", "_", RegexOptions.Compiled);
            var dir = Path.GetDirectoryName(fn);

            if (!string.IsNullOrEmpty(dir))
            {
                var si = fn.IndexOf(dir, StringComparison.OrdinalIgnoreCase) + dir.Length + 1;
                fn = fn.Substring(si, fn.Length - si);
            }

            fn = fn.Replace(":", "_");

            return Path.Combine(dir ?? "", fn);
        }

        /// <summary>
        ///     确保路径存在
        /// </summary>
        /// <param name="path"></param>
        public static string EnsureDirExists(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;
            if (File.Exists(path)) return path;
            path = PathFilter(path);
            var dir = path;
            if (Path.HasExtension(path))
                dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return path;
        }

        /// <summary>
        ///     判定文本文件编码
        /// </summary>
        /// <param name="fn"></param>
        /// <returns></returns>
        public static Encoding CheckFileEncoding(string fn)
        {
            using (var fs = new FileStream(fn, FileMode.Open))
            {
                if (fs.CanRead)
                    switch (fs.ReadByte())
                    {
                        case 0xFF://ff fe
                            return Encoding.Unicode;
                        case 0xFE://fe ff
                            return Encoding.BigEndianUnicode;
                        case 0xEF://ef bb bf
                            return Encoding.UTF8;
                        default:
                            return Encoding.Default;
                    }
            }

            return Encoding.UTF8;
        }
         

        /// <summary>
        ///     重启
        /// </summary>
        public static void ReStart()
        {
            // Process.Start(Application.ExecutablePath);
            // Environment.Exit(0);
            Application.Restart();
        }

        public static void WriteFile<T>(string fn, IEnumerable<T> ts, bool append = true)
        {
            using (var sw = new StreamWriter(fn, append, Encoding.UTF8))
            {
                foreach (var t in ts) sw.WriteLine(t.ToString());
            }
        }

        public static IList<T> ReadFile<T>(string fn, Func<string, T> decode)
        {
            var ls = new List<T>();

            using (var sr = new StreamReader(fn, CheckFileEncoding(fn)))
            {
                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();
                    ls.Add(decode(line));
                }
            }

            return ls;
        }


        /// <summary>
        ///     执行cmd命令
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string DoCmd(string str)
        {
            try
            {
                var p = new Process
                {
                    StartInfo =
                    {
                        FileName = "cmd.exe",
                        //表示执行完命令后马上退出
                        Arguments = "/c " + str,
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                p.Start();
                return p.StandardOutput.ReadToEnd();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                return e.Message;
            }
        }


        /// <summary>
        ///     取得调用堆栈层级
        /// </summary>
        /// <returns></returns>
        public static string GetStackTraceModelName()
        {
            var st = new StackTrace();
            var sfs = st.GetFrames();
            var sb = new StringBuilder();

            for (var i = 1; i < sfs?.Length; ++i)
            {
                if (StackFrame.OFFSET_UNKNOWN == sfs[i].GetILOffset()) break;
                sb.Append($"{sfs[i].GetMethod().Name}() <--- ");
            }

            return sb.ToString();
        }

        /// <summary>
        ///     合并文件
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="fileFormat">合并文件后缀名 例:*.txt</param>
        /// <param name="newFn"></param>
        public static void MergeFile(string dir, string fileFormat, string newFn)
        {
            //DoCmd($"copy /b {dir}\\{fileFormat} {newFn}");

            using (var fs = new FileStream(newFn, FileMode.Create))
            {
                var ls = Directory.GetFiles(dir, fileFormat);

                if (ls.Length > 0)
                {
                    var bs = new byte[4096];

                    foreach (var file in ls)
                        using (var fs2 = new FileStream(file, FileMode.Open))
                        {
                            while (fs2.CanRead)
                            {
                                var i = fs2.Read(bs, 0, bs.Length);
                                fs.Write(bs, 0, i);
                            }
                        }
                }
            }
        }

        /// <summary>
        ///     删除文件夹下所有文件
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="fileFormat">删除文件后缀名 例:*.txt</param>
        public static void DelFiles(string dir, string fileFormat)
        {
            DoCmd($"del /F /S /Q {dir}\\{fileFormat}");
        }
    }
}