using Serilog;
using SWTools.Core;
using System.Windows;

namespace SWTools.WPF {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        public App() {
            // 启动 Core
            Core.Helper.Main.SetupAll();

            // 添加异常兜底程序
            AppDomain.CurrentDomain.UnhandledException += (sender, args) => {
                LogManager.Log.Fatal("Unhandled exception occurred:\n{Exception}", (Exception)args.ExceptionObject);
                ShowExceptionMsgBox((Exception)args.ExceptionObject, "未捕获的异常");
                LogManager.NoOverride = true;
            };
            DispatcherUnhandledException += (sender, args) => {
                LogManager.Log.Fatal("Unhandled exception occurred in UI thread:\n{Exception}", args.Exception);
                LogManager.NoOverride = true;
                ShowExceptionMsgBox(args.Exception, "未捕获的 UI 线程异常");
                // args.Handled = true;
            };
            TaskScheduler.UnobservedTaskException += (sender, args) => {
                LogManager.Log.Fatal("Unobserved task exception occurred:\n{Exception}", args.Exception);
                LogManager.NoOverride = true;
                ShowExceptionMsgBox(args.Exception, "未观测的任务异常");
                // args.SetObserved();
            };
        }

        private void ShowExceptionMsgBox(Exception ex, string name) {
            string exString = ex.ToString();
            if (exString.Length > 200) {
                exString = exString.Substring(0, 200) + "...";
            }
            MsgBox msgBox = new(name, $"程序即将退出，因为发生了{name}，部分内容如下：\n\n" +
                exString + "\n\n请在日志中查看详细信息；本次程序日志不会被覆盖。", false) { Owner = MainWindow };
            msgBox.ShowDialog();
        }

        // 程序退出时...
        protected override void OnExit(ExitEventArgs e) {
            base.OnExit(e);

            // 关闭 Core
            Core.Helper.Main.CleanupAll();

            // 强制退出所有线程
            Environment.Exit(0);
        }
    }
}
