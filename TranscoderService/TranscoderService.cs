using System.Configuration;
using System.ServiceProcess;
using System.Timers;

namespace TranscoderService
{
	public partial class TranscoderService : ServiceBase
	{
		private static Timer timer;

		public TranscoderService()
		{
			InitializeComponent();
		}

		protected override void OnStart(string[] args)
		{
			timer = new Timer();
			var interval = double.Parse(ConfigurationManager.AppSettings["Interval"]);
			timer.Interval = interval * 60000;
			timer.Elapsed += new ElapsedEventHandler(OnTimerElapsed);
			timer.Enabled = true;
			timer.Start();
			Program.WriteLog("Service started");
		}

		protected override void OnStop()
		{
			timer.Enabled = false;
			Program.WriteLog("Service stopped");
		}

		private static void OnTimerElapsed(object sender, ElapsedEventArgs args)
		{
			Program.Transcode();
		}

		public static bool TimerStatus()
		{
			return timer.Enabled;
		}

		public static void EnableTimer()
		{
			timer.Enabled = true;
		}

		public static void DisableTimer()
		{
			timer.Enabled = false;
		}
	}
}