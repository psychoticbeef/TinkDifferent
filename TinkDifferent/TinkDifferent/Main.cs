using System;
using System.Configuration;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Tinkerforge;

namespace TinkDifferent {
	class MainClass {
		
		public static string host = string.Empty;
		public static int port = -1;
		public static int server_port = -1;

		public static BrickletAmbientLight ambient_light;
		public static BrickletTemperature temperature;
		public static BrickletLCD20x4 lcd;
		
		public static LCDisplayHelper displayHelper;
		
		public static bool init(ref IPConnection brick_connection) {
			
			// get UIDs of bricks and bricklets
			AppSettingsReader config = new AppSettingsReader();
			
			host = (string)config.GetValue("host", typeof(string));
			port = (int)config.GetValue("port", typeof(int));
			server_port = (int)config.GetValue("server_port", typeof(int));
			string master_brick_uid = (string)config.GetValue("master_brick_uid", typeof(string));
			string ambient_bricklet_uid = (string)config.GetValue("ambient_light_bricklet_uid", typeof(string));
			string temperature_bricklet_uid = (string)config.GetValue("temperature_bricklet_uid", typeof(string));
			string lcd_20x4_bricklet_uid = (string)config.GetValue("lcd_20x4_bricklet_uid", typeof(string));
			
#if DEBUG
			Console.WriteLine("Master Brick Host:Port ...: {0}:{1}", host, port);
			Console.WriteLine("Serverport (udp msgs).....: :{0}", server_port);
			Console.WriteLine("Master Brick UID .........: {0}", master_brick_uid);
			Console.WriteLine("Ambient Bricklet UID .....: {0}", ambient_bricklet_uid);
			Console.WriteLine("Temperature Bricklet UID .: {0}", temperature_bricklet_uid);
			Console.WriteLine("LCD 20x4 Bricklet UID ....: {0}", lcd_20x4_bricklet_uid);
#endif
			
			// setup connection to master und bricklets
			try {
				brick_connection = new IPConnection(host, port);
				
				ambient_light = new BrickletAmbientLight(ambient_bricklet_uid);
				temperature = new BrickletTemperature(temperature_bricklet_uid);
				lcd = new BrickletLCD20x4(lcd_20x4_bricklet_uid);
				displayHelper = new LCDisplayHelper(lcd);
			
				brick_connection.AddDevice(ambient_light);
				brick_connection.AddDevice(temperature);
				brick_connection.AddDevice(lcd);
			} catch (Tinkerforge.TimeoutException te) {
				Console.WriteLine(te.Message);
				return false;
			}
			
			return true;
		}
		
		public static void setup() {
			lcd.BacklightOff();
			lcd.ClearDisplay();
			lcd.SetConfig(false, false);
		}
		
		//***************************
		//*** Pimmelscheisse Ende ***
		//***************************
		
		static void TemperatureCB(short temperature) {
			string result = temperature/100.0 + " °C";
			displayHelper.setTextForLine(1, result);
#if DEBUG
			Console.WriteLine(result);
#endif
		}
		
		static void IlluminanceCB(ushort illuminance) {
			string result = illuminance/10.0 + " Lux";
        	displayHelper.setTextForLine(2, result);
#if DEBUG
			Console.WriteLine(result);
#endif
		}

		//*********************
		//*** UDP Servierer ***
		//*********************
		
		public static void udp_server() {
			IPHostEntry localHostEntry;
			try {
				Socket soUdp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
				try {
					localHostEntry = Dns.GetHostByName(Dns.GetHostName());
				} catch(Exception) {
					Console.WriteLine("Local Host not found"); // fail
					return ;
				}
						
				IPEndPoint localIpEndPoint = new IPEndPoint(localHostEntry.AddressList[0], server_port);
				soUdp.Bind(localIpEndPoint);
				while (true) {
					Byte[] received = new Byte[256];
					IPEndPoint tmpIpEndPoint = new IPEndPoint(localHostEntry.AddressList[0], server_port);
					EndPoint remoteEP = (tmpIpEndPoint);
					int bytesReceived = soUdp.ReceiveFrom(received, ref remoteEP);
					String dataReceived = (System.Text.Encoding.UTF8.GetString(received)).Trim();
					
					/* int truncated_length = Math.Min(20, dataReceived.Length);
					if (dataReceived != null)
						dataReceived = dataReceived.Substring(0, truncated_length); */
					int cnt_crap = 0;
					for (int i = dataReceived.Length - 1; i > 0; i--) {
						if ((byte)dataReceived[i] == 0)
							cnt_crap++;
						else
							break;
					}
					
					dataReceived = dataReceived.Substring(0, dataReceived.Length - cnt_crap);
#if DEBUG
					Console.WriteLine(dataReceived+ " " + dataReceived.Length);
#endif
					if (dataReceived != null) {
						displayHelper.setTextForLine(3, dataReceived);
					}
				}
			} catch (SocketException se) {
				Console.WriteLine("A Socket Exception has occurred!" + se.ToString());
			}
		}
		
		public static IPConnection brick_connection;
		
		public static void Main (string[] args) {
			if (init(ref brick_connection)) {
				setup();
			
				displayHelper.setTextForLine(0, "Hello World!");
				
				temperature.SetTemperatureCallbackPeriod(1000);
				temperature.RegisterCallback(new BrickletTemperature.Temperature(TemperatureCB));
				
				ambient_light.SetIlluminanceCallbackPeriod(1000);
				ambient_light.RegisterCallback(new BrickletAmbientLight.Illuminance(IlluminanceCB));
			}
			
			Thread udp_thread = new Thread(new ThreadStart(udp_server));
			udp_thread.Start();
			
			brick_connection.JoinThread();
		}
	}
}
